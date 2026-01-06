// Created: 2025-11-28
// Updated: 2025-12-01
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>ステージごとの目標値やパラメーターを管理する。</summary>
public class StageManager : MonoBehaviour
{
    private const int DEFAULT_TARGET_SCORE = 100;
    private const int DEFAULT_ALLOWED_MISSES = 3;
    private const float DEFAULT_CAR_SPEED = 1f;
    private const float DEFAULT_SPAWN_INTERVAL = 1f;
    private const float MINIMUM_SPAWN_INTERVAL = 0.2f;
    private const int DEFAULT_WEIGHT = 1;

    [SerializeField]
    StageConfig[] stageConfigs =
    {
        new StageConfig
        {
            targetScore = DEFAULT_TARGET_SCORE,
            missLimit = DEFAULT_ALLOWED_MISSES,
            carSpeed = DEFAULT_CAR_SPEED,
            spawnInterval = DEFAULT_SPAWN_INTERVAL,
            weightLightTruck = DEFAULT_WEIGHT,
            weightCompactCar = DEFAULT_WEIGHT,
            weightSportsCar = DEFAULT_WEIGHT
        }
    };

    int currentStageIndex;

    /// <summary>選択されたステージの設定を適用する。</summary>
    /// <param name="stageIndex">読み込むステージ番号。</param>
    public void apply_stage(int stageIndex)
    {
        if (stageConfigs == null || stageConfigs.Length == 0)
        {
            currentStageIndex = 0;
            return;
        }

        currentStageIndex = Mathf.Clamp(stageIndex, 0, stageConfigs.Length - 1);
    }

    /// <summary>現在のステージ設定を取得する。</summary>
    /// <returns>現在のステージ設定。</returns>
    public StageConfig get_stage_config()
    {
        if (stageConfigs == null || stageConfigs.Length == 0)
        {
            return StageConfig.CreateDefault(
                DEFAULT_TARGET_SCORE,
                DEFAULT_ALLOWED_MISSES,
                DEFAULT_CAR_SPEED,
                DEFAULT_SPAWN_INTERVAL,
                DEFAULT_WEIGHT);
        }

        return stageConfigs[currentStageIndex];
    }

    /// <summary>現在のステージの目標スコアを取得する。</summary>
    /// <returns>クリアに必要なスコア。</returns>
    public int get_target_score()
    {
        return get_stage_config().targetScore;
    }

    /// <summary>現在のステージで許容されるミス回数を取得する。</summary>
    /// <returns>ゲームオーバーとなるまでのミス上限。</returns>
    public int get_allowed_misses()
    {
        return get_stage_config().missLimit;
    }

    /// <summary>ステージ設定値を規定範囲に補正する。</summary>
    void OnValidate()
    {
        if (stageConfigs == null)
        {
            return;
        }

        foreach (var config in stageConfigs)
        {
            if (config == null)
            {
                continue;
            }

            config.targetScore = Mathf.Max(0, config.targetScore);
            config.missLimit = Mathf.Max(0, config.missLimit);
            config.carSpeed = Mathf.Max(0f, config.carSpeed);
            config.spawnInterval = Mathf.Max(MINIMUM_SPAWN_INTERVAL, config.spawnInterval);
            config.weightLightTruck = Mathf.Max(0, config.weightLightTruck);
            config.weightCompactCar = Mathf.Max(0, config.weightCompactCar);
            config.weightSportsCar = Mathf.Max(0, config.weightSportsCar);
        }
    }
}

/// <summary>ステージ設定値をまとめたデータ。</summary>
[System.Serializable]
public class StageConfig
{
    public int targetScore;
    public int missLimit;
    public float carSpeed;
    public float spawnInterval;
    public int weightLightTruck;
    public int weightCompactCar;
    public int weightSportsCar;

    /// <summary>デフォルト値を元に初期化した設定を生成する。</summary>
    /// <param name="targetScoreValue">目標スコア。</param>
    /// <param name="missLimitValue">許容ミス回数。</param>
    /// <param name="carSpeedValue">車速度。</param>
    /// <param name="spawnIntervalValue">スポーン間隔。</param>
    /// <param name="defaultWeight">車種出現の既定重み。</param>
    /// <returns>生成された設定。</returns>
    public static StageConfig CreateDefault(
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
