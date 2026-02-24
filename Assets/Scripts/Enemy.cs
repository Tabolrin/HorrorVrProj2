using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private float _health = 10f;
    private bool _isDead = false;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeHit()
    {
        _health -= 1f;
        
        if (_health <= 0f)
            Die();
    }
    
    private void Die()
    {
        _isDead = true;
        gameObject.SetActive(false);
    }
}
