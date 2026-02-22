using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace ReheatedItems.ItemChanges.PocketICBMMissilesToDamage;


[MonoDetourTargets]
internal static class WarBonds
{
    [MonoDetourHookInitialize]
    private static void Setup()
    {
        Mdh.RoR2.BarrageOnBossBehaviour.FireMissile.ILHook(ReplaceMissilesWithDamageMult);
    }

    private static void ReplaceMissilesWithDamageMult(ILManipulationInfo info)
    {
        ILWeaver w = new(info);

        w.MatchRelaxed(
            x => x.MatchAdd(),
            x => x.MatchCallOrCallvirt<UnityEngine.Mathf>("Max"),
            x => x.MatchStloc(3) && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.Create(OpCodes.Ldloc_2),
            w.CreateDelegateCall((int icbmCount) =>
            {
                return PocketICBM.GetICBMDamageMult(icbmCount);
            }),
            w.Create(OpCodes.Stloc_3),
            w.CreateDelegateCall(() =>
            {
                return 0;
            }),
            w.Create(OpCodes.Stloc_2)
        );
    }
}