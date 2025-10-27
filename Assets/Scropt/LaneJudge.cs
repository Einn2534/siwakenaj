// Created: 2024-05-25
// Author: gpt-5-codex

using System.Collections.Generic;
using UnityEngine;

/// <summary>Manages note judgement results within a single lane.</summary>
public class LaneJudge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField]
    Transform judgePoint;

    [Header("Windows (world distance)")]
    [SerializeField]
    float perfectDist = 0.08f;

    [SerializeField]
    float greatDist = 0.16f;

    [SerializeField]
    float goodDist = 0.24f;

    [SerializeField]
    readonly List<NotesMoves> queue = new();

    /// <summary>Initializes the judge point if it has not been assigned.</summary>
    void Awake()
    {
        if (!judgePoint)
        {
            judgePoint = transform;
        }
    }

    /// <summary>Processes a tap input coming from the UI (legacy PascalCase entry point).</summary>
    public void OnTap()
    {
        on_tap();
    }

    /// <summary>Processes a tap input coming from the UI.</summary>
    public void on_tap()
    {
        handle_tap();
    }

    /// <summary>Queues a note when it enters the judgement area.</summary>
    /// <param name="other">Trigger collider that entered the lane.</param>
    void OnTriggerEnter2D(Collider2D other)
    {
        var note = other.GetComponent<NotesMoves>();
        if (note)
        {
            queue.Add(note);
        }
    }

    /// <summary>Handles missed notes when they leave the judgement area.</summary>
    /// <param name="other">Trigger collider that exited the lane.</param>
    void OnTriggerExit2D(Collider2D other)
    {
        var note = other.GetComponent<NotesMoves>();
        if (!note || !queue.Contains(note))
        {
            return;
        }

        resolve_note(note, JudgementRank.Miss);
    }

    /// <summary>Executes the tap judgement workflow.</summary>
    void handle_tap()
    {
        cleanup_nulls();
        if (queue.Count == 0)
        {
            return;
        }

        var note = queue[0];
        float distance = Vector2.Distance(note.transform.position, judgePoint.position);

        if (distance <= perfectDist)
        {
            resolve_note(note, JudgementRank.Perfect);
        }
        else if (distance <= greatDist)
        {
            resolve_note(note, JudgementRank.Great);
        }
        else if (distance <= goodDist)
        {
            resolve_note(note, JudgementRank.Good);
        }
        else
        {
            resolve_note(note, JudgementRank.Miss);
        }
    }

    /// <summary>Removes invalid references from the queue.</summary>
    void cleanup_nulls()
    {
        queue.RemoveAll(note => note == null);
    }

    /// <summary>Finalizes a note judgement and removes the note object.</summary>
    /// <param name="note">Note to be resolved.</param>
    /// <param name="rank">Judgement result.</param>
    void resolve_note(NotesMoves note, JudgementRank rank)
    {
        Debug.Log(rank.ToString());
        queue.Remove(note);
        Destroy(note.gameObject);
        GameManager.Instance?.report_judgement(rank);
    }
}
