using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用的「逐字顯示、點擊畫面任意處推進（打字中先秒顯整句、顯示完才推進下一句）、
/// 右上角可跳過整段」對話序列播放器。
/// 只負責文字序列本身的顯示與推進，播完或被跳過後透過 <see cref="OnFinished"/> 通知外部；
/// 完全不認識「Boss」「勝利」「回主選單」等任何上層流程概念，方便之後在其他過場文本重複使用。
/// </summary>
public class DialogueSequencePlayer : MonoBehaviour
{
    [Header("顯示元件")]
    [SerializeField] private GameObject _root;
    [SerializeField] private Text _lineText;
    [Tooltip("說話者名稱文字，留空則不顯示說話者欄位")]
    [SerializeField] private Text _speakerText;

    [Header("逐字顯示")]
    [Tooltip("每秒顯示幾個字；<= 0 表示不使用逐字效果，直接整句顯示")]
    [SerializeField] private float _charactersPerSecond = 30f;

    [Header("互動")]
    [Tooltip("蓋住整個面板的可點擊區域（可以是透明的 Image + Button），點擊任意處推進")]
    [SerializeField] private Button _advanceClickCatcher;
    [Tooltip("畫面右上角的跳過按鈕，會直接結束整段對話")]
    [SerializeField] private Button _skipButton;

    public event Action OnFinished;

    private IReadOnlyList<DialogueLine> _lines;
    private int _currentIndex;
    private bool _isPlaying;
    private bool _isTyping;
    private Coroutine _typingCoroutine;

    private void Awake()
    {
        if (_advanceClickCatcher != null)
            _advanceClickCatcher.onClick.AddListener(HandleAdvanceClicked);

        if (_skipButton != null)
            _skipButton.onClick.AddListener(Skip);
    }

    public void Play(IReadOnlyList<DialogueLine> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            Finish();
            return;
        }

        _lines = lines;
        _currentIndex = 0;
        _isPlaying = true;

        if (_root != null)
            _root.SetActive(true);

        ShowCurrentLine();
    }

    // 跳過整段對話（不是只跳過一句），對應右上角的跳過按鈕
    public void Skip()
    {
        if (!_isPlaying)
            return;

        Finish();
    }

    // 點擊畫面：如果正在逐字顯示中，先把當前這句瞬間顯示完整；已經顯示完整才會真正推進到下一句
    private void HandleAdvanceClicked()
    {
        if (!_isPlaying)
            return;

        if (_isTyping)
        {
            CompleteTypingInstantly();
            return;
        }

        _currentIndex++;

        if (_currentIndex >= _lines.Count)
        {
            Finish();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = _lines[_currentIndex];

        if (_speakerText != null)
            _speakerText.text = line.speaker ?? string.Empty;

        StopTypingCoroutineIfRunning();

        if (_lineText == null)
            return;

        if (_charactersPerSecond <= 0f)
        {
            _isTyping = false;
            _lineText.text = line.text;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeLine(line.text));
    }

    private IEnumerator TypeLine(string fullText)
    {
        _isTyping = true;
        _lineText.text = string.Empty;

        float secondsPerCharacter = 1f / _charactersPerSecond;
        float timer = 0f;
        int shownCount = 0;

        while (shownCount < fullText.Length)
        {
            timer += Time.unscaledDeltaTime;

            while (timer >= secondsPerCharacter && shownCount < fullText.Length)
            {
                shownCount++;
                timer -= secondsPerCharacter;
            }

            _lineText.text = fullText.Substring(0, shownCount);
            yield return null;
        }

        _isTyping = false;
        _typingCoroutine = null;
    }

    private void CompleteTypingInstantly()
    {
        StopTypingCoroutineIfRunning();

        if (_lineText != null && _lines != null && _currentIndex < _lines.Count)
            _lineText.text = _lines[_currentIndex].text;

        _isTyping = false;
    }

    private void StopTypingCoroutineIfRunning()
    {
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
    }

    private void Finish()
    {
        _isPlaying = false;
        _isTyping = false;
        StopTypingCoroutineIfRunning();

        if (_root != null)
            _root.SetActive(false);

        OnFinished?.Invoke();
    }
}
