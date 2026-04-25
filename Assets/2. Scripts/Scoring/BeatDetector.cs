using System;
using UnityEngine;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

/// <summary>
/// Detects beats in real time using FMOD DSP spectrum analysis.
/// Computes BPM from beat timestamps and exposes a shot-evaluation API
/// used by ScoreManager to rate timing accuracy.
/// </summary>
public class BeatDetector : MonoBehaviour
{
    [Header("FMOD Settings")]
    [Tooltip("FMOD event path, e.g. event:/Music/Level1")]
    public string musicEventPath = "event:/Music/Level1";

    [Header("Beat Detection Tuning")]
    // FIX: Range was [0,1] but default value was 1.3 (out of range).
    // Unity would silently clamp it to 1.0 in the inspector, making detection
    // far too sensitive. Corrected range to [1, 3].
    [Range(1f, 3f)]
    [Tooltip("Energy threshold multiplier. Higher = less sensitive to beats.")]
    public float sensitivityThreshold = 1.3f;

    [Range(0.05f, 0.5f)]
    [Tooltip("Minimum seconds between detected beats to prevent double-triggers.")]
    public float minTimeBetweenBeats = 0.15f;

    [Range(64, 4096)]
    [Tooltip("FFT window size for spectrum analysis. Must be a power of 2.")]
    public int spectrumSize = 512;

    [Header("Beat Window (Scoring)")]
    public float beatWindowBefore = 0.1f;
    public float beatWindowAfter  = 0.12f;

    [Header("Score Multipliers")]
    public float perfectBeatMultiplier = 2.0f;
    public float goodBeatMultiplier    = 1.5f;
    public float offBeatMultiplier     = 1.0f;

    // FIX: Analyze every N frames instead of every frame.
    // Marshal.PtrToStructure on DSP_PARAMETER_FFT allocates a managed object
    // (jagged float[][] array) every call. On Android/Oculus this causes
    // severe per-frame GC pressure that leads to crashes.
    // At 72fps, every-3-frames = ~24 analysis ticks/sec, well above what's needed.
    [Header("Performance")]
    [Tooltip("Run spectrum analysis every N frames. 1 = every frame (not recommended on Oculus). 3 is a safe default.")]
    [Range(1, 10)]
    public int analyzeEveryNFrames = 3;

    public Action OnBeatDetected;
    public Action<float> OnBpmUpdated;

    public float CurrentBpm        { get; private set; }
    public float LastBeatTime      { get; private set; }
    public float NextBeatPredicted { get; private set; }
    public bool  IsPlaying         { get; private set; }

    private EventInstance _musicInstance;
    private DSP           _fftDsp;
    private ChannelGroup  _masterGroup;

    // Rolling history buffer - ~1 second of energy samples at 512/44100
    private float[]     _spectrumHistory;
    private int         _historyIndex;
    private const int   HistorySize = 43;

    // Circular buffer for beat timestamps - avoids O(n) RemoveAt(0) on every beat
    private readonly float[] _beatTimestamps = new float[MaxBeatHistory];
    private int   _beatWriteIndex;
    private int   _beatCount;
    private const int MaxBeatHistory = 16;

    private float _energyRunningTotal;
    private float _lastBeatRealTime;
    private int   _frameCounter;

    #region Unity Lifecycle

    private void Awake() => _spectrumHistory = new float[HistorySize];

    // FIX: Start() no longer calls PlayMusic().
    // GameStateManager.StartGame() is the single entry point for starting music.
    // Having both Start() and GameStateManager call PlayMusic() created two
    // FMOD event instances and two FFT DSPs on the master channel group.
    private void Start() { }

    private void Update()
    {
        if (!IsPlaying) return;

        // FIX: Frame-skip spectrum analysis to reduce per-frame GC allocations
        // from Marshal.PtrToStructure (DSP_PARAMETER_FFT contains float[][]).
        _frameCounter++;
        if (_frameCounter >= analyzeEveryNFrames)
        {
            _frameCounter = 0;
            AnalyzeSpectrum();
        }

        UpdatePredictedNextBeat();
    }

    private void OnDestroy() => StopMusic();

    #endregion

    #region FMOD Setup

