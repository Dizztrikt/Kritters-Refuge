using Content.Server.Speech;
using Content.Server.Chat.Systems;

using System.Numerics;

using Content.Shared._Kritters.Serde;
namespace Content.Server._Kritters.Serde;



public sealed class SerdeSpeechSystem : EntitySystem
{

    [Dependency] private readonly ChatSystem _chat = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SerdeSpeechComponent, ComponentInit>(OnActorInit);
        SubscribeLocalEvent<SerdeSpeechComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeSpeechComponent, ListenEvent>(OnListen);
    }

    private void OnActorInit(Entity<SerdeSpeechComponent> ent, ref ComponentInit _)
    {
        RaiseLocalEvent(ent, new SerdeOutEvent(0, "gainedCapability", "Speech", 0, 0, 0, 0));
    }


    private void OnListen(Entity<SerdeSpeechComponent> ent, ref ListenEvent args)
    {
        if (!ent.Comp.CanListen) return;

        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        var message = args.Message.Trim();
        var source = args.Source;

        // TODO: strip the source '>' marker out
        RaiseLocalEvent(ent, new SerdeOutEvent(0, "heard", message, (int)source, 0, 0f, 0f));
    }

    private void OnSerdeIn(Entity<SerdeSpeechComponent> ent, ref SerdeInEvent sev)
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
