using UnityEngine;

public abstract class InteractionEffect : ScriptableObject
{
    [SerializeField] private string debugName;

    public abstract void Execute(InteractionContext context);
}