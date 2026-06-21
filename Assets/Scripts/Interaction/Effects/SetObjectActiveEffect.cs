using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/Effects/Set Object Active")]
public sealed class SetObjectActiveEffect : InteractionEffect
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private bool activeState = true;

    public override void Execute(InteractionContext context)
    {
        if (context == null || targetObject == null)
        {
            return;
        }

        targetObject.SetActive(activeState);
    }
}