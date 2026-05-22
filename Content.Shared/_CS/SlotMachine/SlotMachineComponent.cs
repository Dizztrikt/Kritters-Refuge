using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CS.SlotMachine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SlotMachineComponent : Component
{
    [DataField, AutoNetworkedField]
    public string OffState = "Video-Lotto-OFF";

    [DataField, AutoNetworkedField]
    public string IdleState = "LottoIDLE";

    [DataField, AutoNetworkedField]
    public string SpinState = "LottoSPIN";

    [DataField, AutoNetworkedField]
    public string WinState = "LottoWIN";

    [DataField, AutoNetworkedField]
    public string LoseState = "LottoLOSE";

    [DataField]
    public int JackpotBalance = 250000;

    [DataField]
    public TimeSpan SpinDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan ResultDuration = TimeSpan.FromSeconds(1.5);

    [DataField]
    public int LossWeight = 80;

    [DataField]
    public int DoubleWeight = 18;

    [DataField]
    public int JackpotWeight = 2;

    [ViewVariables(VVAccess.ReadWrite)]
    public SlotMachineVisualState CurrentState = SlotMachineVisualState.Off;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool IsSpinning;
}

[NetSerializable, Serializable]
public enum SlotMachineUiKey : byte
{
    Key
}

[NetSerializable, Serializable]
public enum SlotMachineVisualState : byte
{
    Off,
    Idle,
    Spin,
    Win,
    Lose,
}

[NetSerializable, Serializable]
public enum SlotMachineVisuals : byte
{
    State,
}