using System.Collections;
using UnityEngine;

public enum GameState { Idle, Playing, SongEnded, Results }

/// <summary>
/// Drives the top-level game flow: start -> playing -> song end -> results.
/// Coordinates BeatDetector, BuildingSpawnCoordinator, and ResultsUI.
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BeatDetector _beatDetector;
    [SerializeField] private BuildingSpawnCoordinator _coordinator;
    [SerializeField] private ResultsUI _resultsUI;

    [Header("Timing")]
    [Tooltip("Seconds after song ends before showing the results screen.")]
    [SerializeField] private float _postSongDelay = 3f;

    public GameState State { get; private set; } = GameState.Idle;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start() => StartGame();

    /// <summary>Begins music, spawning, and song-end polling.</summary>
    public void StartGame()
    {
        State = GameState.Playing;
        _coordinator?.StartSpawning();
        _beatDetector?.PlayMusic();
        StartCoroutine(WatchForSongEnd());
    }

    /// <summary>
    /// Polls BeatDetector.IsPlaying each frame.
    /// One frame is skipped on start to allow IsPlaying to be set.
    /// </summary>
    private IEnumerator WatchForSongEnd()
    {
        yield return null;
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

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;
        int stars = StarRatingCalculator.Calculate(score);
        _resultsUI?.Show(score, stars);
    }
}