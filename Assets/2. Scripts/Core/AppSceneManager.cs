using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that owns all scene transitions and carries
/// cross-scene data (selected level, last run result).
///
/// Scene name constants are set in the inspector so they match your
/// actual Build Settings names without hard-coding strings everywhere.
///
/// Place on a GameObject in your MainMenu scene (or a dedicated Bootstrap
/// scene that loads first). It will survive all subsequent scene loads.
/// </summary>
public class AppSceneManager : MonoBehaviour
{
    public static AppSceneManager Instance { get; private set; }

    [Header("Scene Names — must match Build Settings exactly")]
    [SerializeField] private string _mainMenuScene    = "MainMenu";
    [SerializeField] private string _levelSelectScene = "LevelSelect";
    [SerializeField] private string _winLoseScene     = "WinLose";

    [Header("Transition")]
    [Tooltip("Seconds of fade before and after a scene load. Set 0 to disable.")]
    [SerializeField] private float _fadeDuration = 0.4f;

    // ── Cross-scene data ──────────────────────────────────────────────────────

    /// <summary>Level chosen on the level select screen. Read by GameStateManager.</summary>
    public LevelDataSO SelectedLevel { get; private set; }

    /// <summary>Result of the last completed run. Read by WinLoseUI.</summary>
    public RunResultData LastResult { get; private set; }

    // ── Optional fade canvas ──────────────────────────────────────────────────
    // Leave null to skip fading entirely.
    [Header("Fade (optional)")]
    [SerializeField] private CanvasGroup _fadeOverlay;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void GoToMainMenu()    => StartCoroutine(LoadScene(_mainMenuScene));
    public void GoToLevelSelect() => StartCoroutine(LoadScene(_levelSelectScene));

    /// <summary>
    /// Stores the chosen level then loads the gameplay scene defined on that level.
    /// Called by LevelSelectUI when the player picks a level.
    /// </summary>
    public void StartLevel(LevelDataSO level)
    {
        SelectedLevel = level;
        StartCoroutine(LoadScene(level.GameplaySceneName));
    }

    /// <summary>
    /// Stores the run result then loads the win/lose scene.
    /// Called by GameStateManager when a run ends (win or lose).
    /// </summary>
    public void ShowResults(RunResultData result)
    {
        LastResult = result;
        StartCoroutine(LoadScene(_winLoseScene));
    }

    /// <summary>Replays the last selected level. Called by WinLoseUI "Play Again".</summary>
    public void PlayAgain()
    {
        if (SelectedLevel == null)
        {
            Debug.LogWarning("[AppSceneManager] PlayAgain: no level selected, going to level select.");
            GoToLevelSelect();
            return;
        }
        StartCoroutine(LoadScene(SelectedLevel.GameplaySceneName));
    }

    // ── Internal ──────────────────────────────────────────────────────────────

    private IEnumerator LoadScene(string sceneName)
    {
        yield return StartCoroutine(Fade(1f));

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (op != null && !op.isDone)
            yield return null;

        yield return StartCoroutine(Fade(0f));
    }

    /// <summary>Fades the overlay to targetAlpha if one is assigned.</summary>
    private IEnumerator Fade(float targetAlpha)
    {
        if (_fadeOverlay == null || _fadeDuration <= 0f) yield break;

        float startAlpha = _fadeOverlay.alpha;
        float elapsed    = 0f;

        _fadeOverlay.gameObject.SetActive(true);
        while (elapsed < _fadeDuration)
        {
            elapsed += Time.deltaTime;
            _fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
            yield return null;
        }
        _fadeOverlay.alpha = targetAlpha;

        if (targetAlpha <= 0f)
            _fadeOverlay.gameObject.SetActive(false);
    }
}
