// Created: 2024-05-25
// Author: gpt-5-codex

using System.Collections.Generic;
using UnityEngine;

/// <summary>Tracks global gameplay metrics such as score and combo.</summary>
public class GameManager : MonoBehaviour
{
    private const int MissComboPenalty = 0;
    private const int PerfectScore = 300;
    private const int GreatScore = 200;
    private const int GoodScore = 100;
    private const int MissScore = 0;

    static readonly Dictionary<JudgementRank, int> rankScores = new()
    {
        { JudgementRank.Perfect, PerfectScore },
        { JudgementRank.Great, GreatScore },
        { JudgementRank.Good, GoodScore },
        { JudgementRank.Miss, MissScore }
    };

    public static GameManager Instance { get; private set; }

    [SerializeField]
    int score;

    [SerializeField]
    int combo;

    [SerializeField]
    int maxCombo;

    /// <summary>Gets the current accumulated score.</summary>
    public int Score => score;

    /// <summary>Gets the current combo count.</summary>
    public int Combo => combo;

    /// <summary>Gets the highest combo achieved.</summary>
    public int MaxCombo => maxCombo;

    /// <summary>Prepares the singleton instance.</summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Updates metrics according to the supplied judgement.</summary>
    /// <param name="rank">Judgement received from a lane.</param>
    public void report_judgement(JudgementRank rank)
    {
        score += rankScores[rank];

        if (rank == JudgementRank.Miss)
        {
            combo = MissComboPenalty;
            return;
        }

        combo += 1;
        if (combo > maxCombo)
        {
            maxCombo = combo;
        }
    }
}
