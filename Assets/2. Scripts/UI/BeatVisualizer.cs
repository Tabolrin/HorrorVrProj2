using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Debug tool for visualizing beat detection during development.
/// Shows a pulsing circle on each beat, a timeline bar filling between beats,
/// and a shot marker showing where the last shot landed on the timeline.
/// Not required for the final build.
/// </summary>
public class BeatVisualizer : MonoBehaviour
{
    [Header("References")]
    public BeatDetector beatDetector;
    public Image beatPulseImage;  // circle that flashes on beat
    public Image beatTimelineBar; // horizontal fill bar, resets each beat
    public Image shotMarker;      // dot showing where last shot landed

    [Header("Colors")]
    public Color colorPerfect = Color.yellow;
    public Color colorGood    = new Color(0f, 1f, 0.5f);
    public Color colorOffBeat = Color.red;
    public Color colorIdle    = new Color(1f, 1f, 1f, 0.3f);

    private float _pulseTimer;
    private const float PulseDuration = 0.12f;

    private RectTransform _barRect;
    private RectTransform _markerRect;

    private void Awake()
    {
        if (beatTimelineBar) _barRect    = beatTimelineBar.GetComponent<RectTransform>();
        if (shotMarker)      _markerRect = shotMarker.GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (beatDetector) beatDetector.OnBeatDetected += HandleBeat;
    }

    private void OnDisable()
    {
        if (beatDetector) beatDetector.OnBeatDetected -= HandleBeat;
    }

    private void HandleBeat()
    {
        _pulseTimer = PulseDuration;
        if (beatPulseImage) beatPulseImage.color = Color.white;
    }

    private void Update()
    {
        UpdatePulse();
        UpdateTimeline();
    }

    private void UpdatePulse()
    {
        if (_pulseTimer > 0f)
        {
            _pulseTimer -= Time.deltaTime;
            float t = _pulseTimer / PulseDuration;
            if (beatPulseImage)
            {
                // Scale up on beat then shrink back to 1
                beatPulseImage.transform.localScale = Vector3.one * (1f + t * 0.5f);
                beatPulseImage.color = Color.Lerp(colorIdle, Color.white, t);
            }
        }
        else if (beatPulseImage)
        {
            beatPulseImage.transform.localScale = Vector3.one;
            beatPulseImage.color = colorIdle;
        }
    }

    private void UpdateTimeline()
    {
        if (beatDetector == null || beatDetector.CurrentBpm <= 0 || beatTimelineBar == null) return;
        float interval   = 60f / beatDetector.CurrentBpm;
        float timeSince  = Time.time - beatDetector.LastBeatTime;
        beatTimelineBar.fillAmount = Mathf.Clamp01(timeSince / interval);
    }

    /// <summary>
    /// Positions the shot marker on the timeline to show timing accuracy.
    /// Call from game code after RegisterHit() to display the result.
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

        if (beatTimelineBar && beatDetector.CurrentBpm > 0 && _barRect != null && _markerRect != null)
        {
            float interval   = 60f / beatDetector.CurrentBpm;
            float normalized = Mathf.Clamp01(score.timeSinceBeat / interval);
            _markerRect.anchoredPosition = new Vector2(_barRect.rect.width * normalized, 0f);
        }
    }
}