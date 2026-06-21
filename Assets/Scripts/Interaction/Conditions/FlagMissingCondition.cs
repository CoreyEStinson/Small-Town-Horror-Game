using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/Conditions/Flag Missing")]
public sealed class FlagMissingCondition : InteractionCondition
{
    [SerializeField] private string blockedByFlag;
    [SerializeField] private string failureReason = "";

    public override bool Evaluate(InteractionContext context)
    {
        return context != null
            && context.flagStore != null
            && !context.flagStore.HasFlag(blockedByFlag);
    }

    public override string GetFailureReason(InteractionContext context)
    {
        return failureReason;
    }
}