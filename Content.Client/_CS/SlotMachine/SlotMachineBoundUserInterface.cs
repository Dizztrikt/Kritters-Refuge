using Content.Client._CS.SlotMachine.UI;
using Content.Shared._CS.SlotMachine.BUI;
using Content.Shared._CS.SlotMachine.Events;
using Robust.Client.UserInterface;

namespace Content.Client._CS.SlotMachine;

public sealed class SlotMachineBoundUserInterface : BoundUserInterface
{
    private SlotMachineMenu? _menu;

    public SlotMachineBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey) {}

    protected override void Open()
    {
        base.Open();

        if (_menu == null)
        {
            _menu = this.CreateWindow<SlotMachineMenu>();
            _menu.PlayRequest += OnPlay;
        }
    }

    private void OnPlay(int amount)
    {
        SendMessage(new SlotMachinePlayMessage(amount));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not SlotMachineMenuInterfaceState slotState)
            return;

        _menu?.SetEnabled(slotState.Enabled);
        _menu?.SetPlaying(slotState.Playing);
        _menu?.SetBalance(slotState.Balance);
        _menu?.SetJackpot(slotState.JackpotBalance);
        _menu?.SetOutcome(slotState.OutcomeText);
    }
}