using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Win / Lose results screen. Lives in its own scene (WinLose).
/// Reads RunResultData from AppSceneManager.LastResult and displays it.
/// </summary>
public class WinLoseUI : MonoBehaviour
{
    [Header("Result")]
    [SerializeField] private TextMeshProUGUI _resultText;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _gradeText;
    [SerializeField] private TextMeshProUGUI _comboText;

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
            ShowDefault();
            return;
        }
        Display(result);
    }

    private void Display(RunResultData result)
    {
        if (_resultText) _resultText.text = result.IsWin ? "You Win!" : "You Lose!";
        if (_scoreText)  _scoreText.text  = $"{result.Score:N0}";
        if (_gradeText)  _gradeText.text  = result.Grade;
        if (_comboText)  _comboText.text  = $"Best Combo: {result.Combo}";
    }

    private void ShowDefault()
    {
        if (_resultText) _resultText.text = "You Lose!";
        if (_scoreText)  _scoreText.text  = "0";
        if (_gradeText)  _gradeText.text  = "Oh No";
        if (_comboText)  _comboText.text  = "";
    }

    private void OnPlayAgain() => AppSceneManager.Instance?.PlayAgain();
    private void OnMainMenu()  => AppSceneManager.Instance?.GoToMainMenu();
}