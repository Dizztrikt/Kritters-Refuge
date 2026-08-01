

using Content.Shared.Chemistry.Components;
using Content.Shared.DragDrop;

namespace Content.Shared.Fluids.EntitySystems;

public sealed class SolutionDumpingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DrainableSolutionComponent, DragDropDraggedEvent>(OnDrainableDragged);
    }

    private void OnDrainableDragged(Entity<DrainableSolutionComponent> sourceContainer, ref DragDropDraggedEvent args)
    {
        var ev = new DrainedTargetEvent(args.User, sourceContainer, sourceContainer.Comp.Solution);
        RaiseLocalEvent(args.Target, ref ev);
    }

}

[ByRefEvent]
public record struct DrainedTargetEvent(EntityUid User, EntityUid Source, string SourceSelection)
{
    public readonly EntityUid User = User;
    public readonly EntityUid Source = Source;
    public readonly string SourceSelection = SourceSelection;
    public bool Handled = false;
}
