using UnityEngine;

/// <summary>
/// Coordinates both lane spawners.
/// On each interval: each spawner independently rolls a skip chance,
/// but if both would skip, one is forced to spawn.
/// </summary>
public class BuildingSpawnCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BuildingSpawner _leftSpawner;
    [SerializeField] private BuildingSpawner _rightSpawner;

    [Header("Timing")]
    [SerializeField] private float _spawnInterval = 3f;

    [Header("Skip Chance")]
    [Tooltip("0 = never skip, 1 = always skip. Applied independently per lane.")]
    [SerializeField] [Range(0f, 0.5f)] private float _skipChance = 0.2f;

    private float _timer;
    private bool  _active;

    // Called by GameStateManager
    public void StartSpawning() => _active = true;
    public void StopSpawning()  => _active = false;

    private void Update()
    {
        if (!_active) return;

        _timer += Time.deltaTime;
        if (_timer < _spawnInterval) return;
        _timer -= _spawnInterval;

        bool skipLeft  = Random.value < _skipChance;
        bool skipRight = Random.value < _skipChance;

        // Never both skip
        if (skipLeft && skipRight)
        {
            if (Random.value < 0.5f) skipLeft  = false;
            else                      skipRight = false;
        }

        if (!skipLeft)  _leftSpawner.SpawnNext();
        if (!skipRight) _rightSpawner.SpawnNext();
    }
}
