using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoDetour;
using MonoDetour.HookGen;
using MonoDetour.Cil;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;
using RoR2;
using RoR2.ContentManagement;
using RoR2.Items;
using MiscFixes.Modules;
using R2API;
using System.IO;
using UnityEngine.Networking;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets(typeof(GlobalEventManager))]
internal static class MoltenPerforator
{
    private const float _initialDamage = 2.5f;
    private const float _stackDamage = 1.5f;

    [MonoDetourHookInitialize]
    internal static void Setup()
    {
        if (!ConfigOptions.MoltenPerforator.EnableEdit.Value)
        {
            return;
        }

        ModLanguage.LangFilesToLoad.Add("MoltenPerforator");
        Assets.Setup();
        Assets.LoadAssets();
        Mdh.RoR2.GlobalEventManager.ProcessHitEnemy.ILHook(ILHook.ReplaceMerfFunctionality);
    }

    
    private static class Assets
    {
        internal static AssetBundle MerfAssetBundle;
        internal const string MerfAssetBundleName = "moltenperforator";
        internal static string MerfAssetBundlePath
        {
            get
            {
                return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.PluginInfo.Location), MerfAssetBundleName);
            }
        }

        internal static void Setup()
        {
            MerfAssetBundle = AssetBundle.LoadFromFile(MerfAssetBundlePath);
        }



        internal static GameObject MerfExplosionPrefab;

        internal static void LoadAssets()
        {
            MerfExplosionPrefab = MerfAssetBundle.LoadAsset<GameObject>("MoltenPerforatorExplosionVFX");
            ContentAddition.AddEffect(MerfExplosionPrefab);
        }
    }


    private static class ILHook
    {
        private const float _golemRadius = 1.63f;

        internal static void ReplaceMerfFunctionality(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            Instruction startOfVanillaCode = null!;
            ILLabel labelToEndOfVanillaCode = w.DefineLabel();

            // going in the middle of:
            // if (itemCountEffective15 > 0 && !damageInfo.procChainMask.HasProc(ProcType.Meatball))
            // to grab the location to exit merf code
            w.MatchRelaxed(
                x => x.MatchLdloc(23),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out labelToEndOfVanillaCode) && w.SetCurrentTo(x)
            ).ThrowIfFailure();

            // going before:
            // InputBankTest component10 = characterBody.GetComponent<InputBankTest>();
            // which is after the line matched for above
            w.MatchNextRelaxed(
                x => x.MatchLdloc(0) && w.SetCurrentTo(x) && w.SetInstructionTo(ref startOfVanillaCode, x),
                x => x.MatchCallvirt<Component>("GetComponent")
            ).ThrowIfFailure()
            .InsertBeforeCurrentStealLabels(
                w.Create(OpCodes.Ldarg_1), // DamageInfo
                w.Create(OpCodes.Ldarg_2), // victim
                w.CreateCall(DoMoltenPerforatorExplosion),
                w.Create(OpCodes.Br, labelToEndOfVanillaCode)
            );
            //w.LogILInstructions();
        }



        private static void DoMoltenPerforatorExplosion(DamageInfo damageInfo, GameObject victim)
        {
            if (victim == null || damageInfo.attacker == null)
            {
                return;
            }
            CharacterBody victimBody = victim.GetComponent<CharacterBody>();
            if (victimBody == null)
            {
                return;
            }
            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody == null || attackerBody.inventory == null)
            {
                return;
            }
            if (!Util.CheckRoll(10f * damageInfo.procCoefficient, attackerBody.master))
            {
                return;
            }


            int merfCount = attackerBody.inventory.GetItemCountEffective(RoR2Content.Items.FireballsOnHit);
            float damageToDeal = Util.OnHitProcDamage(damageInfo.damage, attackerBody.damage, _initialDamage + ((merfCount - 1) * _stackDamage));
            float radiusMultFromVictimRadius = 1 + ((victimBody.radius - _golemRadius) * 0.08f * 0.8f);


            EffectData effectData = new()
            {
                origin = victim.transform.position,
                // 1 scale is too small so 6.38 makes it 16m radius
                scale = 6.38f * radiusMultFromVictimRadius
            };
            EffectManager.SpawnEffect(Assets.MerfExplosionPrefab, effectData, true);
            BlastAttack blastAttack = new()
            {
                baseForce = 500,
                radius = 16f * radiusMultFromVictimRadius,
                baseDamage = damageToDeal,
                procCoefficient = 1,
                crit = damageInfo.crit,
                damageColorIndex = DamageColorIndex.Item,
                attackerFiltering = AttackerFiltering.Default,
                falloffModel = BlastAttack.FalloffModel.None,
                attacker = damageInfo.attacker,
                teamIndex = attackerBody.teamComponent.teamIndex,
                position = victim.transform.position,
                inflictor = damageInfo.inflictor,
                damageType = DamageType.IgniteOnHit,
                procChainMask = damageInfo.procChainMask,
            };
            blastAttack.procChainMask.AddProc(ProcType.Meatball);
            blastAttack.Fire();
        }
    }

}