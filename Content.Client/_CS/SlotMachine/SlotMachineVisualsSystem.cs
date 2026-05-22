using Content.Shared._CS.SlotMachine;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Client._CS.SlotMachine;

public sealed class SlotMachineVisualsSystem : VisualizerSystem<SlotMachineComponent>
{
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void OnAppearanceChange(EntityUid uid, SlotMachineComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!args.AppearanceData.TryGetValue(SlotMachineVisuals.State, out var visualStateObject) ||
            visualStateObject is not SlotMachineVisualState visualState)
        {
            visualState = SlotMachineVisualState.Idle;
        }

        SetBodyLayerState(component.OffState, (uid, args.Sprite));

        switch (visualState)
        {
            case SlotMachineVisualState.Off:
                HideLayer(SlotMachineVisualLayers.Screen, (uid, args.Sprite));
                break;
            case SlotMachineVisualState.Idle:
                SetScreenLayerState(component.IdleState, (uid, args.Sprite));
                break;
            case SlotMachineVisualState.Spin:
                SetScreenLayerState(component.SpinState, (uid, args.Sprite));
                break;
            case SlotMachineVisualState.Win:
                SetScreenLayerState(component.WinState, (uid, args.Sprite));
                break;
            case SlotMachineVisualState.Lose:
                SetScreenLayerState(component.LoseState, (uid, args.Sprite));
                break;
        }
    }

    private void SetBodyLayerState(string? state, Entity<SpriteComponent> sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), SlotMachineVisualLayers.Body, true);
        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), SlotMachineVisualLayers.Body, false);
        _sprite.LayerSetRsiState(sprite.AsNullable(), SlotMachineVisualLayers.Body, state);
    }

    private void SetScreenLayerState(string? state, Entity<SpriteComponent> sprite)
    {
        if (string.IsNullOrEmpty(state))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), SlotMachineVisualLayers.Screen, true);
        _sprite.LayerSetAutoAnimated(sprite.AsNullable(), SlotMachineVisualLayers.Screen, true);
        _sprite.LayerSetRsiState(sprite.AsNullable(), SlotMachineVisualLayers.Screen, state);
    }

    private void HideLayer(SlotMachineVisualLayers layer, Entity<SpriteComponent> sprite)
    {
        if (!_sprite.LayerMapTryGet(sprite.AsNullable(), layer, out var actualLayer, false))
            return;

        _sprite.LayerSetVisible(sprite.AsNullable(), actualLayer, false);
    }

    private enum SlotMachineVisualLayers : byte
    {
        Body = 0,
        Screen = 1,
    }
}