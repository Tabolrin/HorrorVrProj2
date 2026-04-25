using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Win / Lose results screen. Lives in its own scene (WinLose).
/// Reads RunResultData from AppSceneManager.LastResult and displays it.
///
/// Setup:
///   - Assign _winPanel and _losePanel (shown/hidden based on IsWin).
///   - Assign _scoreText, _comboText.
///   - Assign 5 star GameObjects in _starObjects[0..4].
///   - Wire _playAgainButton and _mainMenuButton.
/// </summary>
public class WinLoseUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _winPanel;
    [SerializeField] private GameObject _losePanel;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _comboText;

    [Header("Stars - assign 5, index 0 to 4")]
    [SerializeField] private GameObject[] _starObjects;

    [Header("Buttons")]
    [SerializeField] private Button _playAgainButton;
    [SerializeField] private Button _mainMenuButton;

    private void Awake()
    {
        _playAgainButton?.onClick.AddListener(OnPlayAgain);
        _mainMenuButton?.onClick.AddListener(OnMainMenu);
    }

    private void OnDestroy()
    {
        _playAgainButton?.onClick.RemoveListener(OnPlayAgain);
        _mainMenuButton?.onClick.RemoveListener(OnMainMenu);
    }

    private void Start()
    {
        var result = AppSceneManager.Instance?.LastResult;
        if (result == null)
        {
            Debug.LogWarning("[WinLoseUI] No result data found. Showing default state.");
            ShowDefault();
            return;
        }
        Display(result);
    }

    // ── Display ───────────────────────────────────────────────────────────────

    private void Display(RunResultData result)
    {
        if (_winPanel)  _winPanel.SetActive(result.IsWin);
        if (_losePanel) _losePanel.SetActive(!result.IsWin);

        if (_scoreText) _scoreText.text = $"{result.Score:N0}";
        if (_comboText) _comboText.text = $"Best Combo: {result.Combo}";

        int stars = Mathf.Clamp(result.Stars, 0, 5);
        for (int i = 0; i < _starObjects.Length; i++)
            if (_starObjects[i] != null)
                _starObjects[i].SetActive(i < stars);
    }

    private void ShowDefault()
    {
        if (_winPanel)  _winPanel.SetActive(false);
        if (_losePanel) _losePanel.SetActive(true);
        if (_scoreText) _scoreText.text = "0";
        if (_comboText) _comboText.text = "";
        foreach (var s in _starObjects)
            if (s != null) s.SetActive(false);
    }

    // ── Buttons ───────────────────────────────────────────────────────────────

    private void OnPlayAgain() => AppSceneManager.Instance?.PlayAgain();
    private void OnMainMenu()  => AppSceneManager.Instance?.GoToMainMenu();
}
