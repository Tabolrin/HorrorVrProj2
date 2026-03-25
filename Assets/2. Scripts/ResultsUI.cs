using UnityEngine;
using TMPro;

/// <summary>
/// End-of-level results screen. Call Show(score, stars) from GameStateManager.
/// Wire the star GameObjects (5 of them) in the inspector — they'll be
/// activated based on rating.
/// </summary>
public class ResultsUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject _panel;

    [Header("Score")]
    [SerializeField] private TextMeshProUGUI _finalScoreText;

    [Header("Stars (assign 5, index 0–4)")]
    [SerializeField] private GameObject[] _starObjects; // length must be 5

    private void Awake()
    {
        if (_panel) _panel.SetActive(false);
    }

    public void Show(int score, int stars)
    {
        if (_panel) _panel.SetActive(true);

        if (_finalScoreText) _finalScoreText.text = $"{score:N0}";

        stars = Mathf.Clamp(stars, 0, 5);
        for (int i = 0; i < _starObjects.Length; i++)
            if (_starObjects[i] != null)
                _starObjects[i].SetActive(i < stars);
    }
}
