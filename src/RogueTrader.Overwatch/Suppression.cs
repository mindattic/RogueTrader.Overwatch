using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.PubSubSystem;
using Kingmaker.PubSubSystem.Core;
using Kingmaker.UnitLogic.Abilities;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using Kingmaker.Utility;
using Kingmaker.Visual.Animation.Kingmaker;
using UnityEngine;

namespace RogueTraderOverwatch;

// Suppression: the ability puts a Suppressed buff on one enemy. Each move command it starts draws one
// real attack from the buff's caster, on its first step. During its turn the caster also fires cosmetic,
// always-missing shots. Nothing here touches PartOverwatch, so the target's actions never trigger it.
// The buff's TurnStartOrCombatEnd end condition removes it when the caster's next turn starts.
internal sealed class Suppression : IUnitCommandStartHandler, IEntityPositionChangedHandler
{
    public const string BuffGuid = "c6af42a8c37040a49f86559e80959c5f";

    private const float MissOffset = 1.6f;
    private const float MissDistance = 30f;

    public static readonly Suppression Instance = new Suppression();

    // Targets that started a move command and haven't drawn this move's shot yet.
    private readonly HashSet<BaseUnitEntity> m_PendingMoves = new HashSet<BaseUnitEntity>();
    private readonly List<(BaseUnitEntity Caster, BaseUnitEntity Target)> m_ShotsToFire = new List<(BaseUnitEntity, BaseUnitEntity)>();
    private readonly Dictionary<BaseUnitEntity, float> m_NextCosmeticShot = new Dictionary<BaseUnitEntity, float>();
    private MechanicEntity m_CosmeticTurnUnit;

    private static IEnumerable<Buff> SuppressedBuffs(MechanicEntity unit) =>
        unit?.Buffs?.Enumerable.Where(b => b.Blueprint?.AssetGuid == BuffGuid) ?? Enumerable.Empty<Buff>();

    private static IEnumerable<BaseUnitEntity> Suppressors(BaseUnitEntity target) =>
        SuppressedBuffs(target)
            .Select(b => b.Context?.MaybeCaster as BaseUnitEntity)
            .Where(c => c != null && !c.IsDeadOrUnconscious)
            .Distinct();

    private static bool IsMove(AbstractUnitCommand command) =>
        command is UnitMoveTo || command is UnitMoveToProper || command is UnitMoveAlongPath || command is UnitMoveContinuously;

    private static AbilityData FirstWeaponAbility(BaseUnitEntity unit) => Abilities.RangedAttack(unit);

