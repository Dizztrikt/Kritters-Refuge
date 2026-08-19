using Robust.Shared.Prototypes;

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

using Content.Shared._Kritters.Serde;
using System.Threading.Tasks;
using Content.Server.NPC.HTN;
namespace Content.Server._Kritters.Serde;

public sealed record NotPrimitiveMarker;

public sealed class SerdeHTNSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private static readonly ProtoId<HTNCompoundPrototype> IdleTask = "IdleCompound";
    //private static readonly ProtoId<HTNCompoundPrototype> BasicTask = "SerdeBasicActorCompound";
    //private static readonly JsonCerializationOptions _options = default!;


    public override void Initialize()
    {
        base.Initialize();

        // _options = new JsonSerializerOptions { WriteIndented = false };

        // You can use a verbatim string literal (@"") to paste standard YAML directly
        //string taskName = "DynamicTaskPrimary";
        //string myYamlString = $"- type: htnCompoundTask\n  id: {taskName}\n  branches:\n";

        // Pass the string to the manager. It parses it just like an on-disk .yml file.
        //_prototypeManager.LoadString(myYamlString);

        SubscribeLocalEvent<SerdeNpcComponent, ComponentInit>(OnActorInit);
        SubscribeLocalEvent<SerdeNpcComponent, SerdeInEvent>(OnSerdeIn);
    }

    private void OnActorInit(Entity<SerdeNpcComponent> ent, ref ComponentInit _)
    {
        RaiseLocalEvent(ent, new SerdeOutEvent(0, "gainedCapability", "HTN", 0, 0, 0, 0));
        EnsureComp<HTNComponent>(ent.Owner, out var htn);
        htn.Enabled = true;
        htn.RootTask = new HTNCompoundTask { Task = IdleTask };
    }

    private void OnSerdeIn(Entity<SerdeNpcComponent> ent, ref SerdeInEvent sev)
    {
        // if it's okay to be accepting commands
        EntityManager.EnsureComponent<SerdeComponent>(ent, out var serdeComponent);
        if (!serdeComponent.AcceptingCommands) return;

        if (sev.Command == "getBlackboard")
        {
            //WIP
            EnsureComp<HTNComponent>(ent.Owner, out var htn);
            var value = htn.Blackboard.GetValue<object>(sev.Text);
            var jsonString = JsonSerializer.Serialize(value); //, _options);
            RaiseLocalEvent(ent, new SerdeOutEvent(sev.ExecutionID, "return", jsonString, 0, 0, 0, 0));
            return;
        }

        if (sev.Command == "setBlackboard")
        {
            EnsureComp<HTNComponent>(ent.Owner, out var htn);
            using JsonDocument doc = JsonDocument.Parse(sev.Text);
            JsonElement root = doc.RootElement;

            if (root.ValueKind != JsonValueKind.Object) {
                RaiseLocalEvent(ent, new SerdeOutEvent(sev.ExecutionID, "nak", "Root element must be a JSON object.", 0, 0, 0, 0));
                return;
            }

            foreach (JsonProperty property in root.EnumerateObject())
            {
                JsonElement element = property.Value;


                bool wasInvalid = false;
                object? nativeValue = element.ValueKind switch
                {
                    JsonValueKind.String => element.GetString(),

                    JsonValueKind.Number => element.TryGetInt32(out int intVal)
                        ? intVal
                        : element.GetDouble(),

                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,

                    // Fallthrough fallback / Strict assertion
                    _ => new NotPrimitiveMarker(),
                };

                if (nativeValue is NotPrimitiveMarker)
                {
                    RaiseLocalEvent(ent, new SerdeOutEvent(sev.ExecutionID, "error", $"value was not a primitive: {property.Name}", 0, 0, 0, 0));
                }
                else if (nativeValue is null)
                {
                    htn.Blackboard.Remove<object>(property.Name);
                }
                else
                {
                    htn.Blackboard.SetValue(property.Name,nativeValue);
                }
            }

            RaiseLocalEvent(ent, new SerdeOutEvent(sev.ExecutionID, "ack", "", 0, 0, 0, 0));
            return;
        }

        if (sev.Command == "dumpBlackboard")
        {
            EnsureComp<HTNComponent>(ent.Owner, out var htn);

            var dictionary = new Dictionary<string, object>();

            foreach (var kvp in htn.Blackboard)
            {
                dictionary[kvp.Key] = kvp.Value;
            }

            var jsonString = JsonSerializer.Serialize(dictionary); //, _options);

            RaiseLocalEvent(ent, new SerdeOutEvent(sev.ExecutionID, "blackboardContents", jsonString, 0, 0, 0, 0));
            return;
        }
    }


}
