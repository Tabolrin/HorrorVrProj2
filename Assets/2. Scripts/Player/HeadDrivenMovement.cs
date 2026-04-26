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

    [Tooltip("Max drift in meters from start position on each axis.")]
    [SerializeField] private float _maxDriftX = 1.5f;
    [SerializeField] private float _maxDriftZ = 0.8f;

    private Vector3 _prevHeadLocalPos;
    private Vector3 _startPosition;
    private bool    _initialized;

    private void Start()
    {
        if (_head == null)
            _head = Camera.main.transform;

        _startPosition    = transform.position;
        _prevHeadLocalPos = _head.localPosition;
    }

    private void Update()
    {
        if (_head == null) return;

        if (!_initialized)
        {
            _prevHeadLocalPos = _head.localPosition;
            _initialized = true;
            return;
        }

        Vector3 delta     = _head.localPosition - _prevHeadLocalPos;
        _prevHeadLocalPos = _head.localPosition;

        Vector3 newPos = transform.position + new Vector3(delta.x, 0f, delta.z) * _movementScale;

        newPos.x = Mathf.Clamp(newPos.x, _startPosition.x - _maxDriftX, _startPosition.x + _maxDriftX);
        newPos.z = Mathf.Clamp(newPos.z, _startPosition.z - _maxDriftZ, _startPosition.z + _maxDriftZ);

        transform.position = newPos;
    }
}