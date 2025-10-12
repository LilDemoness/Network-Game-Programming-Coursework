using UnityEngine;

public class LerpBetweenPoints : MonoBehaviour
{
    [SerializeField] private Vector3 _point1, _point2;
    [SerializeField] private float _speed;
    private bool _isTargetingFirstPoint = false;
    private Vector3 _currentTarget => _isTargetingFirstPoint ? _point1 : _point2;


    private void Update()
    {
        transform.position = Vector3.MoveTowards(transform.position, _currentTarget, _speed * Time.deltaTime);
        if (transform.position == _currentTarget)
            _isTargetingFirstPoint = !_isTargetingFirstPoint;
    }
}
