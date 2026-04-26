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

    [Header("Performance")]
    [Range(1, 10)]
    [Tooltip("Run spectrum analysis every N frames. 3 is recommended for Oculus.")]
    public int analyzeEveryNFrames = 3;

    public Action OnBeatDetected;
    public Action<float> OnBpmUpdated;

    public float CurrentBpm        { get; private set; }
    public float LastBeatTime      { get; private set; }
    public float NextBeatPredicted { get; private set; }

    // IsPlaying now polls the FMOD instance directly so natural song end
    // is detected automatically without requiring an explicit StopMusic() call.
    public bool IsPlaying
    {
        get
        {
            if (!_musicInstance.isValid()) return false;
            _musicInstance.getPlaybackState(out PLAYBACK_STATE state);
            return state != PLAYBACK_STATE.STOPPED && state != PLAYBACK_STATE.STOPPING;
        }
    }

    private bool _started;

    private EventInstance _musicInstance;
    private DSP           _fftDsp;
    private ChannelGroup  _masterGroup;

    private float[]   _spectrumHistory;
    private int       _historyIndex;
    private const int HistorySize = 43;

    private readonly float[] _beatTimestamps = new float[MaxBeatHistory];
    private int       _beatWriteIndex;
    private int       _beatCount;
    private const int MaxBeatHistory = 16;

    private float _energyRunningTotal;
    private float _lastBeatRealTime;
    private int   _frameCounter;

    #region Unity Lifecycle

    private void Awake() => _spectrumHistory = new float[HistorySize];
    private void Start()  { }

    private void Update()
    {
        if (!IsPlaying) return;

        if (++_frameCounter >= analyzeEveryNFrames)
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
            Debug.LogWarning("[BeatDetector] PlayMusic called while already playing. Ignoring.");
            return;
        }

        if (string.IsNullOrEmpty(musicEventPath))
        {
            Debug.LogError("[BeatDetector] musicEventPath is empty.");
            return;
        }

        _musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        _musicInstance.start();

        RuntimeManager.CoreSystem.getMasterChannelGroup(out _masterGroup);
        RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out _fftDsp);
        _fftDsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, spectrumSize);
        _fftDsp.setParameterInt((int)DSP_FFT.WINDOWTYPE, 0);
        _masterGroup.addDSP(CHANNELCONTROL_DSP_INDEX.TAIL, _fftDsp);
        _fftDsp.setActive(true);
    }

    public void StopMusic()
    {
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

        _fftDsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out IntPtr unmanagedData, out _);
        if (unmanagedData == IntPtr.Zero) return;

        DSP_PARAMETER_FFT fftData = (DSP_PARAMETER_FFT)
            Marshal.PtrToStructure(unmanagedData, typeof(DSP_PARAMETER_FFT));

        if (fftData.numchannels == 0 || fftData.spectrum == null || fftData.spectrum.Length == 0) return;

        float energy  = 0f;
        int   bassEnd = Mathf.Min(16, fftData.length);
        for (int i = 0; i < bassEnd; i++)
            energy += fftData.spectrum[0][i];
        energy /= bassEnd;

        _energyRunningTotal -= _spectrumHistory[_historyIndex];
        _spectrumHistory[_historyIndex] = energy;
        _energyRunningTotal += energy;
        _historyIndex = (_historyIndex + 1) % HistorySize;

        if (_historyIndex == 0)
        {
            _energyRunningTotal = 0f;
            foreach (float v in _spectrumHistory) _energyRunningTotal += v;
        }

        float energyAverage = _energyRunningTotal / HistorySize;
        float now           = Time.realtimeSinceStartup;

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

    private void EstimateBpm()
    {
        if (_beatCount < 4) return;

        float total = 0f;
        int   count = 0;
        int   limit = Mathf.Min(_beatCount - 1, MaxBeatHistory - 1);

        for (int i = 1; i <= limit; i++)
        {
            int curr       = (_beatWriteIndex - i     + MaxBeatHistory) % MaxBeatHistory;
            int prev       = (_beatWriteIndex - i - 1 + MaxBeatHistory) % MaxBeatHistory;
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

    public BeatScore EvaluateShot()
    {
        float now           = Time.time;
        float timeSinceBeat = now - LastBeatTime;
        float timeToNext    = Mathf.Max(0f, NextBeatPredicted - now);
        float distToNearest = Mathf.Min(timeSinceBeat, timeToNext);

        BeatRating rating;
        float      multiplier;

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