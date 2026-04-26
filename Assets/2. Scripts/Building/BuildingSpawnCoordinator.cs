using UnityEngine;

/// <summary>
/// Coordinates left and right lane spawners on a shared interval.
/// Each lane independently rolls a skip chance, but both lanes
/// are never allowed to skip on the same interval.
/// </summary>
public class BuildingSpawnCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuildingSpawner _leftSpawner;
    [SerializeField] private BuildingSpawner _rightSpawner;

    [Header("Timing")]
    [SerializeField] private float _spawnInterval = 3f;

    [Header("Skip Chance")]
    [Tooltip("Per-lane probability of skipping a spawn. Max 0.5 to ensure at least one lane always spawns.")]
    [SerializeField] [Range(0f, 0.5f)] private float _skipChance = 0.2f;

    private float _timer;
    private bool  _active;

    public void StartSpawning()
    {
        _active = true;
        _timer  = 0f;
    }

    public void StopSpawning() => _active = false;

    private void Update()
    {
        if (!_active) return;

        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;
        _timer -= _spawnInterval;

        bool skipLeft  = Random.value < _skipChance;
        bool skipRight = Random.value < _skipChance;

        if (skipLeft && skipRight)
        {
            if (Random.value < 0.5f) skipLeft  = false;
            else                     skipRight = false;
        }

        if (!skipLeft)  _leftSpawner?.SpawnNext();
        if (!skipRight) _rightSpawner?.SpawnNext();
    }
}