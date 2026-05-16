using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectLetterbox : MonoBehaviour
{
    [SerializeField]
    private float targetAspect = 9f / 16f;

    [SerializeField]
    private Color letterboxColor = new Color(0.19215687f, 0.3019608f, 0.4745098f, 1f);

    private Camera cam;
    private Camera clearCamera;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        letterboxColor = cam.backgroundColor;
        CreateClearCamera();
        Apply();
    }

    private void OnDestroy()
    {
        if (clearCamera != null)
        {
            Destroy(clearCamera.gameObject);
        }
    }

    private void OnPreCull()
    {
        Apply();
    }

    private void Apply()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1f)
        {
            float y = (1f - scaleHeight) * 0.5f;
            cam.rect = new Rect(0f, y, 1f, scaleHeight);
        }
        else
        {
            float scaleWidth = 1f / scaleHeight;
            float x = (1f - scaleWidth) * 0.5f;
            cam.rect = new Rect(x, 0f, scaleWidth, 1f);
        }

        if (clearCamera != null)
        {
            clearCamera.backgroundColor = letterboxColor;
            clearCamera.depth = cam.depth - 1f;
        }
    }

    private void CreateClearCamera()
    {
        GameObject clearObject = new($"{name} Letterbox Clear Camera");
        clearObject.transform.SetParent(transform, false);
        clearObject.hideFlags = HideFlags.HideAndDontSave;

        clearCamera = clearObject.AddComponent<Camera>();
        clearCamera.clearFlags = CameraClearFlags.SolidColor;
        clearCamera.backgroundColor = letterboxColor;
        clearCamera.cullingMask = 0;
        clearCamera.depth = cam.depth - 1f;
        clearCamera.rect = new Rect(0f, 0f, 1f, 1f);
        clearCamera.orthographic = cam.orthographic;
        clearCamera.nearClipPlane = cam.nearClipPlane;
        clearCamera.farClipPlane = cam.farClipPlane;
        clearCamera.allowHDR = cam.allowHDR;
        clearCamera.allowMSAA = cam.allowMSAA;
        clearCamera.targetDisplay = cam.targetDisplay;
        clearCamera.targetTexture = cam.targetTexture;
    }
}
