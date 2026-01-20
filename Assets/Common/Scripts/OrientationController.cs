// Created: 2025-02-14
// Author: gpt-5.2-codex

using UnityEngine;

/// <summary>画面の向きを縦固定にする。</summary>
public class OrientationController : MonoBehaviour
{
    private const bool ALLOW_PORTRAIT = true;
    private const bool ALLOW_PORTRAIT_UPSIDE_DOWN = false;
    private const bool ALLOW_LANDSCAPE_LEFT = false;
    private const bool ALLOW_LANDSCAPE_RIGHT = false;

    /// <summary>初期化時に縦固定設定を適用する。</summary>
    void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = ALLOW_PORTRAIT;
        Screen.autorotateToPortraitUpsideDown = ALLOW_PORTRAIT_UPSIDE_DOWN;
        Screen.autorotateToLandscapeLeft = ALLOW_LANDSCAPE_LEFT;
        Screen.autorotateToLandscapeRight = ALLOW_LANDSCAPE_RIGHT;
    }
}
