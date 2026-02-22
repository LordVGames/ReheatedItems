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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
namespace ReheatedItems.ItemChanges;


[MonoDetourTargets]
public static class Guillotine
{
    private const float _cooldownTime = 40.0f;
    private const float _damageCoefficient = 10f;
    private const float _debuffDuration = 12f;


    private static List<BuffDef> _allEliteBuffDefs = [];


    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.Guillotine.EnableEdit.Value)
        {
            return;
        }


        ModLanguage.LangFilesToLoad.Add("Guillotine");
        VanillaAssets.Setup();
        ModAssets.Setup();
        Equipment.SetupEquipment();
        Buffs.SetupBuffs();
        Hooks.Setup();
    }


    private static class VanillaAssets
    {
        private static readonly AssetReferenceT<ItemDef> _guillotineItemDefReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ExecuteLowHealthElite.ExecuteLowHealthElite_asset);
        private static readonly AssetReferenceT<GameObject> _royalCapacitorIndicatorReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Lightning.LightningIndicator_prefab);
        internal static GameObject RoyalCapacitorIndicator;


        internal static void Setup()
        {
            AssetAsyncReferenceManager<ItemDef>.LoadAsset(_guillotineItemDefReference).Completed += (handle) =>
            {
                handle.Result.tier = ItemTier.NoTier;
                handle.Result._itemTierDef = null;
#pragma warning disable CS0618 // Type or member is obsolete
                handle.Result.deprecatedTier = ItemTier.NoTier;
#pragma warning restore CS0618 // Type or member is obsolete
            };
            AssetAsyncReferenceManager<GameObject>.LoadAsset(_royalCapacitorIndicatorReference).Completed += (handle) =>
            {
                RoyalCapacitorIndicator = handle.Result;
                ModAssets.EditedVanillaAssets.GuillotineTarget.Setup();
            };
        }
    }


    private static class ModAssets
    {
        internal static void Setup()
        {
            AssetBundleAssets.GuillotineAssetBundle = AssetBundle.LoadFromFile(AssetBundleAssets.GuillotineAssetBundlePath);
            AssetBundleAssets.LoadAssets();
            // EditedVanillaAssets is called by VanillaAssets when that's finished
        }


        internal static class AssetBundleAssets
        {
            internal static AssetBundle GuillotineAssetBundle;
            internal const string GuillotineAssetBundleName = "oldguillotine";
            internal static string GuillotineAssetBundlePath
            {
                get
                {
                    return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Plugin.PluginInfo.Location), GuillotineAssetBundleName);
                }
            }


            internal static Sprite GuillotineIconOrange;


            internal static void LoadAssets()
            {
                GuillotineIconOrange = GuillotineAssetBundle.LoadAsset<Sprite>("texOldGuillotineOrangeIcon");
            }
        }


        internal static class EditedVanillaAssets
        {
            internal static class GuillotineTarget
            {
                internal static GameObject GuillotineTargetingIndicator = PrefabAPI.CreateEmptyPrefab("GuillotineTargetIndicator");


                internal static void Setup()
                {
                    GuillotineTargetingIndicator = PrefabAPI.InstantiateClone(VanillaAssets.RoyalCapacitorIndicator, "GuillotineTargetIndicator", false);
                    GuillotineTargetingIndicator.transform.GetChild(1).GetComponent<TextMeshPro>().color = Color.gray;
                    Transform bracketsHolder = GuillotineTargetingIndicator.transform.GetChild(0);
                    bracketsHolder.GetChild(0).GetComponent<SpriteRenderer>().color = Color.gray;
                    bracketsHolder.GetChild(1).GetComponent<SpriteRenderer>().color = Color.gray;
                }
            }
        }
    }


    public static class Equipment
    {
        private const string _guillotineEquipmentDefName = "edGuillotine";
        public static EquipmentDef edGuillotine = new()
        {
            name = _guillotineEquipmentDefName,
            appearsInMultiPlayer = true,
            appearsInSinglePlayer = true,
            cooldown = _cooldownTime,
            pickupIconSprite = ModAssets.AssetBundleAssets.GuillotineIconOrange,
            pickupModelReference = new(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ExecuteLowHealthElite.PickupGuillotine_prefab),
            nameToken = "ITEM_EXECUTELOWHEALTHELITE_NAME",
            pickupToken = "EQUIPMENT_GUILLOTINE_PICKUP",
            descriptionToken = "EQUIPMENT_GUILLOTINE_DESC",
            loreToken = "ITEM_EXECUTELOWHEALTHELITE_LORE",
        };
        // TODO copy item display rules from vanilla guillotine? somehow??
        public static CustomEquipment ceGuillotine = new(edGuillotine, []);
        public static EquipmentIndex eiGuillotine;


        internal static void SetupEquipment()
        {
            if (!ItemAPI.Add(ceGuillotine))
            {
                Log.Error("Guillotine custom equipment could not be added by ItemAPI for some reason!");
                return;
            }
        }


        [SystemInitializer(dependencies: typeof(EquipmentCatalog))]
        private static void GetGuillotineEquipmentIndex()
        {
            eiGuillotine = EquipmentCatalog.FindEquipmentIndex(_guillotineEquipmentDefName);
        }
    }


    public static class Buffs
    {
        internal static void SetupBuffs()
        {
            DisableEliteBuff.SetupBuff();
            AllowEliteBuffReAdded.SetupBuff();
            WasEthereal.SetupBuff();
            PreventedInitialHeal.SetupBuff();
            PreventedInstantShieldRecharge.SetupBuff();
        }


        public static class DisableEliteBuff
        {
            public static BuffDef bdDisableElite;


            internal static void SetupBuff()
            {
                bdDisableElite = ScriptableObject.CreateInstance<BuffDef>();
                bdDisableElite.name = "bdDisableElite";
                bdDisableElite.isHidden = false;
                bdDisableElite.canStack = false;
                bdDisableElite.isDebuff = true;
                bdDisableElite.flags = BuffDef.Flags.IncludeInRandomBuff;
                bdDisableElite.ignoreGrowthNectar = true;
                bdDisableElite.buffColor = Color.white;
                bdDisableElite.iconSprite = ModAssets.AssetBundleAssets.GuillotineIconOrange;
                ContentAddition.AddBuffDef(bdDisableElite);
            }
        }


        public static class AllowEliteBuffReAdded
        {
            public static BuffDef bdReAddEliteBuff;


            internal static void SetupBuff()
            {
                bdReAddEliteBuff = ScriptableObject.CreateInstance<BuffDef>();
                bdReAddEliteBuff.name = "bdReAddEliteBuff";
                bdReAddEliteBuff.isHidden = true;
                bdReAddEliteBuff.canStack = false;
                bdReAddEliteBuff.isDebuff = false;
                bdReAddEliteBuff.flags = BuffDef.Flags.NONE;
                bdReAddEliteBuff.ignoreGrowthNectar = true;
                bdReAddEliteBuff.buffColor = Color.gray;
                bdReAddEliteBuff.iconSprite = ModAssets.AssetBundleAssets.GuillotineIconOrange;
                ContentAddition.AddBuffDef(bdReAddEliteBuff);
            }
        }


        public static class WasEthereal
        {
            public static BuffDef bdWasEthereal;


            internal static void SetupBuff()
            {
                bdWasEthereal = ScriptableObject.CreateInstance<BuffDef>();
                bdWasEthereal.name = "bdWasEthereal";
                bdWasEthereal.isHidden = true;
                bdWasEthereal.canStack = false;
                bdWasEthereal.isDebuff = false;
                bdWasEthereal.flags = BuffDef.Flags.NONE;
                bdWasEthereal.ignoreGrowthNectar = true;
                bdWasEthereal.buffColor = Color.green;
                bdWasEthereal.iconSprite = ModAssets.AssetBundleAssets.GuillotineIconOrange;
                ContentAddition.AddBuffDef(bdWasEthereal);
            }
        }


        public static class PreventedInstantShieldRecharge
        {
            public static BuffDef bdPreventedInstantShieldRecharge;


            internal static void SetupBuff()
            {
                bdPreventedInstantShieldRecharge = ScriptableObject.CreateInstance<BuffDef>();
                bdPreventedInstantShieldRecharge.name = "bdPreventedInstantShieldRecharge";
                bdPreventedInstantShieldRecharge.isHidden = true;
                bdPreventedInstantShieldRecharge.canStack = false;
                bdPreventedInstantShieldRecharge.isDebuff = false;
                bdPreventedInstantShieldRecharge.flags = BuffDef.Flags.NONE;
                bdPreventedInstantShieldRecharge.ignoreGrowthNectar = true;
                bdPreventedInstantShieldRecharge.buffColor = Color.blue;
                bdPreventedInstantShieldRecharge.iconSprite = ModAssets.AssetBundleAssets.GuillotineIconOrange;
                ContentAddition.AddBuffDef(bdPreventedInstantShieldRecharge);
            }
        }


        public static class PreventedInitialHeal
        {
            public static BuffDef bdPreventedInitialHeal;


            internal static void SetupBuff()
            {
                bdPreventedInitialHeal = ScriptableObject.CreateInstance<BuffDef>();
                bdPreventedInitialHeal.name = "bdPreventedInitialHeal";
                bdPreventedInitialHeal.isHidden = true;
                bdPreventedInitialHeal.canStack = false;
                bdPreventedInitialHeal.isDebuff = false;
                bdPreventedInitialHeal.flags = BuffDef.Flags.NONE;
                bdPreventedInitialHeal.ignoreGrowthNectar = true;
                bdPreventedInitialHeal.buffColor = Color.red;
                bdPreventedInitialHeal.iconSprite = ModAssets.AssetBundleAssets.GuillotineIconOrange;
                ContentAddition.AddBuffDef(bdPreventedInitialHeal);
            }
        }


        [SystemInitializer(dependencies: typeof(BuffCatalog))]
        private static void GetAllEliteBuffDefs()
        {
            foreach (BuffDef buffDef in BuffCatalog.buffDefs)
            {
                if (buffDef.isElite)
                {
                    _allEliteBuffDefs.Add(buffDef);
                }
            }
        }
    }


    private static class Hooks
    {
        internal static void Setup()
        {
            Mdh.RoR2.EquipmentSlot.PerformEquipmentAction.ControlFlowPrefix(EquipmentSlot_PerformEquipmentAction);
            Mdh.RoR2.EquipmentSlot.UpdateTargets.ControlFlowPrefix(Targeting);
            Mdh.RoR2.CharacterBody.SetBuffCount.ControlFlowPrefix(SetBuffCount);
            Mdh.RoR2.CharacterBody.RecalculateStats.ILHook(ScaryHook);
        }

        private static void ScaryHook(ILManipulationInfo info)
        {
            ILWeaver w = new(info);
            int healthDiffVarNumber = 0;
            int shieldDiffVarNumber = 0;
            ILLabel skipHeal = null!;
            ILLabel skipShieldRecharge = null!;


            // going to:
            /*
             * float num126 = maxHealth - num76;
             * float num127 = maxShield - num77;
             */
            w.MatchRelaxed(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxHealth"),
                x => x.MatchLdloc(out _),
                x => x.MatchSub(),
                x => x.MatchStloc(out healthDiffVarNumber),
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield"),
                x => x.MatchLdloc(out _),
                x => x.MatchSub(),
                x => x.MatchStloc(out shieldDiffVarNumber),
                x => x.MatchLdloc(healthDiffVarNumber),
                x => x.MatchLdcR4(0.0f),
                x => x.MatchBleUn(out skipHeal) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .InsertAfterCurrent(
                w.Create(OpCodes.Ldarg_0), // CharacterBody
                w.CreateDelegateCall((CharacterBody characterBody) =>
                {
                    if (
                        characterBody.HasBuff(Buffs.DisableEliteBuff.bdDisableElite)
                        && !characterBody.HasBuff(Buffs.PreventedInitialHeal.bdPreventedInitialHeal)
                        && ConfigOptions.Guillotine.RemoveHealFromLostShield.Value
                        && NetworkServer.active
                    )
                    {
                        characterBody.GetTimedBuffTotalDurationForIndex(Buffs.DisableEliteBuff.bdDisableElite.buffIndex, out float disableEliteTimeLeft);
                        characterBody.AddTimedBuff(Buffs.PreventedInitialHeal.bdPreventedInitialHeal, disableEliteTimeLeft);
                        return true;
                    }
                    return false;
                }),
                w.Create(OpCodes.Brtrue, skipHeal)
            );


            w.MatchRelaxed(
                x => x.MatchLdloc(shieldDiffVarNumber),
                x => x.MatchLdcR4(0.0f),
                x => x.MatchBleUn(out skipShieldRecharge) && w.SetCurrentTo(x)
            ).ThrowIfFailure()
            .InsertAfterCurrent(
                w.Create(OpCodes.Ldarg_0), // CharacterBody
                w.CreateDelegateCall((CharacterBody characterBody) =>
                {
                    if (
                        characterBody.HasBuff(Buffs.AllowEliteBuffReAdded.bdReAddEliteBuff)
                        && !characterBody.HasBuff(Buffs.PreventedInstantShieldRecharge.bdPreventedInstantShieldRecharge)
                        && ConfigOptions.Guillotine.RemoveHealFromLostShield.Value
                        && NetworkServer.active
                    )
                    {
                        characterBody.GetTimedBuffTotalDurationForIndex(Buffs.AllowEliteBuffReAdded.bdReAddEliteBuff.buffIndex, out float allowEliteTimeLeft);
                        characterBody.AddTimedBuff(Buffs.PreventedInstantShieldRecharge.bdPreventedInstantShieldRecharge, allowEliteTimeLeft);
                        return true;
                    }
                    return false;
                }),
                w.Create(OpCodes.Brtrue, skipShieldRecharge)
            );
        }


        private static ReturnFlow SetBuffCount(CharacterBody self, ref BuffIndex buffIndex, ref int newCount)
        {
            if (self == null || self.inventory == null || !NetworkServer.active)
            {
                return ReturnFlow.None;
            }
            

            if (buffIndex == Buffs.DisableEliteBuff.bdDisableElite.buffIndex)
            {
                if (newCount > 0)
                {
                    // not using the equipment passiveBuffDef here in case of wake of vultures elite buffs 
                    foreach (BuffDef eliteBuffDef in _allEliteBuffDefs)
                    {
                        if (self.HasBuff(eliteBuffDef))
                        {
                            self.RemoveBuff(eliteBuffDef);
                        }
                    }
                    if (ModSupport.Starstorm2.Starstorm2Mod.ModIsRunning)
                    {
                        ModSupport.Starstorm2.GuillotineSupport.TrySetEtherealEliteEffectsState(self, false);
                        ModSupport.Starstorm2.GuillotineSupport.TrySetUltraEliteEffectsState(self, false);
                    }
                    return ReturnFlow.None;
                }
                else
                {
                    if (self.inventory.currentEquipmentIndex != EquipmentIndex.None)
                    {
                        BuffDef equipmentBuffDef = EquipmentCatalog.GetEquipmentDef(self.inventory.currentEquipmentIndex).passiveBuffDef;
                        if (equipmentBuffDef != null && equipmentBuffDef.isElite)
                        {
                            self.AddTimedBuff(Buffs.AllowEliteBuffReAdded.bdReAddEliteBuff, 0.2f);
                            self.AddBuff(equipmentBuffDef);
                        }
                    }
                    if (ModSupport.Starstorm2.Starstorm2Mod.ModIsRunning)
                    {
                        ModSupport.Starstorm2.GuillotineSupport.TrySetEtherealEliteEffectsState(self, true);
                        ModSupport.Starstorm2.GuillotineSupport.TrySetUltraEliteEffectsState(self, true);
                    }
                }
            }
            // prevent new elite buffs from being added while the debuff is up
            // this will probably never run unless a player picks up an aspect or procs wake of vultures while having the debuff
            else if (
                self.HasBuff(Buffs.DisableEliteBuff.bdDisableElite)
                && BuffCatalog.GetBuffDef(buffIndex).isElite
                && !self.HasBuff(Buffs.AllowEliteBuffReAdded.bdReAddEliteBuff)
            )
            {
                return ReturnFlow.SkipOriginal;
            }


            return ReturnFlow.None;
        }


        private static ReturnFlow Targeting(EquipmentSlot self, ref EquipmentIndex targetingEquipmentIndex, ref bool userShouldAnticipateTarget)
        {
            if (targetingEquipmentIndex != Equipment.eiGuillotine)
            {
                return ReturnFlow.None;
            }


            self.ConfigureTargetFinderForEnemies();
            HurtBox source = self.targetFinder.GetResults().FirstOrDefault();
            self.currentTarget = new EquipmentSlot.UserTargetInfo(source);
            if (self.currentTarget.transformToIndicateAt == null)
            {
                return ReturnFlow.None;
            }
            self.targetIndicator.visualizerPrefab = ModAssets.EditedVanillaAssets.GuillotineTarget.GuillotineTargetingIndicator;
            self.targetIndicator.active = true;
            self.targetIndicator.visualizerPrefab.SetActive(true); // i shouldnt need to do this but i have to
            self.targetIndicator.targetTransform = self.currentTarget.transformToIndicateAt;


            return ReturnFlow.SkipOriginal;
        }


        private static ReturnFlow EquipmentSlot_PerformEquipmentAction(EquipmentSlot self, ref EquipmentDef equipmentDef, ref bool returnValue)
        {
            if (equipmentDef != Equipment.ceGuillotine.EquipmentDef)
            {
                returnValue = false;
                return ReturnFlow.None;
            }
            if (self.currentTarget.hurtBox == null)
            {
                returnValue = false;
                return ReturnFlow.None;
            }

            
            self.UpdateTargets(Equipment.eiGuillotine, false);
            self.subcooldownTimer = 0.2f;
            // TODO why tf do these sounds not actually play
            Util.PlaySound("Play_merc_m1_hard_swing", self.currentTarget.transformToIndicateAt.gameObject);
            Util.PlaySound("Play_halcyonite_skill1_swing", self.currentTarget.transformToIndicateAt.gameObject);
            self.currentTarget.hurtBox.healthComponent.body.AddTimedBuffOnServer(Buffs.DisableEliteBuff.bdDisableElite, _debuffDuration);
            DamageInfo procDamageInfo = new()
            {
                attacker = self.characterBody.gameObject,
                crit = Util.CheckRoll(self.characterBody.crit, self.characterBody.master),
                damageType = ModUtil.GenericEquipmentDamageType,
                inflictedHurtbox = self.currentTarget.hurtBox,
                inflictor = self.characterBody.gameObject,
                position = self.currentTarget.transformToIndicateAt.position,
                procCoefficient = 1,
                damage = self.characterBody.damage * _damageCoefficient,
                damageColorIndex = DamageColorIndex.Item,
            };
            self.currentTarget.hurtBox.healthComponent.TakeDamage(procDamageInfo);
            self.InvalidateCurrentTarget();


            returnValue = true;
            return ReturnFlow.SkipOriginal;
        }
    }
}