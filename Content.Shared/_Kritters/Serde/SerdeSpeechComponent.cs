namespace Content.Shared._Kritters.Serde;

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates; // Required for AutoGenerateComponentState
using Robust.Shared.Serialization;

using System.Numerics;

[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeSpeechComponent : Component
{
    [DataField]
    public bool CanListen = true;

    [DataField]
    public bool CanSpeak = true;
}