    public void PlayMusic()
    {
        if (IsPlaying)
        {
            Debug.LogWarning("[BeatDetector] PlayMusic called but already playing. Ignoring.");
            return;
        }

        if (string.IsNullOrEmpty(musicEventPath))
        {
            Debug.LogError("[BeatDetector] musicEventPath is empty.");
            return;
        }

        _musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        _musicInstance.start();

        // Attach an FFT DSP to the master channel group to read spectrum data
        RuntimeManager.CoreSystem.getMasterChannelGroup(out _masterGroup);
        RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out _fftDsp);
        _fftDsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, spectrumSize);
        _fftDsp.setParameterInt((int)DSP_FFT.WINDOWTYPE, 0); // 0 = Hanning window
        _masterGroup.addDSP(CHANNELCONTROL_DSP_INDEX.TAIL, _fftDsp);
        _fftDsp.setActive(true);

        IsPlaying = true;
    }

    public void StopMusic()
    {
        IsPlaying = false;

        if (_fftDsp.hasHandle())
        {
            _masterGroup.removeDSP(_fftDsp);
            _fftDsp.release();
        }

        if (_musicInstance.isValid())
        {
            _musicInstance.stop(STOP_MODE.ALLOWFADEOUT);
            _musicInstance.release();
        }
    }

    #endregion

    #region Spectrum Analysis

    private void AnalyzeSpectrum()
    {
        if (!_fftDsp.hasHandle()) return;

        // Read raw spectrum data from unmanaged FMOD memory via pointer.
        // NOTE: This still uses Marshal.PtrToStructure which allocates a managed
        // DSP_PARAMETER_FFT (with float[][] spectrum) each call. The frame-skip
        // above reduces frequency, but if crashes persist the deeper fix is to
        // read individual bin values via getParameterFloat instead.
        _fftDsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out IntPtr unmanagedData, out _);
        if (unmanagedData == IntPtr.Zero) return;

        DSP_PARAMETER_FFT fftData = (DSP_PARAMETER_FFT)
            Marshal.PtrToStructure(unmanagedData, typeof(DSP_PARAMETER_FFT));

        if (fftData.numchannels == 0 || fftData.spectrum == null || fftData.spectrum.Length == 0) return;

        // Sum sub-bass and bass bins (indices 0-15 ~ 0-1300 Hz at 44100Hz/512 window)
        float energy = 0f;
        int bassEnd  = Mathf.Min(16, fftData.length);
        for (int i = 0; i < bassEnd; i++)
            energy += fftData.spectrum[0][i];
        energy /= bassEnd;

        // Maintain rolling average incrementally
        _energyRunningTotal -= _spectrumHistory[_historyIndex];
        _spectrumHistory[_historyIndex] = energy;
        _energyRunningTotal += energy;
        _historyIndex = (_historyIndex + 1) % HistorySize;
        float energyAverage = _energyRunningTotal / HistorySize;

        float now = Time.realtimeSinceStartup;
        if (energy > energyAverage * sensitivityThreshold &&
            now - _lastBeatRealTime > minTimeBetweenBeats)
        {
            _lastBeatRealTime = now;
            RegisterBeat();
        }
    }

    private void RegisterBeat()
    {
        LastBeatTime = Time.time;
        _beatTimestamps[_beatWriteIndex] = LastBeatTime;
        _beatWriteIndex = (_beatWriteIndex + 1) % MaxBeatHistory;
        if (_beatCount < MaxBeatHistory) _beatCount++;

        EstimateBpm();
        OnBeatDetected?.Invoke();
    }

    /// <summary>
    /// Estimates BPM from recent beat intervals.
    /// Only intervals in the 30-300 BPM range (0.2s - 2.0s) are included.
    /// </summary>
    private void EstimateBpm()
    {
        if (_beatCount < 4) return;

        float total = 0f;
        int   count = 0;

        for (int i = 1; i < _beatCount; i++)
        {
            int curr = (_beatWriteIndex - i + MaxBeatHistory)     % MaxBeatHistory;
            int prev = (_beatWriteIndex - i - 1 + MaxBeatHistory) % MaxBeatHistory;
            float interval = _beatTimestamps[curr] - _beatTimestamps[prev];
            if (interval > 0.2f && interval < 2.0f) { total += interval; count++; }
        }

        if (count == 0) return;

        float newBpm = 60f / (total / count);
        if (Mathf.Abs(newBpm - CurrentBpm) > 1f)
        {
            CurrentBpm = newBpm;
            OnBpmUpdated?.Invoke(CurrentBpm);
        }
    }

    private void UpdatePredictedNextBeat()
    {
        if (CurrentBpm <= 0) return;
        NextBeatPredicted = LastBeatTime + 60f / CurrentBpm;
    }

    #endregion

    #region Scoring API

    /// <summary>
    /// Evaluates how close a shot was to the nearest beat.
    /// Returns a BeatScore with a rating and score multiplier.
    /// Called by BeatScoreManager.RegisterHit().
    /// </summary>
    public BeatScore EvaluateShot()
    {
        float now           = Time.time;
        float timeSinceBeat = now - LastBeatTime;
        float timeToNext    = NextBeatPredicted - now;
        float distToNearest = Mathf.Min(timeSinceBeat, timeToNext);

        BeatRating rating;
        float multiplier;

        if (distToNearest <= beatWindowBefore * 0.5f)
        {
            rating     = BeatRating.Perfect;
            multiplier = perfectBeatMultiplier;
        }
        else if (distToNearest <= Mathf.Max(beatWindowBefore, beatWindowAfter))
        {
            rating     = BeatRating.Good;
            multiplier = goodBeatMultiplier;
        }
        else
        {
            rating     = BeatRating.OffBeat;
            multiplier = offBeatMultiplier;
        }

        return new BeatScore
        {
            rating         = rating,
            multiplier     = multiplier,
            distanceToBeat = distToNearest,
            timeSinceBeat  = timeSinceBeat,
            timeToNextBeat = timeToNext
        };
    }

    #endregion
}

#region Data Types

public enum BeatRating { Perfect, Good, OffBeat }

[Serializable]
public struct BeatScore
{
    [FormerlySerializedAs("Rating")]         public BeatRating rating;
    [FormerlySerializedAs("Multiplier")]     public float      multiplier;
    [FormerlySerializedAs("DistanceToBeat")] public float      distanceToBeat;
    [FormerlySerializedAs("TimeSinceBeat")]  public float      timeSinceBeat;
    [FormerlySerializedAs("TimeToNextBeat")] public float      timeToNextBeat;

    public override string ToString() =>
        $"{rating} x{multiplier:F1} | dist={distanceToBeat * 1000:F0}ms";
}

#endregion