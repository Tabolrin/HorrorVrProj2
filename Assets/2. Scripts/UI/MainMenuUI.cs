using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu screen.
/// Wire the two buttons in the inspector - no logic beyond delegation to AppSceneManager.
/// Attach to the root Canvas or a panel GameObject in the MainMenu scene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _playButton;
    [SerializeField] private Button _quitButton;

    private void Awake()
    {
        _playButton?.onClick.AddListener(OnPlay);
        _quitButton?.onClick.AddListener(OnQuit);
    }

    private void OnDestroy()
    {
        _playButton?.onClick.RemoveListener(OnPlay);
        _quitButton?.onClick.RemoveListener(OnQuit);
    }

    private void OnPlay()
    {
        if (AppSceneManager.Instance == null)
        {
            Debug.LogError("[MainMenuUI] AppSceneManager not found in scene.");
            return;
        }
        AppSceneManager.Instance.GoToLevelSelect();
    }

    private void OnQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
