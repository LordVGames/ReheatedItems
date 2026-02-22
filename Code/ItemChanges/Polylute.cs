using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(GlobalEventManager))]
internal class Polylute
{
    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.Polylute.EnableEdit.Value)
        {
            return;
        }

        ModLanguage.LangFilesToLoad.Add("Polylute");
        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.ILHook(SwapStackingHitsForDamage);
    }

    private static void SwapStackingHitsForDamage(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        int voidLightningOrbVariableNumber = -1;
        int polyluteItemCountVariableNumber = -1;

        // grabbing item count from:
        // int itemCountEffective9 = inventory.GetItemCountEffective(DLC1Content.Items.ChainLightningVoid);
        w.MatchRelaxed(
            x => x.MatchLdloc(out _),
            x => x.MatchLdsfld("RoR2.DLC1Content/Items", "ChainLightningVoid"),
            x => x.MatchCallOrCallvirt<Inventory>("GetItemCountEffective"),
            x => x.MatchStloc(out polyluteItemCountVariableNumber)
        ).ThrowIfFailure();

        // going to end of:
        // voidLightningOrb.damageValue = damageValue4;
        w.MatchRelaxed(
            x => x.MatchLdloc(out voidLightningOrbVariableNumber),
            x => x.MatchLdloc(out _),
            x => x.MatchStfld<VoidLightningOrb>("damageValue") && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertBeforeCurrent(
            w.Create(OpCodes.Ldloc, polyluteItemCountVariableNumber),
            w.CreateDelegateCall((float damage, int polyluteCount) =>
            {
                return damage * polyluteCount;
            })
        );

        // at the end of line:
        // voidLightningOrb.totalStrikes = 3 * itemCountEffective9;
        w.MatchRelaxed(
            x => x.MatchLdloc(out voidLightningOrbVariableNumber),
            x => x.MatchLdcI4(3),
            x => x.MatchLdloc(out _),
            x => x.MatchMul(),
            x => x.MatchStfld<VoidLightningOrb>("totalStrikes") && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.Create(OpCodes.Ldloc, voidLightningOrbVariableNumber), // load VoidLightningOrb
            w.CreateDelegateCall((VoidLightningOrb voidLightningOrb) =>
            {
                voidLightningOrb.totalStrikes = 3;
            })
        );
        
        //w.LogILInstructions();
    }

    private static float DoDamageMult(int polyluteCount, float damage)
    {
        return damage * polyluteCount;
    }

    private static void ResetVoidLightningOrbStrikeCount(VoidLightningOrb voidLightningOrb)
    {
        voidLightningOrb.totalStrikes = 3;
    }
}