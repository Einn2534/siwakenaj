// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>車体を左方向へ移動させ、画面外で破棄する。</summary>
public class CarController : MonoBehaviour
{
    private const float MINIMUM_SPEED = 0.1f;
    private const float DEFAULT_LEFT_LIMIT = -15f;

    [SerializeField]
    float speed = 5f;

    [SerializeField]
    float leftLimit = DEFAULT_LEFT_LIMIT;

    [SerializeField]
    CarType carType;


    [SerializeField]
    ScoreManager scoreManager;


    void Awake()
    {
        if (!scoreManager)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
    }

    /// <summary>設定されている車種を取得する。</summary>
    /// <returns>シリアライズ済みの車種。</returns>
    public CarType get_car_type()
    {
        return carType;
    }

    /// <summary>毎フレーム車体を移動させる。</summary>
    void Update()
    {
        Vector3 position = transform.position;
        position += Vector3.left * (speed * Time.deltaTime);
        transform.position = position;

        if (transform.position.x <= leftLimit)
        {
            // 時間切れミス扱い
            if (scoreManager != null)
            {
                scoreManager.apply_miss();
            }
            Destroy(gameObject);
        }

    }

    /// <summary>シリアライズされた値を安全な範囲に補正する。</summary>
    void OnValidate()
    {
        speed = Mathf.Max(speed, MINIMUM_SPEED);
    }
}
