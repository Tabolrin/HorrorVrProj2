using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug visualizer for beat detection.
/// Attach to a Canvas GameObject. Shows a pulsing circle on each beat
/// and a timeline bar showing how close you are to the beat.
/// </summary>
public class BeatVisualizer : MonoBehaviour
{
    [Header("References")]
    public BeatDetector beatDetector;
    public Image        beatPulseImage;   // a circle UI image
    public Image        beatTimelineBar;  // a horizontal bar
    public Image        shotMarker;       // shows where the shot landed

    [Header("Colors")]
    public Color colorPerfect = Color.yellow;
    public Color colorGood    = new Color(0f, 1f, 0.5f);
    public Color colorOffBeat = Color.red;
    public Color colorIdle    = new Color(1f, 1f, 1f, 0.3f);

    private float _pulseTimer;
    private const float PULSE_DURATION = 0.12f;

    private void OnEnable()
    {
        if (beatDetector)
            beatDetector.OnBeatDetected += HandleBeat;
    }

    private void OnDisable()
    {
        if (beatDetector)
            beatDetector.OnBeatDetected -= HandleBeat;
    }

    private void HandleBeat()
    {
        _pulseTimer = PULSE_DURATION;
        if (beatPulseImage) beatPulseImage.color = Color.white;
    }

    private void Update()
    {
        // Fade pulse
        if (_pulseTimer > 0f)
        {
            _pulseTimer -= Time.deltaTime;
            float t = _pulseTimer / PULSE_DURATION;
            if (beatPulseImage)
            {
                beatPulseImage.transform.localScale = Vector3.one * (1f + t * 0.5f);
                beatPulseImage.color = Color.Lerp(colorIdle, Color.white, t);
            }
        }
        else if (beatPulseImage)
        {
            beatPulseImage.transform.localScale = Vector3.one;
            beatPulseImage.color = colorIdle;
        }

        // Timeline bar: fills from 0 to 1 between beats
        if (beatDetector && beatDetector.CurrentBpm > 0 && beatTimelineBar)
        {
            float interval   = 60f / beatDetector.CurrentBpm;
            float timeSince  = Time.time - beatDetector.LastBeatTime;
            float normalized = Mathf.Clamp01(timeSince / interval);
            beatTimelineBar.fillAmount = normalized;
        }
    }

    /// <summary>
    /// Call after RegisterHit to show the shot result visually.
    /// </summary>
    public void ShowShotResult(BeatScore score)
    {
        if (!shotMarker) return;

        shotMarker.color = score.rating switch
        {
            BeatRating.Perfect => colorPerfect,
            BeatRating.Good    => colorGood,
            _                  => colorOffBeat
        };

        // Position marker on the timeline bar
        if (beatTimelineBar && beatDetector.CurrentBpm > 0)
        {
            float interval   = 60f / beatDetector.CurrentBpm;
            float normalized = Mathf.Clamp01(score.timeSinceBeat / interval);
            RectTransform rt    = beatTimelineBar.GetComponent<RectTransform>();
            RectTransform mrkt  = shotMarker.GetComponent<RectTransform>();
            mrkt.anchoredPosition = new Vector2(rt.rect.width * normalized, 0f);
        }
    }
}
