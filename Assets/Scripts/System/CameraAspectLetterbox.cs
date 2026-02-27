// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;

/// <summary>カメラのアスペクト比をレターボックスで調整する。</summary>
[RequireComponent(typeof(Camera))]
public class CameraAspectLetterbox : MonoBehaviour
{
    [SerializeField]
    private float targetAspect = 9f / 16f;

    private Camera cam;

    /// <summary>初期化時にカメラ参照を取得しレターボックスを適用する。</summary>
    private void Awake()
    {
        cam = GetComponent<Camera>();
        apply();
    }

    /// <summary>レンダリング前にレターボックスを再適用する。</summary>
    private void OnPreCull()
    {
        apply();
    }

    /// <summary>画面サイズに応じてレターボックスまたはピラーボックスを適用する。</summary>
    private void apply()
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
    }
}
