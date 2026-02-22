using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoDetour;
using MonoDetour.HookGen;
using MonoDetour.Cil;
using UnityEngine;
using RoR2;
using RoR2.Orbs;
using ReheatedItems.ItemChanges.PocketICBMMissilesToDamage;
namespace ReheatedItems.ModSupport.RocketSurvivorGuy;


[MonoDetourTargets(typeof(EntityStates.RocketSurvivorSkills.Primary.FireRocket))]
internal static class PrimaryICBMSupport
{
    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.PocketICBM.ChangeRocketSurvivorEffect.Value)
        {
            return;
        }

        Mdh.EntityStates.RocketSurvivorSkills.Primary.FireRocket.OnEnter.ILHook(ChangeICBMEffect);
    }

    private static void ChangeICBMEffect(ILManipulationInfo info)
    {
        ILWeaver w = new(info);


        // before "if (flag2)"
        w.MatchRelaxed(
            x => x.MatchStloc(5),
            x => x.MatchLdloc(5) && w.SetCurrentTo(x),
            x => x.MatchBrfalse(out _)
        ).ThrowIfFailure()
        // skip normal icbm support
        .InsertAfterCurrent(w.Create(OpCodes.Pop))
        .Next.OpCode = OpCodes.Br;
        //w.LogILInstructions();


        w.MatchRelaxed(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<EntityStates.BaseState>("damageStat"),
            x => x.MatchLdsfld<EntityStates.RocketSurvivorSkills.Primary.FireRocket>("damageCoefficient"),
            x => x.MatchMul() && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.Create(OpCodes.Ldarg_0),
            w.CreateCall(ChangeDamageBasedOnICBM)
        );
    }

    private static float ChangeDamageBasedOnICBM(float damage, EntityStates.EntityState entityState)
    {
        return damage * PocketICBM.GetICBMDamageMultForCharacterBody(entityState.characterBody);
    }
}