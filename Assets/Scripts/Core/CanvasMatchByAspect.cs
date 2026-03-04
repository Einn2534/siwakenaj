using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class CanvasMatchByAspect : MonoBehaviour
{
    // 基準アスペクト(1080/1920 = 0.5625)
    const float REF_ASPECT = 1080f / 1920f;

    [Range(0f, 1f)] public float wideMatch = 0.0f;   // 幅寄り
    [Range(0f, 1f)] public float tallMatch = 1.0f;   // 高さ寄り

    void Awake()
    {
        var cs = GetComponent<CanvasScaler>();
        float aspect = (float)Screen.width / Screen.height;

        // 端末が基準より「横に広い」→ width寄り、縦長→ height寄り
        cs.matchWidthOrHeight = (aspect > REF_ASPECT) ? wideMatch : tallMatch;
    }
}