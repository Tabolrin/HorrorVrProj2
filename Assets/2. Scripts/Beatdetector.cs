using System;
using UnityEngine;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using STOP_MODE = FMOD.Studio.STOP_MODE;

/// <summary>
/// Beat Detector using FMOD DSP spectrum analysis.
/// Detects BPM and beat timing from any music played through FMOD.
/// </summary>
public class BeatDetector : MonoBehaviour
{
    [Header("FMOD Settings")]
    [Tooltip("The FMOD event path of your music, e.g. event:/Music/Level1")]
    public string musicEventPath = "event:/Music/Level1";

    [Header("Beat Detection Tuning")]
    [Range(0f, 1f)]
    [Tooltip("Energy threshold multiplier. Higher = less sensitive.")]
    public float sensitivityThreshold = 1.3f;

    [Range(0.05f, 0.5f)]
    [Tooltip("Minimum time (seconds) between detected beats to avoid double-triggers.")]
    public float minTimeBetweenBeats = 0.15f;

    [Range(64, 4096)]
    [Tooltip("FFT window size for spectrum analysis. Power of 2.")]
    public int spectrumSize = 512;

    [Header("Beat Window (Scoring)")]
    [Tooltip("How many seconds BEFORE the beat the player can shoot and still be 'on beat'.")]
    public float beatWindowBefore = 0.1f;
    [Tooltip("How many seconds AFTER the beat the player can shoot and still be 'on beat'.")]
    public float beatWindowAfter = 0.12f;

    [Header("Score Multipliers")]
    public float perfectBeatMultiplier = 2.0f;
    public float goodBeatMultiplier    = 1.5f;
    public float offBeatMultiplier     = 1.0f;

    // ── Events ──────────────────────────────────────────────────────────────
    public Action OnBeatDetected;       // fired every beat
    public Action<float> OnBpmUpdated;  // fired when BPM estimate changes

    // ── Public Read-Only State ───────────────────────────────────────────────
    public float CurrentBpm        { get; private set; }
    public float LastBeatTime      { get; private set; }
    public float NextBeatPredicted { get; private set; }
    public bool  IsPlaying         { get; private set; }

    // ── Private ──────────────────────────────────────────────────────────────
    private EventInstance _musicInstance;
    private DSP      _fftDsp;
    private Channel  _channel;
    private ChannelGroup _masterGroup;

    private float[]           _spectrumHistory;
    private int               _historyIndex;
    private const int         HistorySize = 43; // ~1 second at 512/44100

    private readonly List<float> _beatTimestamps = new List<float>();
    private const int            MaxBeatHistory = 16;

