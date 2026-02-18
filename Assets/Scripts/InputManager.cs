using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionReference _shootAction;    
    [SerializeField] private Pistol _pistol;
    
    void Awake()
    {
        _shootAction.action.performed += OnShoot;
    }
    
    private void OnShoot(InputAction.CallbackContext context)
    {
        _pistol.Shoot();
    }
}
