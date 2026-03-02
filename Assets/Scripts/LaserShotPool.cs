using UnityEngine;
using UnityEngine.Pool;

public class LaserShotPool : MonoBehaviour
{
    [SerializeField] private GameObject _laserShotPrefab;
    [SerializeField] private Transform _laserShotSpawnPoint;
    ObjectPool<GameObject> _laserShotPool;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _laserShotPool = new ObjectPool<GameObject>(
            () => Instantiate(_laserShotPrefab, _laserShotSpawnPoint),
            shot => shot.SetActive(true),
            shot => shot.SetActive(false),
            shot => Destroy(shot),
            true,
            5,
            30
        );
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
}
