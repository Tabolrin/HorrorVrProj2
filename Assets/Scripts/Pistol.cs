using UnityEngine;

public class Pistol : MonoBehaviour
{
    [SerializeField] private GunProperties _gunProperties;
    [SerializeField] private Transform _firePoint;

    private float _lastFireTime = 0f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void Shoot()
    {
        Debug.Log("Shoot!");
        RaycastHit hitInfo;

        if (Physics.Raycast(_firePoint.position, transform.forward, out hitInfo, _gunProperties._range, _gunProperties.enemyLayer))
        {
            OnEnemyHit(hitInfo.collider.GetComponent<Enemy>());
            
        }
    }
    
    private void OnEnemyHit(Enemy enemy)
    {
        enemy.TakeHit();
    }
}
