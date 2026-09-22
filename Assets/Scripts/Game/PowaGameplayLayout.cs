using UnityEngine;

// Main's character occupies the free area between the car lane and answer controls.
// Camera-relative sizing also handles render captures at a different aspect ratio.
public sealed class PowaGameplayLayout : MonoBehaviour
{
    private void OnEnable() => Camera.onPreCull += ApplyLayout;
    private void OnDisable() => Camera.onPreCull -= ApplyLayout;
    private void LateUpdate() => ApplyLayout(Camera.main);

    public void ApplyLayout(Camera camera)
    {
        if (camera == null || !camera.CompareTag("MainCamera") || !camera.orthographic) return;
        float height = camera.orthographicSize * 2;
        float width = height * camera.aspect;
        float scale = Mathf.Min(width * .44f, height * .29f) / 5.2f;
        Vector3 center = camera.ViewportToWorldPoint(new Vector3(.27f, .42f, 0));
        transform.position = new Vector3(center.x, center.y, 0);
        transform.localScale = Vector3.one * scale;
    }
}
