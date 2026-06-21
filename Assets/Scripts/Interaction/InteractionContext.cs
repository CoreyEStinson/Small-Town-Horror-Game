using UnityEngine;

public sealed class InteractionContext
{
    public GameObject interactor;
    public Transform interactorTransform;
    public GameObject sourceObject;
    public Transform sourceTransform;
    public DialogueRunner dialogueRunner;
    public DialogueFlagStore flagStore;
    public Collider sourceCollider;

    // Later can include inventory, quest, audio, and cutscene services
}
