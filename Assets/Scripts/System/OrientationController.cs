// Created: 2025-02-14
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;

/// <summary>画面の向きを縦向きに固定する。</summary>
public class OrientationController : MonoBehaviour
{
    /// <summary>初期化時に画面の向きをポートレートへ固定する。</summary>
    void Awake()
    {
        Screen.orientation = ScreenOrientation.Portrait;
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
    }
}
