using Content.Server.Speech;
using Content.Server.Chat.Systems;

using System.Numerics;

using Content.Shared._Kritters.Serde;
namespace Content.Server._Kritters.Serde;



public sealed class SerdeActorSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmill = default!;

    // Always subscribe to events here, on initialize
    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _logManager.GetSawmill("crSerde.logs");

        SubscribeLocalEvent<SerdeActorComponent, ComponentInit>(OnActorInit);
        SubscribeLocalEvent<SerdeActorComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeActorComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<SerdeActorComponent, MoveEvent>(OnMove);
    }

    private void OnMove(Entity<SerdeActorComponent> ent, ref MoveEvent moved)
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

    private void OnActorInit(Entity<SerdeActorComponent> ent, ref ComponentInit _)
    {
        RaiseLocalEvent(ent, new SerdeOutEvent(0, "gainedCapability", "Actor", 0, 0, 0, 0));
        if(EntityManager.TryGetComponent<TransformComponent>(ent, out var transform))
        {
            var position = transform.LocalPosition;
            var uid = transform.ParentUid;
            RaiseLocalEvent(ent, new SerdeOutEvent(0, "position", "", uid.Id, 0, position.X, position.Y));
            ent.Comp.LastPos = position;
            ent.Comp.LastGrid = uid.Id;
        }
    }

    private void OnListen(Entity<SerdeActorComponent> ent, ref ListenEvent args)
    {
        if (!ent.Comp.CanListen) return;

        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        var message = args.Message.Trim();
        var source = args.Source;

        RaiseLocalEvent(ent, new SerdeOutEvent(0, "heard", message, (int) source, 0, 0f, 0f));
    }

    private void OnSerdeIn(Entity<SerdeActorComponent> ent, ref SerdeInEvent sev)
    {
        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        if (sev.Command == "say")
        {
            if (!ent.Comp.CanSpeak) return;
            _chat.TrySendInGameICMessage(ent.Owner, sev.Text, InGameICChatType.Speak, ChatTransmitRange.Normal, false);
            return;
        }

    }
}
