using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoDetour;
using MonoDetour.HookGen;
using MonoDetour.Cil;
using RoR2;
namespace ReheatedItems.ItemChanges.PocketICBMMissilesToDamage;


[MonoDetourTargets(typeof(GlobalEventManager))]
internal static class Plimp
{
    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.PocketICBM.ChangePlasmaShrimpEffect.Value)
        {
            return;
        }

        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.ILHook(ReplacePlimpICBMEffect);
    }

    private static void ReplacePlimpICBMEffect(ILManipulationInfo info)
    {
        ILWeaver w = new(info);

        // near the end of "int num6 = ((num5 <= 0) ? 1 : 3);"
        w.MatchRelaxed(
            x => x.MatchBgt(out _),
            x => x.MatchLdcI4(1),
            x => x.MatchBr(out _),
            x => x.MatchLdcI4(3),
            x => x.MatchStloc(out _) && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertBeforeCurrent(
            // never allow more than 1 missile fired
            w.CreateDelegateCall((int getOut) =>
            {
                return 1;
            })
        );
    }
}