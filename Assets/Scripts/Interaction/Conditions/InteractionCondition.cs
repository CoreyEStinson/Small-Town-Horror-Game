using UnityEngine;

public abstract class InteractionCondition : ScriptableObject
{
    [SerializeField] private string debugName;

    public abstract bool Evaluate(InteractionContext context);

    public virtual string GetFailureReason(InteractionContext context)
    {
        return string.Empty;
    }
}
