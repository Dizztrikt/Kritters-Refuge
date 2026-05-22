using Content.Server.Administration.Logs;
using Content.Server._NF.Bank;
using Content.Server.Chat.Systems;
using Content.Server.Power.Components;
using Content.Server.Popups;
using Content.Shared._CS.SlotMachine;
using Content.Shared._CS.SlotMachine.BUI;
using Content.Shared._CS.SlotMachine.Events;
using Content.Shared._NF.Bank;
using Content.Shared._NF.Bank.Components;
using Content.Shared.Database;
using Content.Shared.Power;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._CS.SlotMachine;

public sealed class SlotMachineSystem : EntitySystem
{
    private static readonly SoundPathSpecifier SpinSound = new("/Audio/_CS/Objects/Slutmachine/Spin.mp3");
    private static readonly SoundPathSpecifier WinSound = new("/Audio/_CS/Objects/Slutmachine/Win.mp3");
    private static readonly SoundPathSpecifier JackpotSound = new("/Audio/_CS/Objects/Slutmachine/Jackpot.mp3");
    private static readonly SoundPathSpecifier LossSound = new("/Audio/_CS/Objects/Slutmachine/Loss.mp3");

    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BankSystem _bank = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<SlotMachineComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<SlotMachineComponent, PowerChangedEvent>(OnPowerChanged);
        SubscribeLocalEvent<SlotMachineComponent, BoundUIOpenedEvent>(OnUIOpened);
        SubscribeLocalEvent<SlotMachineComponent, SlotMachinePlayMessage>(OnPlay);
    }

    private void OnMapInit(EntityUid uid, SlotMachineComponent component, MapInitEvent args)
    {
        ApplyPowerVisualState(uid, component);
    }

    private void OnPowerChanged(EntityUid uid, SlotMachineComponent component, ref PowerChangedEvent args)
    {
        ApplyPowerVisualState(uid, component, args.Powered);
    }

    private void OnUIOpened(EntityUid uid, SlotMachineComponent component, BoundUIOpenedEvent args)
    {
        UpdateUserInterface(uid, component, args.Actor, args.UiKey, Loc.GetString("slot-machine-outcome-ready"));
    }

    private void OnPlay(EntityUid uid, SlotMachineComponent component, SlotMachinePlayMessage args)
    {
        if (args.Actor is not { Valid: true } player)
            return;

        if (component.IsSpinning)
        {
            UpdateUserInterface(uid, component, player, args.UiKey, Loc.GetString("slot-machine-outcome-busy-machine"));
            Popup(player, Loc.GetString("slot-machine-outcome-busy-machine"));
            return;
        }

        if (!TryComp<BankAccountComponent>(player, out var bank))
        {
            UpdateUserInterface(uid, component, player, args.UiKey, Loc.GetString("slot-machine-outcome-no-bank"));
            Popup(player, Loc.GetString("slot-machine-outcome-no-bank"));
            return;
        }

        if (!IsAllowedBet(args.Amount))
        {
            UpdateUserInterface(uid, component, player, args.UiKey, Loc.GetString("slot-machine-outcome-invalid-bet"));
            Popup(player, Loc.GetString("slot-machine-outcome-invalid-bet"));
            return;
        }

        if (bank.Balance < args.Amount)
        {
            UpdateUserInterface(uid, component, player, args.UiKey, Loc.GetString("slot-machine-outcome-insufficient-funds"));
            Popup(player, Loc.GetString("slot-machine-outcome-insufficient-funds"));
            return;
        }

        if (!_bank.TryBankWithdraw(player, args.Amount))
        {
            UpdateUserInterface(uid, component, player, args.UiKey, Loc.GetString("slot-machine-outcome-insufficient-funds"));
            Popup(player, Loc.GetString("slot-machine-outcome-insufficient-funds"));
            return;
        }

        component.JackpotBalance += args.Amount;
        component.IsSpinning = true;
        SetVisualState(uid, component, SlotMachineVisualState.Spin);
        _audio.PlayPvs(SpinSound, uid);
        UpdateUserInterface(uid, component, player, args.UiKey, Loc.GetString("slot-machine-outcome-busy"));

        Timer.Spawn(component.SpinDuration, () => ResolveSpin(uid, component, player, args.Amount, args.UiKey));
    }

    private void ResolveSpin(EntityUid uid, SlotMachineComponent component, EntityUid player, int amount, Enum uiKey)
    {
        if (!Exists(uid))
            return;

        component.IsSpinning = false;

        var outcome = RollOutcome(component);
        switch (outcome)
        {
            case SlotMachineOutcome.Loss:
                SetVisualState(uid, component, SlotMachineVisualState.Lose);
                _audio.PlayPvs(LossSound, uid);
                UpdateUserInterface(uid, component, player, uiKey, Loc.GetString("slot-machine-outcome-loss", ("amount", BankSystemExtensions.ToCreditString(amount))));
                Popup(player, Loc.GetString("slot-machine-outcome-loss", ("amount", BankSystemExtensions.ToCreditString(amount))));
                break;
            case SlotMachineOutcome.Double:
            {
                var payout = amount * 2;
                if (TryPayout(player, component, payout))
                {
                    SetVisualState(uid, component, SlotMachineVisualState.Win);
                    _audio.PlayPvs(WinSound, uid);
                    UpdateUserInterface(uid, component, player, uiKey, Loc.GetString("slot-machine-outcome-double", ("amount", BankSystemExtensions.ToCreditString(payout))));
                    Popup(player, Loc.GetString("slot-machine-outcome-double", ("amount", BankSystemExtensions.ToCreditString(payout))));
                    _adminLogger.Add(LogType.ATMUsage, LogImpact.Low, $"{ToPrettyString(player):actor} won {payout} on {ToPrettyString(uid)}");
                }
                else
                {
                    SetVisualState(uid, component, SlotMachineVisualState.Lose);
                    _audio.PlayPvs(LossSound, uid);
                    UpdateUserInterface(uid, component, player, uiKey, Loc.GetString("slot-machine-outcome-loss", ("amount", BankSystemExtensions.ToCreditString(amount))));
                }

                break;
            }
            case SlotMachineOutcome.Jackpot:
            {
                var payout = component.JackpotBalance;
                if (payout <= 0)
                    payout = amount;

                if (TryPayout(player, component, payout))
                {
                    component.JackpotBalance = 0;
                    SetVisualState(uid, component, SlotMachineVisualState.Win);
                    _audio.PlayPvs(JackpotSound, uid);

                    var payoutText = BankSystemExtensions.ToCreditString(payout);
                    var message = Loc.GetString("slot-machine-jackpot-announcement",
                        ("player", MetaData(player).EntityName),
                        ("amount", payoutText),
                        ("gridname", GetGridName(uid)));

                    _chat.DispatchGlobalAnnouncement(message, MetaData(uid).EntityName, true, colorOverride: Color.Gold);
                    UpdateUserInterface(uid, component, player, uiKey, Loc.GetString("slot-machine-outcome-jackpot", ("amount", payoutText)));
                    Popup(player, Loc.GetString("slot-machine-outcome-jackpot", ("amount", payoutText)));
                    _adminLogger.Add(LogType.ATMUsage, LogImpact.Medium, $"{ToPrettyString(player):actor} won a jackpot of {payout} on {ToPrettyString(uid)}");
                }
                else
                {
                    SetVisualState(uid, component, SlotMachineVisualState.Lose);
                    _audio.PlayPvs(LossSound, uid);
                    UpdateUserInterface(uid, component, player, uiKey, Loc.GetString("slot-machine-outcome-loss", ("amount", BankSystemExtensions.ToCreditString(amount))));
                }

                break;
            }
        }

        Timer.Spawn(component.ResultDuration, () =>
        {
            if (!Exists(uid))
                return;

            if (TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered)
                SetVisualState(uid, component, SlotMachineVisualState.Idle);
            else
                SetVisualState(uid, component, SlotMachineVisualState.Off);
        });

        if (TryComp<ApcPowerReceiverComponent>(uid, out var currentPower) && !currentPower.Powered)
            SetVisualState(uid, component, SlotMachineVisualState.Off);
    }

    private bool TryPayout(EntityUid player, SlotMachineComponent component, int payout)
    {
        if (payout <= 0)
            return false;

        if (component.JackpotBalance < payout)
            return false;

        if (!_bank.TryBankDeposit(player, payout))
            return false;

        component.JackpotBalance -= payout;
        return true;
    }

    private SlotMachineOutcome RollOutcome(SlotMachineComponent component)
    {
        var lossWeight = Math.Max(0, component.LossWeight);
        var doubleWeight = Math.Max(0, component.DoubleWeight);
        var jackpotWeight = Math.Max(0, component.JackpotWeight);

        var total = lossWeight + doubleWeight + jackpotWeight;
        if (total <= 0)
            return SlotMachineOutcome.Loss;

        var roll = _random.NextFloat() * total;
        if (roll < lossWeight)
            return SlotMachineOutcome.Loss;

        roll -= lossWeight;
        if (roll < doubleWeight)
            return SlotMachineOutcome.Double;

        return SlotMachineOutcome.Jackpot;
    }

    private bool IsAllowedBet(int amount)
    {
        return amount is 500 or 1000 or 5000 or 10000;
    }

    private void ApplyPowerVisualState(EntityUid uid, SlotMachineComponent component, bool? powered = null)
    {
        var isPowered = powered ?? (TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered);

        if (!isPowered)
        {
            SetVisualState(uid, component, SlotMachineVisualState.Off);
            return;
        }

        if (component.CurrentState == SlotMachineVisualState.Off)
            SetVisualState(uid, component, SlotMachineVisualState.Idle);
        else
            SetVisualState(uid, component, component.CurrentState);
    }

    private void SetVisualState(EntityUid uid, SlotMachineComponent component, SlotMachineVisualState state)
    {
        component.CurrentState = state;
        _appearance.SetData(uid, SlotMachineVisuals.State, state);
    }

    private void UpdateUserInterface(EntityUid uid, SlotMachineComponent component, EntityUid player, Enum uiKey, string outcomeText)
    {
        var enabled = TryComp<ApcPowerReceiverComponent>(uid, out var power) && power.Powered;
        var balance = TryComp<BankAccountComponent>(player, out var playerBank) ? playerBank.Balance : 0;

        _ui.SetUiState(uid, uiKey, new SlotMachineMenuInterfaceState(balance, component.JackpotBalance, enabled, component.IsSpinning, outcomeText));
    }

    private void Popup(EntityUid player, string text)
    {
        _popup.PopupEntity(text, player, player, PopupType.Medium);
    }

    private string GetGridName(EntityUid uid)
    {
        if (TryComp<TransformComponent>(uid, out var transform) && transform.GridUid is { Valid: true } gridUid)
            return MetaData(gridUid).EntityName;

        return MetaData(uid).EntityName;
    }

    private enum SlotMachineOutcome
    {
        Loss,
        Double,
        Jackpot,
    }
}