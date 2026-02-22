using MiscFixes;
using MiscFixes.Modules;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Projectile;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(BoomerangProjectile))]
internal static class ElectricBoomerang
{
    private const float _damagePerHit = 3.75f;
    private const float _damagePerHitStack = 2.75f;
    private const int _maximumBoomerangCount = 2;
    private static readonly AssetReferenceT<GameObject> _electricBoomerangProjectileAsset = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_StunAndPierce.StunAndPierceBoomerang_prefab);
    private static readonly AssetReferenceT<ItemDef> _electricBoomerangItemDef = new(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_StunAndPierce.StunAndPierce_asset);
    private static readonly FixedConditionalWeakTable<GameObject, RiElectricBoomerangInfo> _riElectricBoomerangInfoTable = [];
    private class RiElectricBoomerangInfo
    {
        internal int ExistingBoomerangProjectileCount;
    }


    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.ElectricBoomerang.EnableEdit.Value)
        {
            return;
        }

        ModLanguage.LangFilesToLoad.Add("ElectricBoomerang");
        EditBoomerangProjectile();
        AIBlacklistNewBoomerang();
        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.Postfix(GlobalEventManager_ProcessHitEnemy);
        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.ILHook(MakeBoomerangBetter);
        Mdh.RoR2.Projectile.BoomerangProjectile.FixedUpdate.ILHook(SetupRemoveOneFromExistingBoomerangCount);
    }


    private static void GlobalEventManager_ProcessHitEnemy(GlobalEventManager self, ref DamageInfo damageInfo, ref GameObject victim)
    {
        if (victim != null && damageInfo.inflictor != null && damageInfo.inflictor.name == "StunAndPierceBoomerang(Clone)")
        {
            // man i just want this sound to be heard, but it's quiet
            // so i'm just gonna spam it
            // afaik nothing bad happens from this but i don't like doing this
            Util.PlaySound("Play_item_proc_chain_lightning", victim);
            Util.PlaySound("Play_item_proc_chain_lightning", victim);
            Util.PlaySound("Play_item_proc_chain_lightning", victim);
            Util.PlaySound("Play_item_proc_chain_lightning", victim);
            Util.PlaySound("Play_item_proc_chain_lightning", victim);
            Util.PlaySound("Play_item_proc_chain_lightning", victim);
        }
    }


    private static void EditBoomerangProjectile()
    {
        AssetAsyncReferenceManager<GameObject>.LoadAsset(_electricBoomerangProjectileAsset).Completed += (handle) =>
        {
            var boomerangProjectile = handle.Result.GetComponent<BoomerangProjectile>();
            boomerangProjectile.distanceMultiplier *= 0.15f;
            boomerangProjectile.travelSpeed *= 3;

            // this removes the bigger initial hit
            handle.Result.TryDestroyComponent<ProjectileOverlapAttack>();
        };
    }


    private static void AIBlacklistNewBoomerang()
    {
        AssetAsyncReferenceManager<ItemDef>.LoadAsset(_electricBoomerangItemDef).Completed += (handle) =>
        {
            // make it ai blacklisted since players can't be stunned
            ItemDef electricBoomerangItemDef = handle.Result;
            electricBoomerangItemDef.tags = [.. electricBoomerangItemDef.tags, ItemTag.AIBlacklist];
        };
    }


    private static void MakeBoomerangBetter(ILManipulationInfo info)
    {
        ILWeaver w = new(info);


        int electricBoomerangEffectiveCountVariableNumber = 0;
        // matching to:
        // int itemCountEffective20 = inventory.GetItemCountEffective(DLC2Content.Items.StunAndPierce);
        w.MatchRelaxed(
            x => x.MatchLdloc(out _),
            x => x.MatchLdsfld("RoR2.DLC2Content/Items", "StunAndPierce"),
            x => x.MatchCallOrCallvirt<Inventory>("GetItemCountEffective"),
            x => x.MatchStloc(out electricBoomerangEffectiveCountVariableNumber)
        ).ThrowIfFailure();


        ILLabel skipVanillaElectricBoomerangCode = w.DefineLabel();
        // going near end of:
        // if (itemCountEffective20 > 0 && !damageInfo.procChainMask.HasProc(ProcType.StunAndPierceDamage) && (ulong)(damageInfo.damageType & DamageTypeExtended.Electrocution) == 0L && LocalCheckRoll(15f * damageInfo.procCoefficient, master2))
        w.MatchRelaxed(
            x => x.MatchLdcI4(512),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchCallOrCallvirt(out _),
            x => x.MatchBrtrue(out skipVanillaElectricBoomerangCode) && w.SetCurrentTo(x),
            x => x.MatchLdcR4(15f)
        ).ThrowIfFailure()
        .InsertAfterCurrent(
            w.Create(OpCodes.Ldarg_1), // DamageInfo
            w.CreateCall(ShouldAllowNewElectricBoomerang),
            w.Create(OpCodes.Brfalse, skipVanillaElectricBoomerangCode),
            w.Create(OpCodes.Ldloc_0), // CharacterBody
            w.CreateCall(TryAddOneToExistingBoomerangProjectileCount)
        );


        int setDamageVariableNumber = 0;
        // going to end of line:
        // float damage6 = characterBody.damage * 0.4f * (float)itemCountEffective20;
        w.MatchNextRelaxed(
            x => x.MatchLdloc(0),
            x => x.MatchCallOrCallvirt<CharacterBody>("get_damage"),
            x => x.MatchLdcR4(0.4f),
            x => x.MatchMul(),
            x => x.MatchLdloc(out _),
            x => x.MatchConvR4(),
            x => x.MatchMul(),
            x => x.MatchStloc(out setDamageVariableNumber) && w.SetCurrentTo(x)
        ).ThrowIfFailure()
        .InsertBeforeCurrent(
            w.Create(OpCodes.Ldloc, electricBoomerangEffectiveCountVariableNumber),
            w.Create(OpCodes.Ldarg_1), // DamageInfo
            w.CreateDelegateCall((float oldDamage, int electricBoomerangEffectiveCount, DamageInfo damageInfo) =>
            {
                return damageInfo.damage * (_damagePerHit + (_damagePerHitStack * (electricBoomerangEffectiveCount - 1)));
            })
        );

        //w.LogILInstructions();
    }

    private static bool ShouldAllowNewElectricBoomerang(DamageInfo damageInfo)
    {
        int existingBoomerangProjectileCount = 0;
        if (_riElectricBoomerangInfoTable.TryGetValue(damageInfo.attacker.gameObject, out var currentRiElectricBoomerangInfo))
        {
            existingBoomerangProjectileCount = currentRiElectricBoomerangInfo.ExistingBoomerangProjectileCount;
        }
        
        //Log.Warning($"damageInfo.damageType.IsDamageSourceSkillBased is {damageInfo.damageType.IsDamageSourceSkillBased}");
        //Log.Warning($"damageInfo.damageType.damageType.HasFlag(DamageType.Stun1s) is {damageInfo.damageType.damageType.HasFlag(DamageType.Stun1s)}");
        //Log.Warning($"existingBoomerangProjectileCount < _maximumBoomerangCount is {existingBoomerangProjectileCount < _maximumBoomerangCount}");
        //Log.Warning("");

        return damageInfo.damageType.IsDamageSourceSkillBased
        && damageInfo.damageType.damageType.HasFlag(DamageType.Stun1s)
        && existingBoomerangProjectileCount < _maximumBoomerangCount;
    }


    private static void TryAddOneToExistingBoomerangProjectileCount(CharacterBody characterBody)
    {
        if (!NetworkServer.active)
        {
            return;
        }

        if (_riElectricBoomerangInfoTable.TryGetValue(characterBody.gameObject, out var currentRiElectricBoomerangInfo)
            && currentRiElectricBoomerangInfo.ExistingBoomerangProjectileCount < _maximumBoomerangCount)
        {
            currentRiElectricBoomerangInfo.ExistingBoomerangProjectileCount += 1;
        }
        else
        {
            _riElectricBoomerangInfoTable.Add(characterBody.gameObject, new RiElectricBoomerangInfo { ExistingBoomerangProjectileCount = 1 });
        }
    }


    private static void SetupRemoveOneFromExistingBoomerangCount(ILManipulationInfo info)
    {
        ILWeaver w = new(info);

        w.MatchMultipleRelaxed(
            onMatch: (Action<ILWeaver>)(mW =>
            {
                mW.InsertAfterCurrent(
                    mW.Create(OpCodes.Ldarg_0),
                    mW.CreateCall((Delegate)TryRemoveOneFromExistingBoomerangCount)
                );
            }),
            x => x.MatchCall("UnityEngine.Object", "Destroy") && w.SetCurrentTo(x)
        )
        .ThrowIfFailure();
    }


    private static void TryRemoveOneFromExistingBoomerangCount(BoomerangProjectile boomerangProjectile)
    {
        if (NetworkServer.active && _riElectricBoomerangInfoTable.TryGetValue(boomerangProjectile.projectileController.owner, out var currentRiElectricBoomerangInfo))
        {
            currentRiElectricBoomerangInfo.ExistingBoomerangProjectileCount -= 1;
            if (currentRiElectricBoomerangInfo.ExistingBoomerangProjectileCount < 1)
            {
                _riElectricBoomerangInfoTable.Remove(boomerangProjectile.projectileController.owner);
            }
        }
    }
}