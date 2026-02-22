/* using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using RoR2;
using RiskyTweaks.Tweaks.Survivors.Toolbot;
using ReheatedItems.ItemChanges;

namespace ReheatedItems.ItemChanges
{
    internal static class Planula
    {
        [MonoDetourTargets(typeof(HealthComponent))]
        private static class RemoveVanillaEffect
        {
            [MonoDetourHookInitialize]
            internal static void Setup()
            {
                Mdh.RoR2.HealthComponent.TakeDamageProcess.ILHook(DeleteVanillaEffect);
            }

            // "member names cannot be the same as their enclosing type" ok fine!!!!!!!!!!
            private static void DeleteVanillaEffect(ILManipulationInfo info)
            {
                ILWeaver w = new(info);
                ILLabel skipVanillaEffect = w.DefineLabel();


                w.MatchRelaxed(
                    x => x.MatchLdarg(0) && w.SetCurrentTo(x),
                    x => x.MatchLdflda<HealthComponent>("itemCounts"),
                    x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "parentEgg"),
                    x => x.MatchLdcI4(0),
                    x => x.MatchBle(out skipVanillaEffect) // nice of a jump location to be right where im matching
                ).ThrowIfFailure()
                .InsertBeforeCurrentStealLabels(
                    w.Create(OpCodes.Br, skipVanillaEffect)
                );
            }
        }



        private static DeployableSlot _incubatorDeployable;
        private const int _maxIncubators = 5;
        private static int GetMaxIncubatorCount(CharacterMaster master, int countMultiplier)
        {
            return _maxIncubators;
        }



        internal static void Setup()
        {
            if (!ConfigOptions.Planula.EnableEdit.Value)
            {
                return;
            }


            RemoveVanillaEffect.Setup();
            _incubatorDeployable = R2API.DeployableAPI.RegisterDeployableSlot(GetMaxIncubatorCount);
            GlobalEventManager.onCharacterDeathGlobal += NewEffect;
        }


        private static void NewEffect(DamageReport damageReport)
        {
            if (damageReport.attackerBody == null || damageReport.attackerBody.inventory == null)
            {
                return;
            }
            int planulaCount = damageReport.attackerBody.inventory.GetItemCountEffective(RoR2Content.Items.ParentEgg);
            if (planulaCount < 1)
            {
                return;
            }
            if (damageReport.attackerMaster.IsDeployableLimited(_incubatorDeployable))
            {
                return;
            }



        }
    }
} */