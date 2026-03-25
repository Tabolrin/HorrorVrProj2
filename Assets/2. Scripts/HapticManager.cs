using UnityEngine;
using UnityEngine.XR;

public enum HapticType { Hit, Miss, MeleeHit }

public class HapticManager : MonoBehaviour
{
    public static HapticManager Instance { get; private set; }

    [Header("Hit (shot connects)")]
    [Range(0f, 1f)] public float hitAmplitude = 0.8f;
    public float hitDuration = 0.1f;

    [Header("Melee Hit")]
    [Range(0f, 1f)] public float meleeAmplitude = 1f;
    public float meleeDuration = 0.15f;

    [Header("Miss / Fire (shot only)")]
    [Range(0f, 1f)] public float missAmplitude = 0.2f;
    public float missDuration = 0.05f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void Play(XRNode hand, HapticType type)
    {
        float amp, dur;
        switch (type)
        {
            case HapticType.MeleeHit: amp = meleeAmplitude; dur = meleeDuration; break;
            case HapticType.Hit:      amp = hitAmplitude;   dur = hitDuration;   break;
            default:                  amp = missAmplitude;  dur = missDuration;  break;
        }

        InputDevices.GetDeviceAtXRNode(hand).SendHapticImpulse(0, amp, dur);
    }
}
