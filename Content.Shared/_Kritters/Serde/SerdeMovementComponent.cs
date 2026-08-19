namespace Content.Shared._Kritters.Serde;

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates; // Required for AutoGenerateComponentState
using Robust.Shared.Serialization;

using System.Numerics;

[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeMovementComponent : Component
{
    [DataField]
    public Vector2 LastPos = new Vector2(0f, 0f);

    [DataField]
    public int LastGrid = EntityUid.Invalid.Id;
}
