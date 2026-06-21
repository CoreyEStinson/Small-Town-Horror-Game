using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/Effects/Set Flags")]
public sealed class SetFlagEffect : InteractionEffect
{
    [SerializeField] private List<string> flagsToSet = new List<string>();

    public override void Execute(InteractionContext context)
    {
        if (context == null || context.flagStore == null)
        {
            return;
        }

        context.flagStore.SetFlags(flagsToSet);
    }
}