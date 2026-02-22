using Mdh.RoR2.GlobalEventManager;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.HookGen;
using MonoMod.Cil;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(GlobalEventManager))]
internal static class ATG
{
    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.ATG.EnableEdit.Value)
        {
            return;
        }

        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.ILHook(ProcessHitEnemy);
    }


    private static void ProcessHitEnemy(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ILLabel skipAtgFireMissile = w.DefineLabel();
        int missileDamageVariableNumber = 0;


        // going to start of line:
        // MissileUtils.FireMissile(characterBody.corePosition, characterBody, damageInfo.procChainMask, victim, num7, damageInfo.crit, GlobalEventManager.CommonAssets.missilePrefab, DamageColorIndex.Item, true);
        w.MatchRelaxed(
            x => x.MatchCallOrCallvirt("RoR2.Util", "OnHitProcDamage"),
            x => x.MatchStloc(out missileDamageVariableNumber),
            x => x.MatchLdloc(0) && w.SetCurrentTo(x),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_corePosition"),
            x => x.MatchLdloc(0),
            x => x.MatchLdarg(1),
            x => x.MatchLdfld(out _)
        ).ThrowIfFailure()
        .InsertBeforeCurrent(
            w.Create(OpCodes.Ldloc, 0), // attackerBody
            w.Create(OpCodes.Ldloc, missileDamageVariableNumber), // missileDamage
            w.Create(OpCodes.Ldarg_1), // DamageInfo
            w.Create(OpCodes.Ldarg_2), // victim
            w.CreateDelegateCall((CharacterBody attackerBody, float missileDamage, DamageInfo damageInfo, GameObject victim) =>
            {
                if (attackerBody.teamComponent.teamIndex == TeamIndex.Player)
                {
                    FireMissileOrb(attackerBody, missileDamage, damageInfo, victim);
                }
                return damageInfo;
            }),
            w.Create(OpCodes.Starg, 1),


            w.Create(OpCodes.Ldloc, 0), // attackerBody
            w.CreateDelegateCall((CharacterBody attackerBody) =>
            {
                return attackerBody.teamComponent.teamIndex == TeamIndex.Player;
            }),
            // a normal missile will not be fired if it was fair to fire a missile orb and vice versa
            w.Create(OpCodes.Brtrue, skipAtgFireMissile)
        );


        // going after firemissile line
        w.MatchRelaxed(
            x => x.MatchLdsfld("RoR2.GlobalEventManager/CommonAssets", "missilePrefab"),
            x => x.MatchLdcI4(3),
            x => x.MatchLdcI4(1),
            x => x.MatchCallOrCallvirt("RoR2.MissileUtils", "FireMissile") && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .MarkLabelToCurrentNext(skipAtgFireMissile);
    }


    internal static void FireMissileOrb(CharacterBody attackerBody, float missileDamage, DamageInfo damageInfo, GameObject victim)
    {
        if (
            !victim.TryGetComponent<CharacterBody>(out CharacterBody victimBody)
            || attackerBody == null
            || attackerBody.inventory == null
            || damageInfo.procChainMask.HasProc(ProcType.Missile)
            || !ConfigOptions.ATG.EnableEdit.Value
        )
        {
            return;
        }


        MicroMissileOrb missileOrb = new()
        {
            origin = attackerBody.aimOrigin,
            damageValue = missileDamage,
            isCrit = damageInfo.crit,
            teamIndex = attackerBody.teamComponent.teamIndex,
            attacker = attackerBody.gameObject,
            procChainMask = damageInfo.procChainMask,
            procCoefficient = 1f,
            damageColorIndex = DamageColorIndex.Item,
            target = victimBody.mainHurtBox
        };
        // stupid???
        missileOrb.procChainMask.AddProc(ProcType.Missile);
        damageInfo.procChainMask.AddProc(ProcType.Missile);


        if (ConfigOptions.PocketICBM.EnableEdit.Value)
        {
            // already hooked GetMoreMissileDamageMultiplier to do the edited version's damage so it's fine to use here
            missileOrb.damageValue *= MissileUtils.GetMoreMissileDamageMultiplier(attackerBody.inventory.GetItemCountEffective(DLC1Content.Items.MoreMissile));
        }
        else
        {
            // gotta play a sound for each just like plimp lol
            Util.PlaySound("Play_item_proc_missile_fire", attackerBody.gameObject);
            Util.PlaySound("Play_item_proc_missile_fire", attackerBody.gameObject);
            OrbManager.instance.AddOrb(missileOrb);
            OrbManager.instance.AddOrb(missileOrb);
        }
        // the orb doesn't play a sound on fire and editing the assets isn't working so
        Util.PlaySound("Play_item_proc_missile_fire", attackerBody.gameObject);
        OrbManager.instance.AddOrb(missileOrb);
    }
}