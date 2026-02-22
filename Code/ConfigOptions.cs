using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Configuration;
using MiscFixes.Modules;
namespace ReheatedItems;


public static class ConfigOptions
{
    public static class VoidDios
    {
        private const string _sectionName = "Pluripotent Larva";
        public static ConfigEntry<bool> EnableEdit;
        public static ConfigEntry<string> RespawnableBodiesList;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            RespawnableBodiesList = config.BindOption(
                _sectionName,
                "Possible Respawn Bodies",
                "Specify what bodies you should have the chance to respawn as, all having equal chance. Separate each by a comma and no spaces.",
                "VoidMegaCrabAllyBody"
            );
        }
    }



    public static class ATG
    {
        private const string _sectionName = "ATG";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                "Enable the ATG change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class PocketICBM
    {
        private const string _sectionName = "Pocket ICBM";
        public static ConfigEntry<bool> EnableEdit;
        public static ConfigEntry<bool> ChangeATGEffect;
        public static ConfigEntry<bool> ChangeArmedBackpackEffect;
        public static ConfigEntry<bool> ChangeGenericMissileEffect;
        public static ConfigEntry<bool> ChangePlasmaShrimpEffect;
        public static ConfigEntry<bool> ChangeRocketSurvivorEffect;
        public static ConfigEntry<bool> ChangeRiskyTweaksScrapLauncherEffect;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                "Enable the Pocket I.C.B.M change? Disabling this will also disable all effect options below.",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            ChangeArmedBackpackEffect = config.BindOption(
                _sectionName,
                "Change Armed Backpack Effect",
                "Make Armed Backpack's ICBM effect triple missile damage instead of firing 2 extra missiles? This will help performance!",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            ChangeATGEffect = config.BindOption(
                _sectionName,
                "Change ATG Effect",
                "Make ATG's ICBM effect triple missile damage instead of firing 2 extra missiles? This will help performance!",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            ChangeGenericMissileEffect = config.BindOption(
                _sectionName,
                "Change Effect for generic missiles",
                "Make DML's and Engineer Harpoons' ICBM effect triple missile damage instead of firing 2 extra missiles? This will help performance!",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            ChangePlasmaShrimpEffect = config.BindOption(
                _sectionName,
                "Change Plasma Shrimp Effect",
                "Make Plasma Shrimp's ICBM effect triple missile damage instead of firing 2 extra missiles? This will help performance!",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            ChangeRocketSurvivorEffect = config.BindOption(
                _sectionName,
                "Change The Rocket Survivor Effect",
                "Make Rocket's ICBM effect triple missile damage instead of firing 2 extra missiles? This will help performance!",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            ChangeRiskyTweaksScrapLauncherEffect = config.BindOption(
                _sectionName,
                "Change RiskyTweaks MUL-T Scrap Launcher Effect",
                "Make RiskyTweaks' MUL-T scrap launcher ICBM synergy effect triple missile damage instead of firing 2 extra missiles? This will help performance!",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class BottledChaos
    {
        private const string _sectionName = "Bottled Chaos";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class Planula
    {
        public static ConfigEntry<bool> EnableEdit;
    }



    public static class ElectricBoomerang
    {
        private const string _sectionName = "Electric Boomerang";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                "Enable the Electric Boomerang change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class MoltenPerforator
    {
        private const string _sectionName = "Molten Perforator";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class ExecutiveCard
    {
        private const string _sectionName = "Executive Card";
        public static ConfigEntry<bool> EnableEdit;
        public static ConfigEntry<bool> AddCreditCardToBottledChaos;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            AddCreditCardToBottledChaos = config.BindOption(
                _sectionName,
                "Add edited effect to the Bottled Chaos pool",
                "Should the new effect for Executive Card be added to the equipment effect pool for Bottled Chaos?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class Polylute
    {
        private const string _sectionName = "Polylute";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class WarBonds
    {
        private const string _sectionName = "War Bonds";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class StunGrenade
    {
        private const string _sectionName = "Stun Grenade";
        public static ConfigEntry<bool> EnableEdit;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
        }
    }



    public static class VoidCrit
    {
        private const string _sectionName = "Lost-Seers Lenses";
        public static ConfigEntry<bool> EnableEdit;
        public static ConfigEntry<bool> UseCritMultiplier;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            UseCritMultiplier = config.BindOption(
                _sectionName,
                "Use Crit Multiplier",
                "Should the damage dealt by a void crit be affected by your crit damage multiplier?",
                true
            );
        }
    }


    public static class Guillotine
    {
        private const string _sectionName = "Guillotine";
        public static ConfigEntry<bool> EnableEdit;
        public static ConfigEntry<bool> RemoveHealFromLostShield;

        internal static void BindConfigOptions(ConfigFile config)
        {
            EnableEdit = config.BindOption(
                _sectionName,
                "Enable Item Change",
                $"Enable the {_sectionName} change?",
                true,
                Extensions.ConfigFlags.RestartRequired
            );
            RemoveHealFromLostShield = config.BindOption(
                _sectionName,
                "Prevent heals from elite shield loss",
                "When an elite that has shield loses or re-gains their elite effect, their health or shield respectively will heal to accomodate the sudden change. This option will prevent the health accomodation, and make elite shields restore like normal when re-gained.",
                true
            );
        }
    }



    internal static void BindAllConfigOptions(ConfigFile config)
    {
        ATG.BindConfigOptions(config);
        BottledChaos.BindConfigOptions(config);
        ElectricBoomerang.BindConfigOptions(config);
        ExecutiveCard.BindConfigOptions(config);
        Guillotine.BindConfigOptions(config);
        VoidCrit.BindConfigOptions(config); // Lost Seers Lenses
        MoltenPerforator.BindConfigOptions(config);
        VoidDios.BindConfigOptions(config); // Pluripotent Larva
        PocketICBM.BindConfigOptions(config);
        Polylute.BindConfigOptions(config);
        StunGrenade.BindConfigOptions(config);
        WarBonds.BindConfigOptions(config);
    }
}
