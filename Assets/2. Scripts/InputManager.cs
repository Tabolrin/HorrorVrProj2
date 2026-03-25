using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionReference _shootAction;
    [SerializeField] private Pistol _pistol;

    private void OnEnable()
    {
        _shootAction.action.Enable();
        _shootAction.action.performed += OnShoot;
    }

    private void OnDisable()
    {
        _shootAction.action.performed -= OnShoot;
        _shootAction.action.Disable();
    }

    private void OnShoot(InputAction.CallbackContext context)
    {
        _pistol?.Shoot();
    }
}
