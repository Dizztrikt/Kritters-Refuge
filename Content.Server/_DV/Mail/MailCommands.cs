using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Content.Shared.Administration;
using Content.Server.Administration;
using Content.Server._DV.Mail.Components;
using Content.Server._DV.Mail.EntitySystems;
using Content.Server._NF.SectorServices;

using Content.Server._NF.Mail.Components;
using Robust.Shared.Toolshed;

namespace Content.Server._DV.Mail;

// Kritters: replaced with toolshead commands
[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed partial class MailCommand : ToolshedCommand
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IEntitySystemManager _entitySystemManager = default!;

    private SharedTransformSystem? _xform;

    private const string BlankMailPrototype = "MailAdminFun";
    private const string BlankLargeMailPrototype = "MailLargeAdminFun"; // Frontier: large mail
    private const string Container = "storagebase";
    private const string MailContainer = "contents";


    [CommandImplementation("contentsTo")]
    public void MailContentsTo(
        [CommandInvocationContext] IInvocationContext shell,
        [PipedArgument] EntityUid containerUid,
        [CommandArgument] EntityUid recipientUid,
        [CommandArgument] bool isFragile = false,
        [CommandArgument] bool isPriority = false,
        [CommandArgument] bool isLarge = false
    )
    {
        var mailPrototype = isLarge ? BlankLargeMailPrototype : BlankMailPrototype;

        var mailSystem = _entitySystemManager.GetEntitySystem<MailSystem>();
        var containerSystem = _entitySystemManager.GetEntitySystem<SharedContainerSystem>();
        var sectorService = _entitySystemManager.GetEntitySystem<SectorServiceSystem>(); // Frontier

        // Frontier: sector-wide mail
        if (!_entityManager.TryGetComponent(sectorService.GetServiceEntity(), out SectorMailComponent? sectorMail))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailservice"));
            return;
        }
        // End Frontier

        if (!_entityManager.HasComponent<MailReceiverComponent>(recipientUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailreceiver", ("requiredComponent", nameof(MailReceiverComponent))));
            return;
        }

        if (!_prototypeManager.HasIndex<EntityPrototype>(mailPrototype)) // Frontier: _blankMailPrototype<mailPrototype
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-blankmail", ("blankMail", mailPrototype))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        // Frontier: box optional
        // if (!containerSystem.TryGetContainer(containerUid, Container, out var targetContainer))
        // {
        //     shell.WriteLine(Loc.GetString("command-mailto-invalid-container", ("requiredContainer", Container)));
        //     return;
        // }
        // End Frontier

        if (!mailSystem.TryGetMailRecipientForReceiver(recipientUid, out var recipient))
        {
            shell.WriteLine(Loc.GetString("command-mailto-unable-to-receive"));
            return;
        }

        if (!mailSystem.TryGetMailTeleporterForReceiver(recipientUid, out var teleporterComponent, out var teleporterUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-teleporter-found"));
            return;
        }

        var mailUid = _entityManager.SpawnEntity(mailPrototype, _entityManager.GetComponent<TransformComponent>(containerUid).Coordinates); // Frontier: _blankMailPrototype<mailPrototype
        var mailContents = containerSystem.EnsureContainer<Container>(mailUid, MailContainer);

        if (!_entityManager.TryGetComponent<MailComponent>(mailUid, out var mailComponent))
        {
            shell.WriteLine(Loc.GetString("command-mailto-bogus-mail", ("blankMail", mailPrototype), ("requiredMailComponent", nameof(MailComponent)))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        // Frontier: box optional
        if (containerSystem.TryGetContainer(containerUid, Container, out var targetContainer))
        {
            foreach (var entity in targetContainer.ContainedEntities.ToArray())
            {
                containerSystem.Insert(entity, mailContents);
            }
        }
        else
        {
            containerSystem.Insert(containerUid, mailContents);
        }
        // End Frontier

        mailComponent.IsFragile = isFragile;
        mailComponent.IsPriority = isPriority;
        mailComponent.IsLarge = isLarge;

        mailSystem.SetupMail(mailUid, sectorMail, recipient.Value); // Frontier: use SectorMailComponent

        var teleporterQueue = containerSystem.EnsureContainer<Container>((EntityUid)teleporterUid, "queued");
        containerSystem.Insert(mailUid, teleporterQueue);
        shell.WriteLine(Loc.GetString("command-mailto-success", ("timeToTeleport", sectorMail.TeleportInterval.TotalSeconds - sectorMail.Accumulator))); // Frontier: use SectorMailComponent
    }

    [CommandImplementation("to")]
    public void MailTo(
        [CommandInvocationContext] IInvocationContext shell,
        [PipedArgument] IEnumerable<EntityUid> contentsUids,
        [CommandArgument] EntityUid recipientUid,
        [CommandArgument] bool isFragile = false,
        [CommandArgument] bool isPriority = false,
        [CommandArgument] bool isLarge = false
    )
    {

        _xform ??= GetSys<SharedTransformSystem>();

        var mailPrototype = isLarge ? BlankLargeMailPrototype : BlankMailPrototype;

        var mailSystem = _entitySystemManager.GetEntitySystem<MailSystem>();
        var containerSystem = _entitySystemManager.GetEntitySystem<SharedContainerSystem>();
        var sectorService = _entitySystemManager.GetEntitySystem<SectorServiceSystem>(); // Frontier


        // Frontier: sector-wide mail
        if (!_entityManager.TryGetComponent(sectorService.GetServiceEntity(), out SectorMailComponent? sectorMail))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailservice"));
            return;
        }
        // End Frontier

        if (!_entityManager.HasComponent<MailReceiverComponent>(recipientUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-mailreceiver", ("requiredComponent", nameof(MailReceiverComponent))));
            return;
        }

        if (!_prototypeManager.HasIndex<EntityPrototype>(mailPrototype)) // Frontier: _blankMailPrototype<mailPrototype
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-blankmail", ("blankMail", mailPrototype))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        if (!mailSystem.TryGetMailRecipientForReceiver(recipientUid, out var recipient))
        {
            shell.WriteLine(Loc.GetString("command-mailto-unable-to-receive"));
            return;
        }

        if (!mailSystem.TryGetMailTeleporterForReceiver(recipientUid, out var teleporterComponent, out var teleporterUid))
        {
            shell.WriteLine(Loc.GetString("command-mailto-no-teleporter-found"));
            return;
        }

        // Kritters: the "box" is now an iterator
        using var contentsIterator = contentsUids.GetEnumerator();

        if (!contentsIterator.MoveNext())
        {
            shell.WriteLine(Loc.GetString("command-mailto-cannot-mail-nothing"));
            return;
        }

        var mailSpawnCoords = _entityManager.GetComponent<TransformComponent>(contentsIterator.Current).Coordinates;

        var mailUid = _entityManager.SpawnEntity(mailPrototype, mailSpawnCoords); // Frontier: _blankMailPrototype<mailPrototype
        var mailContents = containerSystem.EnsureContainer<Container>(mailUid, MailContainer);

        if (!_entityManager.TryGetComponent<MailComponent>(mailUid, out var mailComponent))
        {
            shell.WriteLine(Loc.GetString("command-mailto-bogus-mail", ("blankMail", mailPrototype), ("requiredMailComponent", nameof(MailComponent)))); // Frontier: _blankMailPrototype<mailPrototype
            return;
        }

        containerSystem.Insert(contentsIterator.Current, mailContents);

        while (contentsIterator.MoveNext())
        {
            var contentUid = contentsIterator.Current;
            containerSystem.Insert(contentUid, mailContents);
        }
        // End Kritters

        mailComponent.IsFragile = isFragile;
        mailComponent.IsPriority = isPriority;
        mailComponent.IsLarge = isLarge;

        mailSystem.SetupMail(mailUid, sectorMail, recipient.Value); // Frontier: use SectorMailComponent

        var teleporterQueue = containerSystem.EnsureContainer<Container>((EntityUid)teleporterUid, "queued");
        containerSystem.Insert(mailUid, teleporterQueue);
        shell.WriteLine(Loc.GetString("command-mailto-success", ("timeToTeleport", sectorMail.TeleportInterval.TotalSeconds - sectorMail.Accumulator))); // Frontier: use SectorMailComponent
    }

    [CommandImplementation("now")]
    public void MailNow(
        [CommandInvocationContext] IInvocationContext shell
    )
    {
        var sectorService = _entitySystemManager.GetEntitySystem<SectorServiceSystem>();
        // Frontier: sector-wide mail
        if(_entityManager.TryGetComponent<SectorMailComponent>(sectorService.GetServiceEntity(), out var mail))
        {
            mail.Accumulator = (float)mail.TeleportInterval.TotalSeconds;
        }
        // End Frontier: sector-wide mail

        shell.WriteLine(Loc.GetString("command-mailnow-success"));
    }

}
