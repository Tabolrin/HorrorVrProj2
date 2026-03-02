using UnityEngine;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;

namespace FMOD
{
    public class DSP_FFT_WINDOW
    {
        public static int HANNING { get; set; }
    }
}

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
    public System.Action OnBeatDetected;       // fired every beat
    public System.Action<float> OnBPMUpdated;  // fired when BPM estimate changes

    // ── Public Read-Only State ───────────────────────────────────────────────
    public float CurrentBPM        { get; private set; }
    public float LastBeatTime      { get; private set; }
    public float NextBeatPredicted { get; private set; }
    public bool  IsPlaying         { get; private set; }

    // ── Private ──────────────────────────────────────────────────────────────
    private EventInstance _musicInstance;
    private FMOD.DSP      _fftDsp;
    private FMOD.Channel  _channel;
    private FMOD.ChannelGroup _masterGroup;

    private float[]           _spectrumData;
    private float[]           _spectrumHistory;
    private int               _historyIndex;
    private const int         HISTORY_SIZE = 43; // ~1 second at 512/44100

    private readonly List<float> _beatTimestamps = new List<float>();
    private const int            MAX_BEAT_HISTORY = 16;

    private float _lastBeatRealTime;
    private float _energyAverage;

    // ─────────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        _spectrumData    = new float[spectrumSize];
        _spectrumHistory = new float[HISTORY_SIZE];
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
        FMODUnity.RuntimeManager.CoreSystem.getMasterChannelGroup(out _masterGroup);
        FMODUnity.RuntimeManager.CoreSystem.createDSPByType(FMOD.DSP_TYPE.FFT, out _fftDsp);

        _fftDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWSIZE, spectrumSize);
        _fftDsp.setParameterInt((int)FMOD.DSP_FFT.WINDOWTYPE, (int)FMOD.DSP_FFT_WINDOW.HANNING);

        _masterGroup.addDSP(FMOD.CHANNELCONTROL_DSP_INDEX.TAIL, _fftDsp);
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
            _musicInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
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
        System.IntPtr unmanagedData;
        uint           length;
        _fftDsp.getParameterData((int)FMOD.DSP_FFT.SPECTRUMDATA, out unmanagedData, out length);

        FMOD.DSP_PARAMETER_FFT fftData = (FMOD.DSP_PARAMETER_FFT)
            System.Runtime.InteropServices.Marshal.PtrToStructure(unmanagedData, typeof(FMOD.DSP_PARAMETER_FFT));

        if (fftData.numchannels == 0) return;

        // Compute sub-bass + bass energy (indices 0-15 ≈ 0-1300 Hz at 44100/512)
        float energy = 0f;
        int bassEnd  = Mathf.Min(16, fftData.length);
        for (int i = 0; i < bassEnd; i++)
            energy += fftData.spectrum[0][i]; // channel 0 (left)

        energy /= bassEnd;

        // Rolling average
        _spectrumHistory[_historyIndex] = energy;
        _historyIndex = (_historyIndex + 1) % HISTORY_SIZE;

        _energyAverage = 0f;
        foreach (float e in _spectrumHistory)
            _energyAverage += e;
        _energyAverage /= HISTORY_SIZE;

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
        if (_beatTimestamps.Count > MAX_BEAT_HISTORY)
            _beatTimestamps.RemoveAt(0);

        EstimateBPM();
        OnBeatDetected?.Invoke();

        Debug.Log($"[BeatDetector] Beat! BPM ≈ {CurrentBPM:F1}");
    }

    private void EstimateBPM()
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
        float newBPM      = 60f / avgInterval;

        if (Mathf.Abs(newBPM - CurrentBPM) > 1f)
        {
            CurrentBPM = newBPM;
            OnBPMUpdated?.Invoke(CurrentBPM);
        }
    }

    private void UpdatePredictedNextBeat()
    {
        if (CurrentBPM <= 0) return;
        float interval     = 60f / CurrentBPM;
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
            Rating         = rating,
            Multiplier     = multiplier,
            DistanceToBeat = distToNearest,
            TimeSinceBeat  = timeSinceBeat,
            TimeToNextBeat = timeToNext
        };
    }

    #endregion
}

// ─────────────────────────────────────────────────────────────────────────────
#region Data Types

public enum BeatRating { Perfect, Good, OffBeat }

[System.Serializable]
public struct BeatScore
{
    public BeatRating Rating;
    public float      Multiplier;
    public float      DistanceToBeat;
    public float      TimeSinceBeat;
    public float      TimeToNextBeat;

    public override string ToString() =>
        $"{Rating} ×{Multiplier:F1} | dist={DistanceToBeat*1000:F0}ms";
}

#endregion