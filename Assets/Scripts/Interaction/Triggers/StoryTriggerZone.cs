using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public sealed class StoryTriggerZone : MonoBehaviour
{
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private DialogueFlagStore flagStore;
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private bool disableAfterTrigger = true;
    [SerializeField] private List<InteractionCondition> conditions = new List<InteractionCondition>();
    [SerializeField] private List<InteractionEffect> effects = new List<InteractionEffect>();
    [SerializeField] private UnityEvent onTriggered;

    private bool hasTriggered;

    private void Awake()
    {
        if (dialogueRunner == null)
        {
            dialogueRunner = FindAnyObjectByType<DialogueRunner>();
        }

        if (flagStore == null)
        {
            flagStore = FindAnyObjectByType<DialogueFlagStore>();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        PlayerInteractor playerInteractor = other.GetComponent<PlayerInteractor>();
        if (playerInteractor == null)
        {
            playerInteractor = other.GetComponentInParent<PlayerInteractor>();
        }

        if (playerInteractor == null)
        {
            return;
        }

        InteractionContext context = BuildContext(other, playerInteractor);
        if (!MeetsConditions(context))
        {
            return;
        }

        RunEffects(context);
        onTriggered?.Invoke();
        hasTriggered = true;

        if (disableAfterTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    private InteractionContext BuildContext(Collider other, PlayerInteractor playerInteractor)
    {
        Transform interactorTransform = playerInteractor != null ? playerInteractor.transform : other.transform;

        return new InteractionContext
        {
            interactor = interactorTransform != null ? interactorTransform.gameObject : null,
            interactorTransform = interactorTransform,
            dialogueRunner = dialogueRunner,
            flagStore = flagStore,
            sourceCollider = other
        };
    }

    private bool MeetsConditions(InteractionContext context)
    {
        for (int i = 0; i < conditions.Count; i++)
        {
            InteractionCondition condition = conditions[i];
            if (condition == null)
            {
                continue;
            }

            if (!condition.Evaluate(context))
            {
                return false;
            }
        }

        return true;
    }

    private void RunEffects(InteractionContext context)
    {
        for (int i = 0; i < effects.Count; i++)
        {
            InteractionEffect effect = effects[i];
            if (effect == null)
            {
                continue;
            }

            effect.Execute(context);
        }
    }
}