    public void HandleUnitCommandDidStart(AbstractUnitCommand command)
    {
        try
        {
            if (TurnController.IsInTurnBasedCombat() && IsMove(command) && command.Executor is BaseUnitEntity target && Suppressors(target).Any())
                m_PendingMoves.Add(target);
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "Suppression: move start");
        }
    }

    public void HandleEntityPositionChanged()
    {
        try
        {
            if (EventInvokerExtensions.Entity is BaseUnitEntity target && m_PendingMoves.Remove(target))
                foreach (var caster in Suppressors(target))
                    m_ShotsToFire.Add((caster, target));
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "Suppression: position changed");
        }
    }

    // Runs in the same simulation tick as the engine's own overwatch shots.
    public void Tick()
    {
        if (!TurnController.IsInTurnBasedCombat())
        {
            m_PendingMoves.Clear();
            m_ShotsToFire.Clear();
            m_NextCosmeticShot.Clear();
            return;
        }
        FireQueuedShots();
        CosmeticFire();
    }

    private void FireQueuedShots()
    {
        if (m_ShotsToFire.Count == 0)
            return;
        var shots = m_ShotsToFire.ToList();
        m_ShotsToFire.Clear();
        foreach (var (caster, target) in shots)
        {
            try
            {
                if (caster.IsDeadOrUnconscious || target.IsDeadOrUnconscious)
                    continue;
                var ability = FirstWeaponAbility(caster);
                if (ability == null || !ability.CanTarget(target, out _))
                    continue;
                caster.Commands.Run(new UnitOverwatchAttackParams(ability, new TargetWrapper(target)));
                m_NextCosmeticShot[caster] = Time.time + UnityEngine.Random.Range(1.5f, 2.5f);
            }
            catch (Exception ex)
            {
                PFLog.Mods.Exception(ex, "Suppression: attack of opportunity");
            }
        }
    }

    private void CosmeticFire()
    {
        if (!(Game.Instance.TurnController.CurrentUnit is BaseUnitEntity target) || target.IsDeadOrUnconscious)
            return;
        if (target != m_CosmeticTurnUnit)
        {
            m_CosmeticTurnUnit = target;
            m_NextCosmeticShot.Clear();
        }
        foreach (var caster in Suppressors(target))
        {
            try
            {
                if (!m_NextCosmeticShot.TryGetValue(caster, out var next))
                {
                    m_NextCosmeticShot[caster] = Time.time + UnityEngine.Random.Range(0.6f, 1.2f);
                    continue;
                }
                // Pause while the caster is busy, e.g. playing a real attack of opportunity.
                if (Time.time < next || !caster.Commands.Empty || !caster.HasLOS(target))
                    continue;
                ShootToMiss(caster, target);
                m_NextCosmeticShot[caster] = Time.time + UnityEngine.Random.Range(1.5f, 2.5f);
            }
            catch (Exception ex)
            {
                PFLog.Mods.Exception(ex, "Suppression: cosmetic fire");
                m_NextCosmeticShot[caster] = Time.time + 5f;
            }
        }
    }

    // Visual only: the shoot animation plus a projectile aimed past the target, with no hit delivered.
    // Uses Unity's random, not the game's seeded one, so the game state and its hash are untouched.
    private static void ShootToMiss(BaseUnitEntity caster, BaseUnitEntity target)
    {
        var ability = FirstWeaponAbility(caster);
        var weapon = ability?.Weapon;
        if (ability == null || weapon == null)
            return;

        var from = caster.EyePosition;
        var aim = target.Position + Vector3.up;
        var flat = aim - from;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f)
            return;
        var side = Vector3.Cross(Vector3.up, flat.normalized) * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
        var missAt = aim + side * UnityEngine.Random.Range(MissOffset, MissOffset * 1.5f) + Vector3.up * UnityEngine.Random.Range(0.2f, 1.2f);
        var missPoint = from + (missAt - from).normalized * MissDistance;

        var animation = caster.View?.AnimationManager;
        if (animation != null)
        {
            animation.UseAbilityDirection = Quaternion.LookRotation(flat).eulerAngles.y;
            var handle = animation.CreateHandle(UnitAnimationType.MainHandAttack, errorOnEmpty: false);
            if (handle != null)
            {
                handle.AttackWeaponStyle = caster.View.HandsEquipment?.GetWeaponStyleForWeapon(weapon) ?? weapon.GetAnimationStyle();
                handle.AttackTargetDistance = flat.magnitude;
                handle.IsBurst = false;
                animation.Execute(handle);
            }
        }

        var projectiles = ability.ProjectileVariants?.ToList();
        if (projectiles == null || projectiles.Count == 0)
            return;
        new ProjectileLauncher(projectiles[UnityEngine.Random.Range(0, projectiles.Count)], caster, new TargetWrapper(missPoint))
            .Ability(ability)
            .SetDoNotDeliverHit(true)
            .SetDoNotPlayHitEffect(true)
            .Launch();
    }
}

[HarmonyPatch(typeof(OverwatchController), nameof(OverwatchController.Tick))]
internal static class SuppressionTickPatch
{
    private static void Postfix()
    {
        try
        {
            Suppression.Instance.Tick();
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "Suppression: tick");
        }
    }
}
