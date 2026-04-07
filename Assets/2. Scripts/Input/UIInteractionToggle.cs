using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles between game mode and UI interaction mode on a button press.
/// In UI mode: Ray Interactor is enabled, shooting and melee are disabled.
/// Attach to RightHand. Wire _toggleUIAction to XRI Right/Primary Button.
/// </summary>
public class UIInteractionToggle : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _toggleUIAction;

    [Header("References")]
    [SerializeField] private GameObject  _rayInteractor;
    [SerializeField] private InputManager _inputManager;
    [SerializeField] private MonoBehaviour _gunMeleeHit;

    public bool IsUIMode { get; private set; }

    private void OnEnable()
    {
        _toggleUIAction.action.Enable();
        _toggleUIAction.action.performed += OnToggle;
    }

    private void OnDisable()
    {
        _toggleUIAction.action.performed -= OnToggle;
        _toggleUIAction.action.Disable();
    }

    private void Start() => SetUIMode(false);

    private void OnToggle(InputAction.CallbackContext ctx) => SetUIMode(!IsUIMode);

    private void SetUIMode(bool uiMode)
    {
        IsUIMode = uiMode;
        if (_rayInteractor) _rayInteractor.SetActive(uiMode);
        if (_inputManager)  _inputManager.enabled  = !uiMode;
        if (_gunMeleeHit)   _gunMeleeHit.enabled   = !uiMode;
    }
}