using Robust.Shared.Toolshed;
using Content.Server.NPC.HTN;

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

using Content.Shared.CombatMode;
using Robust.Shared.Toolshed.Syntax;

namespace Content.Server.NPC.Commands;

// Kritters: replaced `npcadd` with a toolshead command `npc:setBehavor`
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class NPCCommand : ToolshedCommand
{

    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntitySystemManager _sysManager = default!;

    [CommandImplementation("setBehavor")]
    public void SetBehavor(
        [PipedArgument] EntityUid entity,
        [CommandArgument] ProtoId<HTNCompoundPrototype> behavor
    )
    {
        var htnComponent = _entities.EnsureComponent<HTNComponent>(entity);


        htnComponent.RootTask = new HTNCompoundTask()
        {
            Task = behavor.ToString()
        };
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
        if (_entities.TryGetComponent<CombatModeComponent>(entity, out var combatComponent)) {
            return combatComponent.IsInCombatMode;
        }
        return false;
    }
}
