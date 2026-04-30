using System.Diagnostics.CodeAnalysis;
using Content.Shared.Construction.Components;
using Content.Shared.Construction.Prototypes;
using Content.Shared.Examine;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Construction.Steps
{
    [DataDefinition]
    public sealed partial class MachinePartConstructionGraphStep : EntityInsertConstructionGraphStep
    {
        [DataField("machinePart", required: true)]
        public ProtoId<MachinePartPrototype> MachinePartPrototypeId { get; private set; }

        [DataField] public int Amount { get; private set; } = 1;

        [DataField] public LocId Name { get; private set; } = string.Empty;

        [DataField] public SpriteSpecifier? Icon { get; private set; }

        public override bool EntityValid(EntityUid uid, IEntityManager entityManager, IComponentFactory compFactory)
        {
            if (!entityManager.TryGetComponent(uid, out MachinePartComponent? machinePart) ||
                machinePart.PartType != MachinePartPrototypeId)
            {
                return false;
            }

            if (Amount <= 1)
                return true;

            return entityManager.TryGetComponent(uid, out StackComponent? stack) && stack.Count >= Amount;
        }

        public bool EntityValid(EntityUid uid,
            IEntityManager entityManager,
            [NotNullWhen(true)] out MachinePartComponent? machinePart,
            out StackComponent? stack)
        {
            stack = null;

            if (!entityManager.TryGetComponent(uid, out machinePart) ||
                machinePart.PartType != MachinePartPrototypeId)
            {
                machinePart = null;
                return false;
            }

            if (Amount <= 1)
                return true;

            if (!entityManager.TryGetComponent(uid, out stack) || stack.Count < Amount)
            {
                machinePart = null;
                stack = null;
                return false;
            }

            return true;
        }

        public override void DoExamine(ExaminedEvent examinedEvent)
        {
            var stepName = GetStepName();
            examinedEvent.PushMarkup(Loc.GetString("construction-insert-material-entity", ("amount", Amount), ("materialName", stepName)));
        }

        public override ConstructionGuideEntry GenerateGuideEntry()
        {
            var stepName = GetStepName();

            return new ConstructionGuideEntry
            {
                Localization = "construction-presenter-material-step",
                Arguments = new (string, object)[] { ("amount", Amount), ("material", stepName) },
                Icon = Icon,
            };
        }

        private string GetStepName()
        {
            if (!string.IsNullOrEmpty(Name))
                return Loc.GetString(Name);

            var protoManager = IoCManager.Resolve<IPrototypeManager>();
            var part = protoManager.Index(MachinePartPrototypeId);
            return Loc.GetString(part.Name);
        }
    }
}