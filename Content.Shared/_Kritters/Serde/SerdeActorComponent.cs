namespace Content.Shared._Kritters.Serde;

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates; // Required for AutoGenerateComponentState
using Robust.Shared.Serialization;

using System.Numerics;

// <summary>
// Component to debug the event serde system
// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeActorComponent : Component
{
    [DataField]
    public bool CanSpeak = true;

    [DataField]
    public bool CanListen = true;

    [DataField]
    public Vector2 LastPos = new Vector2(0f, 0f);

    [DataField]
    public int LastGrid = EntityUid.Invalid.Id;
}
