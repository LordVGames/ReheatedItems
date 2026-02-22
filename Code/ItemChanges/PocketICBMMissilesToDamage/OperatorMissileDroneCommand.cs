using System;
using UnityEngine;
using RoR2;
using Mono.Cecil.Cil;
using MonoDetour;
using MonoDetour.Cil;
using MonoDetour.DetourTypes;
using MonoDetour.HookGen;
using MonoMod.Cil;
using RoR2.Orbs;
using System.Runtime.CompilerServices;
namespace ReheatedItems.ItemChanges.PocketICBMMissilesToDamage;


[MonoDetourTargets(typeof(EntityStates.Drone.Command.CommandFireMissiles))]
internal static class OperatorMissileDroneCommand
{
    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.PocketICBM.ChangeGenericMissileEffect.Value)
        {
            return;
        }

        Mdh.EntityStates.Drone.Command.CommandFireMissiles.FireMissile.ILHook(CommandFireMissiles_FireMissile);
    }

    private static void CommandFireMissiles_FireMissile(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ModUtil.SkipNext2FireProjectiles(w);
    }
}