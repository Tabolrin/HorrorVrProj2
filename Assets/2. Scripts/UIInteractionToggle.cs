using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attach to RightHand.
/// Toggles UI interaction mode (enables Ray Interactor, disables shooting).
/// Assign the Toggle action to your Primary Button (A).
/// </summary>
public class UIInteractionToggle : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference _toggleUIAction;

    [Header("References")]
    [SerializeField] private GameObject _rayInteractor;       // RightHand/Ray Interactor GO
    [SerializeField] private InputManager _inputManager;      // to disable shooting in UI mode
    [SerializeField] private MonoBehaviour _gunMeleeHit;      // GunMeleeHit to disable in UI mode

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

    private void Start()
    {
        SetUIMode(false);
    }

    private void OnToggle(InputAction.CallbackContext ctx) => SetUIMode(!IsUIMode);

    private void SetUIMode(bool uiMode)
    {
        IsUIMode = uiMode;

        // Ray interactor only active in UI mode
        if (_rayInteractor) _rayInteractor.SetActive(uiMode);

        // Disable shooting and melee in UI mode
        if (_inputManager) _inputManager.enabled = !uiMode;
        if (_gunMeleeHit)  _gunMeleeHit.enabled  = !uiMode;

        Debug.Log($"[UIToggle] UI Mode: {uiMode}");
    }
}
