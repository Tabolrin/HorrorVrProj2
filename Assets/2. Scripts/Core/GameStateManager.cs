using System.Collections;
using UnityEngine;

public enum GameState { Idle, Playing, SongEnded, Results }

/// <summary>
/// Drives the top-level game flow: start -> playing -> end (win or lose) -> results scene.
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

    private bool _runEnded;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        ApplySelectedLevel();

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

    private void ApplySelectedLevel()
    {
        var level = AppSceneManager.Instance?.SelectedLevel;
        if (level == null)
        {
            Debug.LogWarning("[GameStateManager] No level selected via AppSceneManager. Using BeatDetector's default musicEventPath.");
            return;
        }

        if (_beatDetector != null)
            _beatDetector.musicEventPath = level.FmodEventPath;
    }

    public void StartGame()
    {
        _runEnded = false;
        State     = GameState.Playing;
        _coordinator?.StartSpawning();
        _beatDetector?.PlayMusic();
        StartCoroutine(WatchForSongEnd());
    }

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

        var result = new RunResultData
        {
            IsWin = isWin,
            Score = score,
            Grade = RunResultData.CalculateGrade(score),
            Combo = combo,
            Level = AppSceneManager.Instance?.SelectedLevel
        };

        if (AppSceneManager.Instance != null)
            AppSceneManager.Instance.ShowResults(result);
        else
            Debug.LogWarning("[GameStateManager] AppSceneManager not found - cannot navigate to results.");
    }
}