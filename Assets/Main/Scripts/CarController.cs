// Created: 2025-11-28
// Author: gpt-5.1-codex-max

using UnityEngine;

/// <summary>Moves a car leftwards and removes it when off-screen.</summary>
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

    /// <summary>Gets the identifier for this car type.</summary>
    /// <returns>Enum representing the serialized car type.</returns>
    public CarType get_car_type()
    {
        return carType;
    }

    /// <summary>Moves the car each frame.</summary>
    void Update()
    {
        Vector3 position = transform.position;
        position += Vector3.left * (speed * Time.deltaTime);
        transform.position = position;

        if (transform.position.x <= leftLimit)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Validates serialized fields for safe operation.</summary>
    void OnValidate()
    {
        speed = Mathf.Max(speed, MINIMUM_SPEED);
    }
}
