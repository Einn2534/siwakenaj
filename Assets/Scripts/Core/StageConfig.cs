// Created: 2026-02-26
// Author: Einn

using UnityEngine;

/// <summary>ステージ設定値をまとめたデータ。</summary>
[System.Serializable]
public class StageConfig
{
    /// <summary>クリアに必要な目標スコア。</summary>
    public int targetScore;

    /// <summary>許容されるミス回数。</summary>
    public int missLimit;

    /// <summary>車の移動速度（PlayZone幅/秒）。</summary>
    public float carSpeed;

    /// <summary>スポーン間隔（秒）。</summary>
    public float spawnInterval;

    /// <summary>ライトトラック出現の重み。</summary>
    public int weightLightTruck;

    /// <summary>コンパクトカー出現の重み。</summary>
    public int weightCompactCar;

    /// <summary>スポーツカー出現の重み。</summary>
    public int weightSportsCar;

    /// <summary>デフォルト値を元に初期化した設定を生成する。</summary>
    /// <param name="targetScoreValue">目標スコア。</param>
    /// <param name="missLimitValue">許容ミス回数。</param>
    /// <param name="carSpeedValue">車速度。</param>
    /// <param name="spawnIntervalValue">スポーン間隔。</param>
    /// <param name="defaultWeight">車種出現の既定重み。</param>
    /// <returns>生成された設定。</returns>
    public static StageConfig create_default(
        int targetScoreValue,
        int missLimitValue,
        float carSpeedValue,
        float spawnIntervalValue,
        int defaultWeight)
    {
        return new StageConfig
        {
            targetScore = targetScoreValue,
            missLimit = missLimitValue,
            carSpeed = carSpeedValue,
            spawnInterval = spawnIntervalValue,
            weightLightTruck = defaultWeight,
            weightCompactCar = defaultWeight,
            weightSportsCar = defaultWeight
        };
    }
}
