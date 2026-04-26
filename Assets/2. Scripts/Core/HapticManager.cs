using UnityEngine;
using UnityEngine.XR;

public enum HapticType { Hit, Miss, MeleeHit }

/// <summary>
/// Singleton that plays haptic feedback via the XR Input system.
/// All haptic calls in the project route through here for centralized tuning.
/// Uses UnityEngine.XR.InputDevices - no XRI dependency.
/// </summary>
public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Hit - shot connects")]
    [Range(0f, 1f)] public float hitAmplitude = 0.8f;
    public float hitDuration = 0.1f;

    [Header("Melee Hit")]
    [Range(0f, 1f)] public float meleeAmplitude = 1f;
    public float meleeDuration = 0.15f;

    [Header("Miss / Fire")]
    [Range(0f, 1f)] public float missAmplitude = 0.2f;
    public float missDuration = 0.05f;

    private InputDevice _rightDevice;
    private InputDevice _leftDevice;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        InputDevices.deviceConnected    += OnDeviceChanged;
        InputDevices.deviceDisconnected += OnDeviceChanged;
        RefreshDevices();
    }

    private void OnDisable()
    {
        InputDevices.deviceConnected    -= OnDeviceChanged;
        InputDevices.deviceDisconnected -= OnDeviceChanged;
    }

    private void OnDeviceChanged(InputDevice _) => RefreshDevices();

    private void RefreshDevices()
    {
        _rightDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        _leftDevice  = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
    }

    /// <summary>Sends a haptic impulse to the specified hand controller.</summary>
    public void Play(XRNode hand, HapticType type)
    {
        float amp, dur;
        switch (type)
        {
            case HapticType.MeleeHit: amp = meleeAmplitude; dur = meleeDuration; break;
            case HapticType.Hit:      amp = hitAmplitude;   dur = hitDuration;   break;
            default:                  amp = missAmplitude;  dur = missDuration;  break;
        }

        InputDevice device = hand == XRNode.RightHand ? _rightDevice : _leftDevice;
        if (device.isValid)
            device.SendHapticImpulse(0, amp, dur);
    }
}