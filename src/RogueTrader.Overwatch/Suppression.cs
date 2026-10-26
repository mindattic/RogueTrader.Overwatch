using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Controllers.Projectiles;
using Kingmaker.Controllers.TurnBased;
using Kingmaker.Controllers.Units;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Pathfinding;
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

// Suppression: the ability puts a Suppressed buff on one enemy. Leaving the square it stands on, by any means
// (walking, a charge or jump, being pushed), draws one real attack from the buff's caster: once per command, so a
// long move is one shot, not one per square. During its turn the caster also fires cosmetic, always-missing shots.
// Nothing here touches PartOverwatch, so the target's actions never trigger it. The buff's TurnStartOrCombatEnd end
// condition removes it when the caster's next turn starts.
internal sealed class Suppression : IUnitBuffHandler, IEntityPositionChangedHandler, IAreaHandler
{
    public const string BuffGuid = "c6af42a8c37040a49f86559e80959c5f";

    private const float MissOffset = 1.6f;
    private const float MissDistance = 30f;

    public static readonly Suppression Instance = new Suppression();

    // The square each suppressed target holds, and the command that already drew its shot for leaving it.
    private readonly Dictionary<BaseUnitEntity, CustomGridNodeBase> m_Anchors = new Dictionary<BaseUnitEntity, CustomGridNodeBase>();
    private readonly Dictionary<BaseUnitEntity, AbstractUnitCommand> m_ShotForCommand = new Dictionary<BaseUnitEntity, AbstractUnitCommand>();
    private bool m_NeedsScan = true;
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

    private static CustomGridNodeBase Square(BaseUnitEntity unit) => unit.CurrentUnwalkableNode as CustomGridNodeBase;

    private static AbilityData FirstWeaponAbility(BaseUnitEntity unit) => Abilities.RangedAttack(unit);

    public void HandleBuffDidAdded(Buff buff)
    {
        try
        {
            if (buff?.Blueprint?.AssetGuid == BuffGuid && buff.Owner is BaseUnitEntity target)
                m_Anchors[target] = Square(target);
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "Suppression: buff added");
        }
    }

    public void HandleBuffDidRemoved(Buff buff)
    {
        try
        {
            if (buff?.Blueprint?.AssetGuid == BuffGuid && buff.Owner is BaseUnitEntity target && !SuppressedBuffs(target).Any(b => b != buff))
            {
                m_Anchors.Remove(target);
                m_ShotForCommand.Remove(target);
            }
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "Suppression: buff removed");
        }
    }

    public void HandleBuffRankIncreased(Buff buff) { }

    public void HandleBuffRankDecreased(Buff buff) { }

    public void HandleBuffIsSuppressedChanged(Buff buff) { }

    // After a load the anchors are rebuilt from the saved buffs (see Tick).
    public void OnAreaDidLoad() => m_NeedsScan = true;

    public void OnAreaBeginUnloading()
    {
        m_Anchors.Clear();
        m_ShotForCommand.Clear();
    }

    public void HandleEntityPositionChanged()
    {
        try
        {
            if (!(EventInvokerExtensions.Entity is BaseUnitEntity target) || !m_Anchors.TryGetValue(target, out var anchor))
                return;
            var square = Square(target);
            if (square == null || square == anchor)
                return;
            m_Anchors[target] = square;
            if (anchor == null || !TurnController.IsInTurnBasedCombat())
                return;
            var command = target.Commands.Current;
            if (command != null && m_ShotForCommand.TryGetValue(target, out var shotFor) && shotFor == command)
                return;
            m_ShotForCommand[target] = command;
            foreach (var caster in Suppressors(target))
                m_ShotsToFire.Add((caster, target));
        }
        catch (Exception ex)
        {
            PFLog.Mods.Exception(ex, "Suppression: position changed");
        }
    }

    private void ScanIfNeeded()
    {
        if (!m_NeedsScan)
            return;
        m_NeedsScan = false;
        foreach (var unit in Game.Instance.State.AllBaseAwakeUnits)
            if (!m_Anchors.ContainsKey(unit) && SuppressedBuffs(unit).Any())
                m_Anchors[unit] = Square(unit);
    }

    // Runs in the same simulation tick as the engine's own overwatch shots.
    public void Tick()
    {
        if (!TurnController.IsInTurnBasedCombat())
        {
            m_ShotsToFire.Clear();
            m_NextCosmeticShot.Clear();
            return;
        }
        ScanIfNeeded();
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
