namespace Content.Shared._Kritters.Serde;

using Robust.Shared.GameObjects;
using Robust.Shared.GameStates; // Required for AutoGenerateComponentState
using Robust.Shared.Serialization;

using System.Numerics;

[RegisterComponent, NetworkedComponent]
public sealed partial class SerdeNpcComponent : Component
{
    [DataField]
    public Vector2 Memories = new Vector2(0f, 0f);
}
