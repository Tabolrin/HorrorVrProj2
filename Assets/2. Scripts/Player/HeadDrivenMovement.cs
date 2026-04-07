using UnityEngine;

/// <summary>
/// Translates real-world head/body movement into in-game positional dodging.
/// Reads the camera's local position delta each frame and applies it to the XR rig root.
/// Only horizontal axes (X, Z) are applied - vertical is ignored.
/// Attach to XR Origin Player root.
/// </summary>
public class HeadDrivenMovement : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Assign Main Camera (child of Camera Offset).")]
    [SerializeField] private Transform _head;

    [Header("Tuning")]
    [Tooltip("1 = true 1:1 with real movement. Values above 1 exaggerate the dodge.")]
    [SerializeField] private float _movementScale = 1f;

    [Tooltip("Max drift in meters from world origin on each axis.")]
    [SerializeField] private float _maxDriftX = 1.5f;
    [SerializeField] private float _maxDriftZ = 0.8f;

    private Vector3 _prevHeadLocalPos;

    private void Start()
    {
        if (_head == null)
            _head = Camera.main.transform;
        _prevHeadLocalPos = _head.localPosition;
    }

    private void Update()
    {
        if (_head == null) return;

        // Delta in local rig space - isolates physical head movement from rig movement
        Vector3 delta     = _head.localPosition - _prevHeadLocalPos;
        _prevHeadLocalPos = _head.localPosition;

        Vector3 newPos = transform.position + new Vector3(delta.x, 0f, delta.z) * _movementScale;

        // Clamp relative to world origin (player starts at 0,0,0)
        newPos.x = Mathf.Clamp(newPos.x, -_maxDriftX, _maxDriftX);
        newPos.z = Mathf.Clamp(newPos.z, -_maxDriftZ, _maxDriftZ);

        transform.position = newPos;
    }
}