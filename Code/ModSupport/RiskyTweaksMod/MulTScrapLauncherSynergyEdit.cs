using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoDetour.Logging;
using MonoMod.Cil;
using RiskyTweaks.Tweaks.Survivors.Toolbot;
using RoR2;
using UnityEngine;
using HarmonyLib;
using ReheatedItems.ItemChanges.PocketICBMMissilesToDamage;
namespace ReheatedItems.ModSupport.RiskyTweaksMod;


[MonoDetourTargets(typeof(ScrapICBM), GenerateControlFlowVariants = true)]
internal static class MulTScrapLauncherSynergyEdit
{
    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.PocketICBM.ChangeRiskyTweaksScrapLauncherEffect.Value)
        {
            return;
        }

        Mdh.RiskyTweaks.Tweaks.Survivors.Toolbot.ScrapICBM.FireGrenadeLauncher_ModifyProjectileAimRay.ILHook(JustDoMyThing);
    }

    private static void JustDoMyThing(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ILLabel skipToOrig = w.DefineLabel();
        //w.LogILInstructions();


        w.InsertBeforeCurrent(
            w.Create(OpCodes.Ldarg_2),
            w.CreateCall(ChangeICBMSynergy),
            w.Create(OpCodes.Br, skipToOrig)
        );


        w.MatchRelaxed(
            x => x.MatchLdarg(1) && w.SetCurrentTo(x),
            x => x.MatchLdarg(2),
            x => x.MatchLdarg(3),
            x => x.MatchCallvirt(out _)
        ).ThrowIfFailure()
        .MarkLabelToCurrentPrevious(skipToOrig);
    }
    private static void ChangeICBMSynergy(EntityStates.Toolbot.FireGrenadeLauncher fireGrenadeLauncherState)
    {
        fireGrenadeLauncherState.damageCoefficient *= PocketICBM.GetICBMDamageMultForCharacterBody(fireGrenadeLauncherState.characterBody);
    }
}