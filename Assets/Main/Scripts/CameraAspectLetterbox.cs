using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraAspectLetterbox : MonoBehaviour
{
    [SerializeField] private float targetAspect = 9f / 16f; // 縦16:9 = 9:16

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        Apply();
    }

    private void OnPreCull()
    {
        Apply(); // 画面サイズ変更にも追従
    }

    private void Apply()
    {
        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / targetAspect;

        if (scaleHeight < 1f)
        {
            // 上下に黒帯（レターボックス）
            float y = (1f - scaleHeight) * 0.5f;
            cam.rect = new Rect(0f, y, 1f, scaleHeight);
        }
        else
        {
            // 左右に黒帯（ピラーボックス）
            float scaleWidth = 1f / scaleHeight;
            float x = (1f - scaleWidth) * 0.5f;
            cam.rect = new Rect(x, 0f, scaleWidth, 1f);
        }
    }
}
