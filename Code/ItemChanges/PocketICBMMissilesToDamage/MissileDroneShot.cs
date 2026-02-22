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


[MonoDetourTargets(typeof(EntityStates.Drone.DroneWeapon.FireMissileBarrage))]
internal static class MissileDroneShot
{
    [MonoDetourHookInitialize]
    private static void Setup()
    {
        if (!ConfigOptions.PocketICBM.ChangeGenericMissileEffect.Value)
        {
            return;
        }

        Mdh.EntityStates.Drone.DroneWeapon.FireMissileBarrage.FireMissile.ILHook(FireMissileBarrage_FireMissile);
    }

    private static void FireMissileBarrage_FireMissile(ILManipulationInfo info)
    {
        ILWeaver w = new(info);
        ModUtil.SkipNext2FireProjectiles(w);
    }
}