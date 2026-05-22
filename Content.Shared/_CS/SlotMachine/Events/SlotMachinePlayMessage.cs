using Robust.Shared.Serialization;

namespace Content.Shared._CS.SlotMachine.Events;

[Serializable, NetSerializable]
public sealed class SlotMachinePlayMessage : BoundUserInterfaceMessage
{
    public int Amount;

    public SlotMachinePlayMessage(int amount)
    {
        Amount = amount;
    }
}