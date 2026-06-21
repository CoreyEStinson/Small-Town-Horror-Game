using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/Conditions/Flag Required")]
public sealed class FlagRequiredCondition : InteractionCondition
{
    [SerializeField] private string requiredFlag;
    [SerializeField] private string failureReason = "You can't do that yet";

    public override bool Evaluate(InteractionContext context)
    {
        return context != null
            && context.flagStore != null
            && context.flagStore.HasFlag(requiredFlag);
    }

    public override string GetFailureReason(InteractionContext context)
    {
        return failureReason;
    }
}