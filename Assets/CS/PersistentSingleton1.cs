using UnityEngine;

public class PersistentSingleton1 : MonoBehaviour
{
    private static PersistentSingleton1 instance;

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