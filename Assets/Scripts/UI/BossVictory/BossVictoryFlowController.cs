using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 勝利流程的協調者：播放擊敗過場文本 → 文本結束後顯示「回到標題畫面」提示。
/// 文字顯示細節交給 <see cref="DialogueSequencePlayer"/>，回標題的實際跳轉
/// 沿用專案既有作法，直接在 Editor 把按鈕綁到 ScenesManager.LoadScene("Menu")，
/// 這裡完全不處理場景載入，只負責串接「文本播完」跟「顯示下一個提示」這兩段。
/// </summary>
public class BossVictoryFlowController : MonoBehaviour
{
    [SerializeField] private GameObject _root;
    [SerializeField] private DialogueSequencePlayer _dialoguePlayer;
    [SerializeField] private GameObject _returnToTitlePanel;

    public void Play(IReadOnlyList<DialogueLine> lines)
    {
        if (_root != null)
            _root.SetActive(true);

        if (_returnToTitlePanel != null)
            _returnToTitlePanel.SetActive(false);

        if (_dialoguePlayer == null)
        {
            Debug.LogWarning("BossVictoryFlowController 缺少 DialogueSequencePlayer，直接顯示回標題提示");
            ShowReturnToTitlePanel();
            return;
        }

        _dialoguePlayer.OnFinished += HandleDialogueFinished;
        _dialoguePlayer.Play(lines);
    }

    private void HandleDialogueFinished()
    {
        _dialoguePlayer.OnFinished -= HandleDialogueFinished;
        ShowReturnToTitlePanel();
    }

    private void ShowReturnToTitlePanel()
    {
        if (_returnToTitlePanel != null)
            _returnToTitlePanel.SetActive(true);
    }
}
