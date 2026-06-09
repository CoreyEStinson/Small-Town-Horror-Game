using System.Collections;
using TMPro;
using UnityEngine;

public sealed class DialogueTypewriter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float baseCharacterDelay = 0.025f;

    private Coroutine typingCoroutine;

    public bool IsTyping { get; private set; }

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TextMeshProUGUI>();
        }
    }

    public void Play(string text)
    {
        if (targetText == null)
        {
            return;
        }

        StopCurrentTyping();

        targetText.text = text ?? string.Empty;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();

        int visibleCharacterCount = targetText.textInfo.characterCount;
        if (visibleCharacterCount == 0)
        {
            targetText.maxVisibleCharacters = int.MaxValue;
            IsTyping = false;
            return;
        }

        typingCoroutine = StartCoroutine(TypeText(text ?? string.Empty, visibleCharacterCount));
    }

    public void Complete()
    {
        if (targetText == null)
        {
            return;
        }

        StopCurrentTyping();
        targetText.maxVisibleCharacters = int.MaxValue;
    }

    public void StopAndClear()
    {
        StopCurrentTyping();

        if (targetText == null)
        {
            return;
        }

        targetText.text = string.Empty;
        targetText.maxVisibleCharacters = int.MaxValue;
    }

    private IEnumerator TypeText(string sourceText, int visibleCharacterCount)
    {
        IsTyping = true;
        int visibleCount = 0;

        for (int i = 0; i < sourceText.Length; i++)
        {
            char currentCharacter = sourceText[i];

            if (char.IsHighSurrogate(currentCharacter))
            {
                continue;
            }

            visibleCount++;
            targetText.maxVisibleCharacters = Mathf.Min(visibleCount, visibleCharacterCount);

            float delay = GetDelayForCharacter(currentCharacter);
            if (delay <= 0f)
            {
                continue;
            }

            float elapsed = 0f;
            while (elapsed < delay)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        targetText.maxVisibleCharacters = int.MaxValue;
        typingCoroutine = null;
        IsTyping = false;
    }

    private float GetDelayForCharacter(char currentCharacter)
    {
        if (currentCharacter == '\r')
        {
            return 0f;
        }

        return baseCharacterDelay;
    }

    private void StopCurrentTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        IsTyping = false;
    }
}
