using UnityEngine;

public class PersistentSingleton2 : MonoBehaviour
{
    private static PersistentSingleton2 instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}