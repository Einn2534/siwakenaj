// Created: 2025-02-14
// Updated: 2026-03-12
// Author: Einn

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>ステージ選択画面の挙動を管理する。</summary>
public class StageSelectController : MonoBehaviour
{
    private const string MAIN_SCENE = "Main";
    private const string TITLE_SCENE = "Title";
    private const string STATUS_UNLOCKED = "Unlocked";
    private const string STATUS_LOCKED = "Locked";
    private const string STATUS_COMING_SOON = "ComingSoon";
    private const int DEFAULT_STAGE_NUMBER = 1;

    [SerializeField]
    SwipeSnapController swipeSnapController;

    [SerializeField]
    StageCardView[] stageCardViews;

    [SerializeField]
    StageInfo[] stageInfos;

    [SerializeField]
    Button playButton;

    int selectedStageNumber = DEFAULT_STAGE_NUMBER;

    /// <summary>開始時にカード情報を更新し、前回選択ステージへ移動する。</summary>
    IEnumerator Start()
    {
        if (swipeSnapController)
        {
            swipeSnapController.onPageChanged += on_selection_changed;
        }

        update_cards();
        yield return null;

        int lastStageNumber = SaveService.get_last_stage();
        int startIndex = get_index_from_stage_number(lastStageNumber);
        if (swipeSnapController)
        {
            swipeSnapController.jump_to_index(startIndex);
        }

        apply_selection_index(startIndex);
    }

    /// <summary>破棄時にイベント購読を解除する。</summary>
    void OnDestroy()
    {
        if (swipeSnapController)
        {
            swipeSnapController.onPageChanged -= on_selection_changed;
        }
    }

    /// <summary>スナップ選択が変わった時の処理。</summary>
    /// <param name="index">選択中のインデックス。</param>
    public void on_selection_changed(int index)
    {
        apply_selection_index(index);
    }

    /// <summary>プレイボタン押下時にメインへ遷移する。</summary>
    public void on_play_pressed()
    {
        if (!can_play_selected_stage())
        {
            return;
        }

        GameSession.set_stage(selectedStageNumber);
        SaveService.set_last_stage(selectedStageNumber);
        SaveService.save();
        SceneManager.LoadScene(MAIN_SCENE);
    }

    /// <summary>戻るボタン押下時にタイトルへ遷移する。</summary>
    public void on_back_pressed()
    {
        SceneManager.LoadScene(TITLE_SCENE);
    }

    /// <summary>選択中のステージ番号を取得する。</summary>
    /// <returns>1 から始まるステージ番号。</returns>
    public int get_selected_stage_number()
    {
        return selectedStageNumber;
    }

    /// <summary>選択されたインデックスを適用する。</summary>
    /// <param name="index">選択インデックス。</param>
    void apply_selection_index(int index)
    {
        if (stageInfos == null || stageInfos.Length == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(index, 0, stageInfos.Length - 1);
        StageInfo info = stageInfos[clampedIndex];
        selectedStageNumber = info.stageNumber;
        SaveService.set_last_stage(selectedStageNumber);
        SaveService.save();

        update_cards();
        if (playButton)
        {
            playButton.interactable = can_play_selected_stage();
        }
    }

    /// <summary>カード表示内容を更新する。</summary>
    void update_cards()
    {
        if (stageInfos == null || stageCardViews == null)
        {
            return;
        }

        int count = Mathf.Min(stageInfos.Length, stageCardViews.Length);
        for (int i = 0; i < count; i += 1)
        {
            StageInfo info = stageInfos[i];
            int bestScore = SaveService.get_best_score(info.stageNumber);
            bool isUnlocked = get_unlock_state(i);
            string status = get_status_label(info, isUnlocked);
            stageCardViews[i].set_data(info.stageNumber, info.targetScore, bestScore, status);
        }
    }

    /// <summary>選択中ステージがプレイ可能か確認する。</summary>
    /// <returns>プレイ可能なら true。</returns>
    bool can_play_selected_stage()
    {
        int index = get_index_from_stage_number(selectedStageNumber);
        if (index < 0 || index >= stageInfos.Length)
        {
            return false;
        }

        StageInfo info = stageInfos[index];
        return info.isImplemented && get_unlock_state(index);
    }

    /// <summary>選択中ステージの解放状態を取得する。</summary>
    /// <param name="index">対象インデックス。</param>
    /// <returns>解放されていれば true。</returns>
    bool get_unlock_state(int index)
    {
        if (stageInfos == null || stageInfos.Length == 0)
        {
            return false;
        }

        StageInfo info = stageInfos[index];
        if (!info.isImplemented)
        {
            return false;
        }

        if (info.stageNumber == DEFAULT_STAGE_NUMBER)
        {
            return true;
        }

        int previousIndex = Mathf.Clamp(index - 1, 0, stageInfos.Length - 1);
        StageInfo previousInfo = stageInfos[previousIndex];
        int previousBest = SaveService.get_best_score(previousInfo.stageNumber);
        return previousBest >= previousInfo.targetScore;
    }

    /// <summary>ステージ番号からインデックスを取得する。</summary>
    /// <param name="stageNumber">1 から始まるステージ番号。</param>
    /// <returns>該当インデックス。見つからなければ 0。</returns>
    int get_index_from_stage_number(int stageNumber)
    {
        if (stageInfos == null || stageInfos.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < stageInfos.Length; i += 1)
        {
            if (stageInfos[i].stageNumber == stageNumber)
            {
                return i;
            }
        }

        return 0;
    }

    /// <summary>カード表示用の状態ラベルを取得する。</summary>
    /// <param name="info">ステージ情報。</param>
    /// <param name="isUnlocked">解放状態。</param>
    /// <returns>状態ラベル。</returns>
    string get_status_label(StageInfo info, bool isUnlocked)
    {
        if (!info.isImplemented)
        {
            return STATUS_COMING_SOON;
        }

        return isUnlocked ? STATUS_UNLOCKED : STATUS_LOCKED;
    }
}
