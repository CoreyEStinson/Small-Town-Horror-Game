using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JournalToastController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject toastPanel;
    [SerializeField] private RectTransform toastRectTransform;
    [SerializeField] private CanvasGroup toastCanvasGroup;
    [SerializeField] private TMP_Text toastText;

    [Header("Animation")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float slideDuration = 0.25f;
    [SerializeField] private float hiddenOffset = 450f;

    [Header("Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip toastSound;

    private readonly Queue<ToastRequest> queuedToasts =
        new Queue<ToastRequest>();

    private JournalManager journalManager;
    private Coroutine toastRoutine;
    private Vector2 visiblePosition;
    private Vector2 hiddenPosition;

    private struct ToastRequest
    {
        public string text;

        public ToastRequest(string text)
        {
            this.text = text;
        }
    }

    private void Awake()
    {
        if (toastRectTransform == null)
        {
            toastRectTransform = toastPanel.GetComponent<RectTransform>();
        }

        visiblePosition = toastRectTransform.anchoredPosition;
        hiddenPosition = visiblePosition + Vector2.right * hiddenOffset;

        toastRectTransform.anchoredPosition = hiddenPosition;
        toastCanvasGroup.alpha = 0f;
        toastPanel.SetActive(false);
    }

    private void Start()
    {
        journalManager = JournalManager.Instance;

        if (journalManager == null)
        {
            Debug.LogError(
                "JournalToastController could not find JournalManager",
                this
            );
            return;
        }

        journalManager.JournalToastRequested += QueueToast;
    }

    private void OnDestroy()
    {
        if (journalManager != null)
        {
            journalManager.JournalToastRequested -= QueueToast;
        }
    }

    private void QueueToast(JournalUpdateType updateType, string updateText)
    {
        string toastMessage = BuildToastMessage(
            updateType,
            updateText
        );

        queuedToasts.Enqueue(new ToastRequest(toastMessage));

        if (toastRoutine == null)
        {
            toastRoutine = StartCoroutine(ShowQueuedToasts());
        }
    }

    private string BuildToastMessage(JournalUpdateType updateType, string updateText)
    {
        switch (updateType)
        {
            case JournalUpdateType.QuestCompleted:
                return $"Quest Completed: {updateText}";
            default: 
                return $"Journal Updated: {updateText}";
        } 
    }

    private IEnumerator ShowQueuedToasts()
    {
        while (queuedToasts.Count > 0)
        {
            ToastRequest toast = queuedToasts.Dequeue();

            toastText.text = toast.text;
            toastPanel.SetActive(true);

            if (audioSource != null && toastSound != null)
            {
                audioSource.PlayOneShot(toastSound);
            }

            yield return SlideToast(
                hiddenPosition,
                visiblePosition,
                0f,
                1f
            );

            yield return new WaitForSecondsRealtime(displayDuration);

            yield return SlideToast(
                visiblePosition,
                hiddenPosition,
                1f,
                0f
            );

            toastPanel.SetActive(false);
        }

        toastRoutine = null;
    }

    private IEnumerator SlideToast(
        Vector2 fromPosition,
        Vector2 toPosition,
        float fromAlpha,
        float toAlpha
    )
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / slideDuration);

            progress = progress * progress * (3f - 2f * progress);

            toastRectTransform.anchoredPosition = 
                Vector2.Lerp(
                    fromPosition,
                    toPosition,
                    progress
                );
            
            toastCanvasGroup.alpha = Mathf.Lerp(
                fromAlpha,
                toAlpha,
                progress
            );

            yield return null;
        }

        toastRectTransform.anchoredPosition = toPosition;
        toastCanvasGroup.alpha = toAlpha;
    }
}