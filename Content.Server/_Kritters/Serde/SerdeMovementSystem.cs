using Content.Server.Speech;
using Content.Server.Chat.Systems;

using System.Numerics;

using Content.Shared._Kritters.Serde;
namespace Content.Server._Kritters.Serde;



public sealed class SerdeMovementSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SerdeMovementComponent, ComponentInit>(OnActorInit);
        SubscribeLocalEvent<SerdeMovementComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeMovementComponent, MoveEvent>(OnMove);
    }

    private void OnMove(Entity<SerdeMovementComponent> ent, ref MoveEvent moved)
    {
        float updateDistanceSquared = 0.5f * 0.5f;

        var pos = moved.NewPosition.Position;
        var lastPos = ent.Comp.LastPos;

        var newGrid = moved.NewPosition.EntityId;
        var diffrentGrid = newGrid.Id != ent.Comp.LastGrid;
        if (diffrentGrid || Vector2.DistanceSquared(lastPos, pos) > updateDistanceSquared) {
            RaiseLocalEvent(ent, new SerdeOutEvent(0, "position", "", newGrid.Id, 0, pos.X, pos.Y));
            ent.Comp.LastPos = pos;
            ent.Comp.LastGrid = newGrid.Id;
        }
    }

    private void OnSerdeIn(Entity<SerdeMovementComponent> ent, ref SerdeInEvent sev)
    {
        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        if (sev.Command == "moveTo")
        {
            //WIP
        }
    }

    private void OnActorInit(Entity<SerdeMovementComponent> ent, ref ComponentInit _)
    {
        RaiseLocalEvent(ent, new SerdeOutEvent(0, "gainedCapability", "Movement", 0, 0, 0, 0));
        if(EntityManager.TryGetComponent<TransformComponent>(ent, out var transform))
        {
            var position = transform.LocalPosition;
            var uid = transform.ParentUid;
            RaiseLocalEvent(ent, new SerdeOutEvent(0, "position", "", uid.Id, 0, position.X, position.Y));
            ent.Comp.LastPos = position;
            ent.Comp.LastGrid = uid.Id;
        }
    }
}
