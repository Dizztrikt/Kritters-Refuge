using Robust.Shared.Prototypes;
using Content.Shared.Administration;
using Content.Server.Administration;
using Content.Shared.SubFloor;
using Robust.Shared.Map.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._Kritters;

// Kritters: replaced with toolshed commands
[ToolshedCommand, AdminCommand(AdminFlags.Admin)]
public sealed partial class OnFloorCommand : ToolshedCommand
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;

    private SharedTransformSystem? _xform;

    private const string BlankMailPrototype = "MailAdminFun";
    private const string BlankLargeMailPrototype = "MailLargeAdminFun"; // Frontier: large mail
    private const string Container = "storagebase";
    private const string MailContainer = "contents";

    [CommandImplementation]
    public IEnumerable<EntityUid> OnFloorIter(
        [CommandInvocationContext] IInvocationContext shell,
        [CommandInverted] bool inverted,
        [PipedArgument] IEnumerable<EntityUid> input,
        [CommandArgument] bool includeAnchored = false,
        [CommandArgument] bool includeUnanchored = true,
        [CommandArgument] bool includeSubfloor = false
    )
    {
        foreach (var entityUid in input)
        {
            if (OnFloorBool(shell,
                    inverted,
                    entityUid,
                    includeAnchored,
                    includeUnanchored,
                    includeSubfloor
                    ))
            {
                yield return entityUid;
            }
        }
    }

    [CommandImplementation]
    public bool OnFloorBool(
        [CommandInvocationContext] IInvocationContext shell,
        [CommandInverted] bool inverted,
        [PipedArgument] EntityUid input,
        [CommandArgument] bool includeAnchored = false,
        [CommandArgument] bool includeUnanchored = true,
        [CommandArgument] bool includeSubfloor = false
    )
    {
        if (Deleted(input) || !_entityManager.TryGetComponent<TransformComponent>(input, out var transform))
            return inverted;

        if (transform.Anchored ? !includeAnchored : !includeUnanchored)
            return inverted;

        if (_entityManager.HasComponent<SubFloorHideComponent>(input))
            return inverted;

        if (!_entityManager.HasComponent<MapGridComponent>(transform.ParentUid))
            return inverted;

        return !inverted;
    }
}
