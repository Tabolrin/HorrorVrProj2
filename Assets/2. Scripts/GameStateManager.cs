using System.Collections;
using UnityEngine;

public enum GameState { Idle, Playing, SongEnded, Results }

/// <summary>
/// Central game flow controller.
/// Subscribe to BeatDetector's FMOD song-end callback and drive state transitions.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BeatDetector _beatDetector;
    [SerializeField] private BuildingSpawnCoordinator _coordinator;
    [SerializeField] private ResultsUI _resultsUI;

    [Header("Timing")]
    [Tooltip("Seconds after song ends before showing results screen.")]
    [SerializeField] private float _postSongDelay = 3f;

    public GameState State { get; private set; } = GameState.Idle;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Auto-start; replace with a menu trigger if needed
        StartGame();
    }

    public void StartGame()
    {
        State = GameState.Playing;
        _coordinator?.StartSpawning();
        _beatDetector?.PlayMusic();

        // Poll for song end each frame (FMOD EventInstance state check)
        StartCoroutine(WatchForSongEnd());
    }

    private IEnumerator WatchForSongEnd()
    {
        yield return null; // skip frame 1 so IsPlaying has time to be set true
        while (State == GameState.Playing)
        {
            if (_beatDetector != null && !_beatDetector.IsPlaying)
            {
                OnSongEnded();
                yield break;
            }
            yield return null;
        }
    }

    private void OnSongEnded()
    {
        State = GameState.SongEnded;
        _coordinator?.StopSpawning();
        StartCoroutine(DelayThenResults());
    }

    private IEnumerator DelayThenResults()
    {
        yield return new WaitForSeconds(_postSongDelay);
        State = GameState.Results;

        int score  = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;
        int stars  = StarRatingCalculator.Calculate(score);
        _resultsUI?.Show(score, stars);
    }
}
