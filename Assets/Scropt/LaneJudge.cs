using System.Collections.Generic;
using UnityEngine;

public class LaneJudge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform judgePoint;   // 未指定なら自分を使用

    [Header("Windows (world distance)")]
    [SerializeField] float perfectDist = 0.08f;
    [SerializeField] float greatDist = 0.16f;
    [SerializeField] float goodDist = 0.24f;

    // 判定対象キュー（このレーンの判定エリアに入っているノーツ）
    [SerializeField]
    readonly List<NotesMoves> queue = new();

    void Awake()
    {
        if (!judgePoint) judgePoint = transform;
    }

    // UIボタンの OnClick から呼ぶ
    public void OnTap()
    {
        CleanupNulls();
        if (queue.Count == 0) return;

        var note = queue[0];
        float d = Vector2.Distance(note.transform.position, judgePoint.position);

        if (d <= perfectDist) Hit(note, "Perfect");
        else if (d <= greatDist) Hit(note, "Great");
        else if (d <= goodDist) Hit(note, "Good");
        else Miss(note); // 触れてはいるが離れすぎ
    }

    void Hit(NotesMoves note, string rank)
    {
        Debug.Log(rank);
        queue.Remove(note);
        Destroy(note.gameObject);
    }

    void Miss(NotesMoves note)
    {
        Debug.Log("Miss");
        queue.Remove(note);
        Destroy(note.gameObject);
    }

    void CleanupNulls() => queue.RemoveAll(n => n == null);

    // 判定エリア入退場の管理（判定ラインに付けた2D Trigger）
    void OnTriggerEnter2D(Collider2D other)
    {
        var n = other.GetComponent<NotesMoves>();
        if (n) queue.Add(n);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var n = other.GetComponent<NotesMoves>();
        if (!n) return;
        if (queue.Contains(n))
        {
            // 触れたまま叩かずに出た → ミス
            Debug.Log("Miss");
            queue.Remove(n);
            Destroy(n.gameObject);
        }
    }
}
