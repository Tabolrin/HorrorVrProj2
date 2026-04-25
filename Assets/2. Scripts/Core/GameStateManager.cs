using System.Collections;
using UnityEngine;

public enum GameState { Idle, Playing, SongEnded, Results }

/// <summary>
/// Drives the top-level game flow: start -> playing -> end (win or lose) -> results scene.
///
/// Changes from original:
///   - Reads AppSceneManager.SelectedLevel to configure BeatDetector's FMOD path.
///   - Subscribes to PlayerManager.OnDeath for the lose condition.
///   - On run end, builds a RunResultData and hands it to AppSceneManager.ShowResults()
///     instead of showing an in-scene ResultsUI panel.
///   - ResultsUI reference removed (that UI now lives in its own scene).
/// </summary>
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private BeatDetector             _beatDetector;
    [SerializeField] private BuildingSpawnCoordinator _coordinator;
    [SerializeField] private PlayerManager            _playerManager;

    [Header("Timing")]
    [Tooltip("Seconds after a run ends before transitioning to the results scene.")]
    [SerializeField] private float _postRunDelay = 3f;

    public GameState State { get; private set; } = GameState.Idle;

    private bool _runEnded; // guard against win and lose firing simultaneously

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // Apply the level chosen on the level select screen
        ApplySelectedLevel();

        // Subscribe to player death for the lose condition
        if (_playerManager != null)
            _playerManager.OnDeath.AddListener(OnPlayerDied);
        else
            Debug.LogWarning("[GameStateManager] PlayerManager not assigned - lose condition won't fire.");

        StartGame();
    }

    private void OnDestroy()
    {
        if (_playerManager != null)
            _playerManager.OnDeath.RemoveListener(OnPlayerDied);
    }

    // ── Level Config ──────────────────────────────────────────────────────────

    /// <summary>
    /// Reads the level selected on the level select screen and configures BeatDetector.
    /// Safe if AppSceneManager doesn't exist (e.g. entering Play Mode directly
    /// from the Gameplay scene in the editor - BeatDetector's default path is used).
    /// </summary>
    private void ApplySelectedLevel()
    {
        var level = AppSceneManager.Instance?.SelectedLevel;
        if (level == null)
        {
            Debug.LogWarning("[GameStateManager] No level selected via AppSceneManager. " +
                             "Using BeatDetector's default musicEventPath.");
            return;
        }

        if (_beatDetector != null)
            _beatDetector.musicEventPath = level.FmodEventPath;
    }

    // ── Game Flow ─────────────────────────────────────────────────────────────

    /// <summary>Begins music, spawning, and song-end polling.</summary>
    public void StartGame()
    {
        _runEnded = false;
        State     = GameState.Playing;
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
                OnRunEnded(isWin: true);
                yield break;
            }
            yield return null;
        }
    }

    // ── End Conditions ────────────────────────────────────────────────────────

    /// <summary>Lose condition - fired by PlayerManager.OnDeath.</summary>
    private void OnPlayerDied() => OnRunEnded(isWin: false);

    private void OnRunEnded(bool isWin)
    {
        if (_runEnded) return;
        _runEnded = true;

        State = GameState.SongEnded;
        _coordinator?.StopSpawning();
        _beatDetector?.StopMusic();

        StartCoroutine(DelayThenShowResults(isWin));
    }

    private IEnumerator DelayThenShowResults(bool isWin)
    {
        yield return new WaitForSeconds(_postRunDelay);
        State = GameState.Results;

        int score = ScoreManager.Instance != null ? ScoreManager.Instance.TotalScore : 0;
        int combo = ScoreManager.Instance != null ? ScoreManager.Instance.Combo      : 0;
        int stars = StarRatingCalculator.Calculate(score);

        var result = new RunResultData
        {
            IsWin = isWin,
            Score = score,
            Stars = stars,
            Combo = combo,
            Level = AppSceneManager.Instance?.SelectedLevel
        };

        if (AppSceneManager.Instance != null)
            AppSceneManager.Instance.ShowResults(result);
        else
            Debug.LogWarning("[GameStateManager] AppSceneManager not found - cannot navigate to results.");
    }
}