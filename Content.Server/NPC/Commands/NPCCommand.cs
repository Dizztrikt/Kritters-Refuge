using Robust.Shared.Toolshed;
using Content.Server.NPC.HTN;

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Prototypes;
using Content.Shared.CombatMode;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.NPC.Systems;

namespace Content.Server.NPC.Commands;

// Kritters: replaced `npcadd` with a toolshed command `npc:setBehavior`
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed partial class NPCCommand : ToolshedCommand
{

    [Dependency] private IEntityManager _entities = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntitySystemManager _sysManager = default!;

    [CommandImplementation("setBehavior")]
    public void SetBehaviorIter (
        [PipedArgument] IEnumerable<EntityUid> entities,
        [CommandArgument] ProtoId<HTNCompoundPrototype> behavior
    )
    {
        foreach (var entityUid in entities)
        {
            SetBehavior(entityUid, behavior);
        }
    }

    [CommandImplementation("setBehavior")]
    public void SetBehavior(
        [PipedArgument] EntityUid entity,
        [CommandArgument] ProtoId<HTNCompoundPrototype> behavior
    )
    {
        var htnComponent = _entities.EnsureComponent<HTNComponent>(entity);

        htnComponent.RootTask = new HTNCompoundTask()
        {
            Task = behavior.ToString(),
        };
    }

    [CommandImplementation("setCombatMode")]
    public void SetCombatModeIter (
        [PipedArgument] IEnumerable<EntityUid> entities,
        [CommandArgument] bool enabled
    )
    {
        foreach (var entityUid in entities)
        {
            SetCombatMode(entityUid, enabled);
        }
    }

    [CommandImplementation("isEnabled")]
    public bool GetHTNRunning(
        [PipedArgument] EntityUid entity
    )
    {
        if (_entities.TryGetComponent<HTNComponent>(entity, out var htnComponent))
            return htnComponent.Enabled;
        return false;
    }

    [CommandImplementation("setEnabled")]
    public void SetHTNRunning(
        [PipedArgument] EntityUid entity,
        [CommandArgument] bool enabled
    )
    {
        var htnComponent = _entities.EnsureComponent<HTNComponent>(entity);
        var htnSystem = _sysManager.GetEntitySystem<HTNSystem>();

        htnSystem.SetHTNEnabled((entity,htnComponent),enabled);
    }

    [CommandImplementation("setCombatMode")]
    public void SetCombatMode(
        [PipedArgument] EntityUid entity,
        [CommandArgument] bool enabled
    )
    {
        var combatComponent = _entities.EnsureComponent<CombatModeComponent>(entity);
        var combatSystem = _sysManager.GetEntitySystem<SharedCombatModeSystem>();
        combatSystem.SetInCombatMode(entity, enabled, combatComponent);
    }

    [CommandImplementation("inCombatMode")]
    public bool InCombatMode(
        [PipedArgument] EntityUid entity
    )
    {
        if (_entities.TryGetComponent<CombatModeComponent>(entity, out var combatComponent))
            return combatComponent.IsInCombatMode;
        return false;
    }

    [CommandImplementation("joinFaction")]
    public void AddToFaction(
        [PipedArgument] EntityUid entity,
        [CommandArgument] ProtoId<NpcFactionPrototype> faction
        )
    {
        var factionSystem = _sysManager.GetEntitySystem<NpcFactionSystem>();
        factionSystem.AddFaction(entity, faction);
    }

    [CommandImplementation("leaveFaction")]
    public void RemoveFromFaction(
        [PipedArgument] EntityUid entity,
        [CommandArgument] ProtoId<NpcFactionPrototype> faction
    )
    {
        var factionSystem = _sysManager.GetEntitySystem<NpcFactionSystem>();
        factionSystem.RemoveFaction(entity, faction);
    }

    [CommandImplementation("factions")]
    public IEnumerable<string> printFactions(
        [PipedArgument] EntityUid entity
    )
    {
        if (_entities.TryGetComponent<NpcFactionMemberComponent>(entity, out var member))
        {
            foreach(var hasFaction in member.Factions)
            {
                yield return "member of: " + hasFaction.ToString();
            }

            foreach(var hasFaction in member.FriendlyFactions)
            {
                yield return "friendly: " + hasFaction.ToString();
            }

            foreach(var hasFaction in member.HostileFactions)
            {
                yield return "hostile: " + hasFaction.ToString();
            }
        }
    }

    [CommandImplementation("leaveAllFactions")]
    public void FactionClear(
        [PipedArgument] EntityUid entity
    )
    {
        _sysManager.GetEntitySystem<NpcFactionSystem>().ClearFactions(entity);
    }

    [CommandImplementation("isFactionMember")]
    public bool IsInFaction(
        [PipedArgument] EntityUid entity,
        [CommandArgument] ProtoId<NpcFactionPrototype> faction
    )
    {
        return _sysManager.GetEntitySystem<NpcFactionSystem>().IsMember(entity,faction);
    }
}
