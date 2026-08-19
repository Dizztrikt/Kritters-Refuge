using Content.Shared.Shuttles.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Shuttles;

[TestFixture]
public sealed class RadarConsolePrototypeTest
{
    [TestCase("HandHeldMassScanner")]
    [TestCase("HandHeldMassScannerBorg")]
    [TestCase("PersonalAI")]
    public async Task PortableRadarHasBasicIffRange(string prototypeId)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var prototypes = server.ResolveDependency<IPrototypeManager>();
        var components = server.ResolveDependency<IComponentFactory>();

        await server.WaitAssertion(() =>
        {
            var prototype = prototypes.Index<EntityPrototype>(prototypeId);
            Assert.That(prototype.TryGetComponent<RadarConsoleComponent>(out var radar, components), Is.True);
            Assert.That(radar.MaxIffRange, Is.EqualTo(512f));
        });

        await pair.CleanReturnAsync();
    }
}
