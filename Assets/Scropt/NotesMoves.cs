using UnityEngine;
using UnityEngine.UI;

public class NotesMoves : MonoBehaviour
{
    [SerializeField]
    Transform judgementArea;
    [SerializeField]
    float speed;
    [SerializeField]
    Sprite[] texture;
    [SerializeField]
    int id;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        id = Random.Range(0, texture.Length);
        judgementArea = GameObject.FindWithTag("judgementArea").transform;
        this.GetComponent<SpriteRenderer>().sprite = texture[id];
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 current = transform.position;
        Vector3 target = judgementArea.position;
        float step = speed * Time.deltaTime;    // 1ƒtƒŒ[ƒ€‚ÌÅ‘åˆÚ“®‹——£

        transform.position = Vector3.MoveTowards(current, target, step);

        if (transform.position == target) 
        {
            Destroy(gameObject);
        }
    }
}
