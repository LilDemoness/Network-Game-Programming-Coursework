using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class DebugTester : MonoBehaviour
{
    public static DebugTester Instance;
    private ObjectPool<LineRenderer> _lineRendererPool;
    [SerializeField] private LineRenderer _lineRendererPrefab;


    private void Awake()
    {
        Instance = this;

        _lineRendererPool = new ObjectPool<LineRenderer>(createFunc: CreateLineRenderer, actionOnGet: GetLineRenderer, actionOnRelease: ReleaseLineRenderer);
    }
    private LineRenderer CreateLineRenderer() => Instantiate(_lineRendererPrefab);
    private void GetLineRenderer(LineRenderer renderer) => renderer.gameObject.SetActive(true);
    private void ReleaseLineRenderer(LineRenderer renderer) => renderer.gameObject.SetActive(false);

    public void DrawRay(Vector3 origin, Vector3 direction, Color color, float duration)
    {
        LineRenderer lineRenderer = _lineRendererPool.Get();

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, origin + direction);
        
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        
        StartCoroutine(ReturnAfterDuration(lineRenderer, duration));
    }
    private IEnumerator ReturnAfterDuration(LineRenderer lineRenderer, float duration)
    {
        yield return new WaitForSeconds(duration);
        _lineRendererPool.Release(lineRenderer);
    }
}
