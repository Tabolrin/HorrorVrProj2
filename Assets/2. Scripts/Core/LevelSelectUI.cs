using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Level select screen.
/// Instantiates one button per LevelDataSO entry and wires it to AppSceneManager.StartLevel().
///
/// Setup:
///   1. Assign all your LevelDataSO assets to _levels[].
///   2. Create a Button prefab with:
///        - A TextMeshProUGUI child named "Label" for the level name.
///        - (Optional) An Image child named "Preview" for the thumbnail.
///   3. Assign a layout parent (e.g. a Vertical/Grid Layout Group) to _buttonContainer.
///   4. Optionally wire _backButton to go back to the main menu.
/// </summary>
public class LevelSelectUI : MonoBehaviour
{
    [Header("Levels")]
    [Tooltip("All playable levels in the order they should appear.")]
    [SerializeField] private LevelDataSO[] _levels;

    [Header("UI References")]
    [SerializeField] private GameObject  _levelButtonPrefab;
    [SerializeField] private Transform   _buttonContainer;
    [SerializeField] private Button      _backButton;

    private void Awake()
    {
        _backButton?.onClick.AddListener(OnBack);
        BuildButtons();
    }

    private void OnDestroy()
    {
        _backButton?.onClick.RemoveListener(OnBack);
    }

    private void BuildButtons()
    {
        if (_levelButtonPrefab == null || _buttonContainer == null)
        {
            Debug.LogError("[LevelSelectUI] Button prefab or container not assigned.");
            return;
        }

        if (_levels == null || _levels.Length == 0)
        {
            Debug.LogWarning("[LevelSelectUI] No levels assigned.");
            return;
        }

        foreach (var level in _levels)
        {
            if (level == null) continue;

            var go  = Instantiate(_levelButtonPrefab, _buttonContainer);
            var btn = go.GetComponent<Button>();

            // Set label text
            var label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = level.DisplayName;

            // Set preview image if available
            var previewImage = go.transform.Find("Preview")?.GetComponent<Image>();
            if (previewImage != null)
            {
                previewImage.gameObject.SetActive(level.PreviewSprite != null);
                if (level.PreviewSprite != null)
                    previewImage.sprite = level.PreviewSprite;
            }

            // Capture for lambda - necessary to avoid closure-over-loop-variable bug
            var captured = level;
            btn?.onClick.AddListener(() => OnLevelSelected(captured));
        }
    }

    private void OnLevelSelected(LevelDataSO level)
    {
        if (AppSceneManager.Instance == null)
        {
            Debug.LogError("[LevelSelectUI] AppSceneManager not found.");
            return;
        }
        AppSceneManager.Instance.StartLevel(level);
    }

    private void OnBack() => AppSceneManager.Instance?.GoToMainMenu();
}
