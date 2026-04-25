using UnityEngine;

public class LoadingSymbol : MonoBehaviour
{
    [SerializeField] private float RotationSpeed = 2f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, 0, -1 * RotationSpeed * Time.deltaTime);
    }
}
