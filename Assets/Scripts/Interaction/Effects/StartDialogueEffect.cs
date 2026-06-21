using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/Effects/Start Dialogue")]
public sealed class StartDialogueEffect : InteractionEffect
{
    [SerializeField] private DialogueYamlLoader dialogueLoader;

    public override void Execute(InteractionContext context)
    {
        if (context == null || context.dialogueRunner == null)
        {
            return;
        }

        DialogueYamlLoader loader = FindDialogueLoader(context);
        if (loader == null)
        {
            Debug.LogWarning("StartDialogueEffect could not find a DialogueYamlLoader.", this);
            return;
        }

        context.dialogueRunner.BeginDialogue(loader);
    }

    private DialogueYamlLoader FindDialogueLoader(InteractionContext context)
    {
        if (dialogueLoader != null)
        {
            return dialogueLoader;
        }

        if (context.sourceObject != null &&
            context.sourceObject.TryGetComponent(out DialogueYamlLoader loader))
        {
            return loader;
        }

        if (context.sourceTransform != null)
        {
            loader = context.sourceTransform.GetComponentInParent<DialogueYamlLoader>();
            if (loader != null)
            {
                return loader;
            }

            loader = context.sourceTransform.GetComponentInChildren<DialogueYamlLoader>();
            if (loader != null)
            {
                return loader;
            }
        }

        return null;
    }
}
