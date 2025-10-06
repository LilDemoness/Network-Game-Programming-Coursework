using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Component
{
    private static T s_instance;
    public static T Instance => s_instance;

    public static bool HasInstance = s_instance != null;


    protected virtual void Awake() => InitialiseSingleton();

    protected virtual void InitialiseSingleton()
    {
        if (!Application.isPlaying)
        {
            // Don't set singleton values when not playing.
            return;
        }

        if (s_instance == null)
        {
            s_instance = this as T;
        }
        else
        {
            Debug.LogError($"Error: A {typeof(T).Name} instance already exists: {s_instance.name}.\n Destroying {this.name}", this);
            Destroy(this.gameObject);
        }
    }
}