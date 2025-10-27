// Created: 2024-05-25
// Author: gpt-5-codex

using UnityEngine;

/// <summary>Moves spawned notes toward the judgement area.</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class NotesMoves : MonoBehaviour
{
    private const string JudgementAreaTag = "judgementArea";
    private const int TextureIndexMin = 0;
    private const float ArrivalThreshold = 0.001f;

    [SerializeField]
    Transform judgementArea;

    [SerializeField]
    float speed = 1f;

    [SerializeField]
    Sprite[] textures;

    [SerializeField]
    int id;

    SpriteRenderer spriteRenderer;

    /// <summary>Caches component references.</summary>
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>Configures the sprite and locates the judgement area.</summary>
    void Start()
    {
        assign_judgement_area();
        apply_random_texture();
    }

    /// <summary>Moves the note towards the target transform.</summary>
    void Update()
    {
        if (!judgementArea)
        {
            return;
        }

        Vector3 current = transform.position;
        Vector3 target = judgementArea.position;
        float step = speed * Time.deltaTime;

        transform.position = Vector3.MoveTowards(current, target, step);

        if (Vector3.Distance(transform.position, target) <= ArrivalThreshold)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>Validates serialized configuration values.</summary>
    void OnValidate()
    {
        speed = Mathf.Max(speed, 0f);
    }

    /// <summary>Fetches the judgement area if no reference is assigned.</summary>
    void assign_judgement_area()
    {
        if (judgementArea)
        {
            return;
        }

        GameObject target = GameObject.FindWithTag(JudgementAreaTag);
        if (target)
        {
            judgementArea = target.transform;
        }
    }

    /// <summary>Applies a random sprite to the note.</summary>
    void apply_random_texture()
    {
        if (textures == null || textures.Length == 0)
        {
            return;
        }

        id = Random.Range(TextureIndexMin, textures.Length);
        spriteRenderer.sprite = textures[id];
    }
}
