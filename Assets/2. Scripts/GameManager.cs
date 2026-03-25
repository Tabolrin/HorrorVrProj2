using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject RayInteractor;
    public static GameManager Instance { get; private set; }
    [field: SerializeField] public Transform Player { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