    private float _lastBeatRealTime;
    private float _energyAverage;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        _spectrumHistory = new float[HistorySize];
    }

    private void Start()
    {
        PlayMusic();
    }

    private void Update()
    {
        if (!IsPlaying) return;

        AnalyzeSpectrum();
        UpdatePredictedNextBeat();
    }

    private void OnDestroy()
    {
        StopMusic();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region FMOD Setup

    public void PlayMusic()
    {
        if (string.IsNullOrEmpty(musicEventPath))
        {
            Debug.LogError("[BeatDetector] musicEventPath is empty!");
            return;
        }

        _musicInstance = RuntimeManager.CreateInstance(musicEventPath);
        _musicInstance.start();

        // Attach FFT DSP to the master channel group for spectrum reading
        RuntimeManager.CoreSystem.getMasterChannelGroup(out _masterGroup);
        RuntimeManager.CoreSystem.createDSPByType(DSP_TYPE.FFT, out _fftDsp);

        _fftDsp.setParameterInt((int)DSP_FFT.WINDOWSIZE, spectrumSize);
        _fftDsp.setParameterInt((int)DSP_FFT.WINDOWTYPE, 0);

        _masterGroup.addDSP(CHANNELCONTROL_DSP_INDEX.TAIL, _fftDsp);
        _fftDsp.setActive(true);

        IsPlaying = true;
        Debug.Log("[BeatDetector] Music started, beat detection active.");
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

    // ─────────────────────────────────────────────────────────────────────────
    #region Spectrum Analysis & Beat Detection

    private void AnalyzeSpectrum()
    {
        if (!_fftDsp.hasHandle()) return;

        // Pull spectrum data from FFT DSP
        IntPtr unmanagedData;
        _fftDsp.getParameterData((int)DSP_FFT.SPECTRUMDATA, out unmanagedData, out _);

        DSP_PARAMETER_FFT fftData = (DSP_PARAMETER_FFT)
            Marshal.PtrToStructure(unmanagedData, typeof(DSP_PARAMETER_FFT));

        if (fftData.numchannels == 0) return;

        // Compute sub-bass + bass energy (indices 0-15 ≈ 0-1300 Hz at 44100/512)
        float energy = 0f;
        int bassEnd  = Mathf.Min(16, fftData.length);
        for (int i = 0; i < bassEnd; i++)
            energy += fftData.spectrum[0][i]; // channel 0 (left)

        energy /= bassEnd;

        // Rolling average
        _spectrumHistory[_historyIndex] = energy;
        _historyIndex = (_historyIndex + 1) % HistorySize;

        _energyAverage = 0f;
        foreach (float e in _spectrumHistory)
            _energyAverage += e;
        _energyAverage /= HistorySize;

        // Beat condition: current energy significantly above recent average
        float threshold = _energyAverage * sensitivityThreshold;
        float now       = Time.realtimeSinceStartup;

        if (energy > threshold && (now - _lastBeatRealTime) > minTimeBetweenBeats)
        {
            _lastBeatRealTime = now;
            RegisterBeat();
        }
    }

    private void RegisterBeat()
    {
        float t = Time.time;
        LastBeatTime = t;

        _beatTimestamps.Add(t);
        if (_beatTimestamps.Count > MaxBeatHistory)
            _beatTimestamps.RemoveAt(0);

        EstimateBpm();
        OnBeatDetected?.Invoke();

        Debug.Log($"[BeatDetector] Beat! BPM ≈ {CurrentBpm:F1}");
    }

    private void EstimateBpm()
    {
        if (_beatTimestamps.Count < 4) return;

        // Average interval between recent beats
        float totalInterval = 0f;
        int   count         = 0;
        for (int i = 1; i < _beatTimestamps.Count; i++)
        {
            float interval = _beatTimestamps[i] - _beatTimestamps[i - 1];
            if (interval > 0.2f && interval < 2.0f) // sanity: 30–300 BPM
            {
                totalInterval += interval;
                count++;
            }
        }

        if (count == 0) return;

        float avgInterval = totalInterval / count;
        float newBpm      = 60f / avgInterval;

        if (Mathf.Abs(newBpm - CurrentBpm) > 1f)
        {
            CurrentBpm = newBpm;
            OnBpmUpdated?.Invoke(CurrentBpm);
        }
    }

    private void UpdatePredictedNextBeat()
    {
        if (CurrentBpm <= 0) return;
        float interval     = 60f / CurrentBpm;
        NextBeatPredicted  = LastBeatTime + interval;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────────
    #region Public Scoring API

    /// <summary>
    /// Call this when the player shoots.
    /// Returns a BeatScore with a multiplier and rating string.
    /// </summary>
    public BeatScore EvaluateShot()
    {
        float now         = Time.time;
        float timeSinceBeat = now - LastBeatTime;
        float timeToNext    = NextBeatPredicted - now;

        // Distance to nearest beat (could be last or next)
        float distToNearest = Mathf.Min(timeSinceBeat, timeToNext);

        BeatRating rating;
        float multiplier;

        if (distToNearest <= beatWindowBefore * 0.5f)
        {
            // Dead center
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

// ─────────────────────────────────────────────────────────────────────────────
#region Data Types

public enum BeatRating { Perfect, Good, OffBeat }

[Serializable]
public struct BeatScore
{
    [FormerlySerializedAs("Rating")] public BeatRating rating;
    [FormerlySerializedAs("Multiplier")] public float      multiplier;
    [FormerlySerializedAs("DistanceToBeat")] public float      distanceToBeat;
    [FormerlySerializedAs("TimeSinceBeat")] public float      timeSinceBeat;
    [FormerlySerializedAs("TimeToNextBeat")] public float      timeToNextBeat;

    public override string ToString() =>
        $"{rating} ×{multiplier:F1} | dist={distanceToBeat*1000:F0}ms";
}

#endregion