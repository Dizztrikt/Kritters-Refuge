using Robust.Shared.Serialization;

namespace Content.Shared._CS.SlotMachine.BUI;

[NetSerializable, Serializable]
public sealed class SlotMachineMenuInterfaceState : BoundUserInterfaceState
{
    public int Balance;
    public int JackpotBalance;
    public bool Enabled;
    public bool Playing;
    public string OutcomeText;

    public SlotMachineMenuInterfaceState(int balance, int jackpotBalance, bool enabled, bool playing, string outcomeText)
    {
        Balance = balance;
        JackpotBalance = jackpotBalance;
        Enabled = enabled;
        Playing = playing;
        OutcomeText = outcomeText;
    }
}