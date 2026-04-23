using Content.Shared.CCVar;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;

namespace Content.Client.Audio;

public sealed partial class ContentAudioSystem
{
    private const string AndyAnnouncementPathPrefix = "/Audio/_NF/Announcements/PocketSizedAndy/";
    private const float AndyAnnouncementMaxVolume = 0f;
    // Options sliders display whole percents. Treat <= 1% as muted so "0%" cannot leak quiet playback.
    private const float AndyAnnouncementMuteThreshold = 0.01f;

    private float _andyAnnouncementVolume;
    private bool _andyAnnouncementsEnabled = true;
    private bool _andyAnnouncementsMuted;
    private readonly Dictionary<EntityUid, float> _andyAnnouncementBaseVolumes = new();
    private bool _andyAnnouncementsInitialized;

    private void InitializeAndyAnnouncements()
    {
        if (_andyAnnouncementsInitialized)
            return;

        _andyAnnouncementsInitialized = true;
        Subs.CVar(_configManager, CCVars.AndyAnnouncementsEnabled, AndyAnnouncementEnabledChanged, true);
        Subs.CVar(_configManager, CCVars.AndyAnnouncementVolume, AndyAnnouncementVolumeChanged, true);
    }

    private void AndyAnnouncementEnabledChanged(bool enabled)
    {
        _andyAnnouncementsEnabled = enabled;

        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateAndyAnnouncementVolume(uid, component);
        }
    }

    private void AndyAnnouncementVolumeChanged(float volume)
    {
        _andyAnnouncementsMuted = volume <= AndyAnnouncementMuteThreshold;
        _andyAnnouncementVolume = SharedAudioSystem.GainToVolume(volume);

        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateAndyAnnouncementVolume(uid, component);
        }
    }

    private void UpdateAndyAnnouncementVolumes()
    {
        var query = EntityQueryEnumerator<AudioComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            UpdateAndyAnnouncementVolume(uid, component);
        }
    }

    private void UpdateAndyAnnouncementVolume(EntityUid uid, AudioComponent component)
    {
        if (!IsAndyAnnouncement(component.FileName))
            return;

        if (!_andyAnnouncementsEnabled || _andyAnnouncementsMuted)
        {
            if (component.Playing)
                _audio.Stop(uid, component);
            return;
        }

        if (!_andyAnnouncementBaseVolumes.TryGetValue(uid, out var baseVolume))
        {
            baseVolume = component.Params.Volume;
            _andyAnnouncementBaseVolumes[uid] = baseVolume;
        }

        var expected = MathF.Min(baseVolume + _andyAnnouncementVolume, AndyAnnouncementMaxVolume);

        if (MathF.Abs(component.Volume - expected) < 0.001f)
            return;

        _audio.SetVolume(uid, expected, component);
    }

    private static bool IsAndyAnnouncement(string fileName)
    {
        if (!fileName.StartsWith(AndyAnnouncementPathPrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var filenameStart = fileName.LastIndexOf('/') + 1;
        if (filenameStart <= 0 || filenameStart >= fileName.Length)
            return false;

        return fileName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase)
            && fileName[filenameStart..].StartsWith("andy", StringComparison.OrdinalIgnoreCase);
    }
}