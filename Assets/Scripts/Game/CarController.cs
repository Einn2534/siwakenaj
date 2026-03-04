// Created: 2025-11-28
// Updated: 2026-02-26
// Author: Einn

using UnityEngine;

/// <summary>車体を左方向へ移動させ、画面外で破棄する。</summary>
public class CarController : MonoBehaviour
{
    private const float MINIMUM_SPEED = 0.0f;
    private const float MISS_MARGIN_RATIO = 0.02f;

    [SerializeField]
    CarType carType;

    [SerializeField]
    ScoreManager scoreManager;

    float speedWorld;
    float leftEdgeX;
    float missMarginX;
    bool hasMissLine;

    /// <summary>スコアマネージャー参照を初期化する。</summary>
    void Awake()
    {
        if (!scoreManager)
        {
            scoreManager = FindAnyObjectByType<ScoreManager>();
        }
    }

    /// <summary>設定されている車種を取得する。</summary>
    /// <returns>シリアライズ済みの車種。</returns>
    public CarType get_car_type()
    {
        return carType;
    }

    /// <summary>ワールド座標での移動速度を設定する。</summary>
    /// <param name="newSpeedWorld">ワールド空間での速度。</param>
    public void set_speed_world(float newSpeedWorld)
    {
        speedWorld = Mathf.Max(newSpeedWorld, MINIMUM_SPEED);
    }

    /// <summary>時間切れ判定ラインを設定する。</summary>
    /// <param name="newLeftEdgeX">左端ライン。</param>
    /// <param name="playZoneWidth">PlayZoneの幅。</param>
    public void set_miss_line(float newLeftEdgeX, float playZoneWidth)
    {
        leftEdgeX = newLeftEdgeX;
        missMarginX = playZoneWidth * MISS_MARGIN_RATIO;
        hasMissLine = true;
    }

    /// <summary>車の左端座標を取得する。</summary>
    /// <returns>現在の左端座標。</returns>
    public float get_min_x()
    {
        if (BoundsHelper.try_get_bounds(gameObject, out Bounds bounds))
        {
            return bounds.min.x;
        }

        return transform.position.x;
    }

    /// <summary>毎フレーム車体を移動させる。</summary>
    void Update()
    {
        if (speedWorld > 0f)
        {
            Vector3 position = transform.position;
            position += Vector3.left * (speedWorld * Time.deltaTime);
            transform.position = position;
        }

        if (hasMissLine && is_out_of_play_zone())
        {
            if (scoreManager != null)
            {
                scoreManager.apply_miss();
            }

            Destroy(gameObject);
        }
    }

    /// <summary>左端ラインを越えているか判定する。</summary>
    /// <returns>時間切れ判定なら true。</returns>
    bool is_out_of_play_zone()
    {
        float leftMinX = get_min_x();
        return leftMinX < (leftEdgeX - missMarginX);
    }
}
