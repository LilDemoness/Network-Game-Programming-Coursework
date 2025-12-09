using UnityEngine;
using Unity.Netcode;

/// <summary>
///     A NetworkBehaviour Singleton.
/// </summary>
public class NetworkSingleton<T> : NetworkBehaviour where T : Component
{
    private static T s_instance;
    public static T Instance => s_instance;

    //public static bool HasInstance = s_instance != null;  // For some reason this is always returning null. For now, manually perform the check by using 'X.Instance != null'


    protected virtual void Awake() => InitialiseSingleton();
    public override void OnDestroy()
    {
        if (s_instance == this)
            s_instance = null;

        base.OnDestroy();
    }

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
