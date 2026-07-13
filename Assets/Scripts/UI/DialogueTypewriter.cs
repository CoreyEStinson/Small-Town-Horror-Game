using System.Collections;
using TMPro;
using UnityEngine;

public sealed class DialogueTypewriter : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float baseCharacterDelay = 0.025f;

    private Coroutine typingCoroutine;
    private Coroutine animationCoroutine;
    private DialogueTextEffectParser textEffectParser;
    private DialogueProcessedText processedText;
    private bool hasTextChanged;
    private TMP_MeshInfo[] baseMeshData;
    private int lastVisibleCharacterCount = -1;

    [Header("Text Wave Effect")]
    [SerializeField] private float waveAmplitude = 3f;
    [SerializeField] private float waveFrequency = 20f;

    public bool IsTyping { get; private set; }

    [Header("Dialogue Bark Audio")]
    [SerializeField] private AudioSource barkAudioSource;

    private DialogueBarkProfile activeBarkProfile;
    private int charactersSinceLastBark;

    private void Awake()
    {
        if (targetText == null)
        {
            targetText = GetComponent<TextMeshProUGUI>();
        }

        textEffectParser = new DialogueTextEffectParser();
        processedText = new DialogueProcessedText();
    }

    private void OnEnable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);

        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
            animationCoroutine = null;
        }
    }

    public void Play(string text)
    {
        if (targetText == null)
        {
            return;
        }

        StopCurrentTyping();

        processedText = textEffectParser.Parse(text);

        for (int i = 0; i < processedText.warnings.Count; i++)
        {
            Debug.LogWarning(processedText.warnings[i]);
        }

        targetText.text = processedText.processedText ?? string.Empty;
        targetText.maxVisibleCharacters = 0;
        targetText.ForceMeshUpdate();
        hasTextChanged = true;
        lastVisibleCharacterCount = targetText.maxVisibleCharacters;

        int visibleCharacterCount = targetText.textInfo.characterCount;
        if (visibleCharacterCount == 0)
        {
            targetText.maxVisibleCharacters = int.MaxValue;
            IsTyping = false;
            return;
        }

        if (animationCoroutine == null)
        {
            animationCoroutine = StartCoroutine(AnimateTextEffects());
        }

        typingCoroutine = StartCoroutine(TypeText(processedText.processedText ?? string.Empty, visibleCharacterCount));
    }

    public void Complete()
    {
        if (targetText == null)
        {
            return;
        }

        StopCurrentTyping();
        StopBarkAudio();
        targetText.maxVisibleCharacters = int.MaxValue;
    }

    public void StopAndClear()
    {
        StopCurrentTyping();
        StopBarkAudio();
        
        if (targetText == null)
        {
            return;
        }

        targetText.text = string.Empty;
        targetText.maxVisibleCharacters = int.MaxValue;
        processedText = new DialogueProcessedText();
        hasTextChanged = true;
        baseMeshData = null;
        lastVisibleCharacterCount = -1;
    }

    private IEnumerator TypeText(string sourceText, int visibleCharacterCount)
    {
        IsTyping = true;
        int visibleCount = 0;
        bool insideTag = false;

        for (int i = 0; i < sourceText.Length; i++)
        {
            char currentCharacter = sourceText[i];

            if (currentCharacter == '<')
            {
                insideTag = true;
            }

            if (insideTag)
            {
                if (currentCharacter == '>')
                {
                    insideTag = false;
                }

                continue;
            }

            if (char.IsHighSurrogate(currentCharacter))
            {
                continue;
            }

            visibleCount++;
            TryPlayBark(currentCharacter);
            targetText.maxVisibleCharacters = Mathf.Min(visibleCount, visibleCharacterCount);
            if (targetText.maxVisibleCharacters != lastVisibleCharacterCount)
            {
                hasTextChanged = true;
                lastVisibleCharacterCount = targetText.maxVisibleCharacters;
            }

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
        hasTextChanged = true;
        lastVisibleCharacterCount = targetText.maxVisibleCharacters;
        typingCoroutine = null;
        IsTyping = false;
    }

    private IEnumerator AnimateTextEffects()
    {
        if (targetText == null)
        {
            yield break;
        }

        targetText.ForceMeshUpdate();

        TMP_TextInfo textInfo = targetText.textInfo;
        Vector3[][] copyOfVertices = new Vector3[0][];
        hasTextChanged = true;

        while (true)
        {
            if (hasTextChanged)
            {
                targetText.ForceMeshUpdate();
                textInfo = targetText.textInfo;
                baseMeshData = textInfo.CopyMeshInfoVertexData();

                if (copyOfVertices.Length < textInfo.meshInfo.Length)
                {
                    copyOfVertices = new Vector3[textInfo.meshInfo.Length][];
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    int length = textInfo.meshInfo[i].vertices.Length;
                    copyOfVertices[i] = new Vector3[length];
                }

                hasTextChanged = false;
            }

            int characterCount = textInfo.characterCount;
            if (characterCount == 0
                || processedText.effectSpans == null
                || processedText.effectSpans.Count == 0
                || string.IsNullOrEmpty(targetText.text)
                || targetText.maxVisibleCharacters <= 0
                || baseMeshData == null)
            {
                yield return null;
                continue;
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                Vector3[] sourceVertices = baseMeshData[i].vertices;
                Vector3[] destinationVertices = copyOfVertices[i];

                int copyLength = Mathf.Min(sourceVertices.Length, destinationVertices.Length);
                for (int j = 0; j < copyLength; j++)
                {
                    destinationVertices[j] = sourceVertices[j];
                }
            }

            for (int spanIndex = 0; spanIndex < processedText.effectSpans.Count; spanIndex++)
            {
                DialogueTextEffectSpan span = processedText.effectSpans[spanIndex];
                if (span.effectType != DialogueTextEffectType.Wave) continue;

                int start = span.startVisibleCharacterIndex;
                int end = start + span.length;

                for (int charIndex = start; charIndex < end; charIndex++)
                {
                    if (charIndex >= targetText.maxVisibleCharacters) break;
                    if (charIndex >= textInfo.characterCount) break;

                    ApplyWaveToCharacter(textInfo, copyOfVertices, charIndex);
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = copyOfVertices[i];
                targetText.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }

    private void ApplyWaveToCharacter(TMP_TextInfo textInfo, Vector3[][] copyOfVertices, int charIndex)
    {
        TMP_CharacterInfo charInfo = textInfo.characterInfo[charIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Vector3[] sourceVertices = baseMeshData[materialIndex].vertices;
        Vector3[] destinationVertices = copyOfVertices[materialIndex];

        Vector3 charCenter = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2f;

        float time = Time.time * waveFrequency;
        float y = Mathf.Sin(time + charIndex * 0.53f) * waveAmplitude;
        Vector3 offset = new Vector3(0f, y, 0f);

        destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - charCenter + charCenter + offset;
        destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - charCenter + charCenter + offset;
        destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - charCenter + charCenter + offset;
        destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - charCenter + charCenter + offset;
    }

    // Planned to have different delays for punctuation 
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

    private void OnTextChanged(Object changedObject)
    {
        if (changedObject == targetText)
        {
            hasTextChanged = true;
        }
    }

    public void SetBarkProfile(DialogueBarkProfile profile)
    {
        activeBarkProfile = profile;
        charactersSinceLastBark = 0;
    }

    private void TryPlayBark(char currentCharacter)
    {
        if (activeBarkProfile == null
            || barkAudioSource == null
            || char.IsWhiteSpace(currentCharacter)
            || char.IsPunctuation(currentCharacter))
        {
            return;
        }

        charactersSinceLastBark++;

        if (charactersSinceLastBark < activeBarkProfile.CharactersPerBlip)
        {
            return;
        }

        charactersSinceLastBark = 0;

        AudioClip clip = activeBarkProfile.GetRandomClip();
        if (clip == null)
        {
            return;
        }

        barkAudioSource.pitch = activeBarkProfile.GetRandomPitch();
        barkAudioSource.PlayOneShot(clip, activeBarkProfile.Volume);
    }

    private void StopBarkAudio()
    {
        charactersSinceLastBark = 0;

        if (barkAudioSource != null)
        {
            barkAudioSource.Stop();
        }
    }
}
