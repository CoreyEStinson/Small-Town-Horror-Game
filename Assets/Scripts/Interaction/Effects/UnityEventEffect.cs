using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Interaction/Effects/Unity Event")]
public sealed class UnityEventEffect : InteractionEffect
{
    [SerializeField] private UnityEvent onExecute;

    public override void Execute(InteractionContext context)
    {
        onExecute?.Invoke();
    }
}
