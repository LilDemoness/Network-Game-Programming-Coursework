using UnityEngine;

public abstract class GFXSection<T> : MonoBehaviour
{
    [SerializeField] protected T AssociatedValue;


    public void Toggle(T activeData) => this.gameObject.SetActive(activeData.Equals(AssociatedValue));
    public void Finalise(T activeData)
    {
        if (!activeData.Equals(AssociatedValue))
        {
            Destroy(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }
}
