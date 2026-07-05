
using System.Linq;
using Content.Shared.Mind.Components;
using Robust.Shared.Serialization;

using Content.Shared.CCVar;
using Robust.Shared.Configuration;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

using System.Text;
using System.Threading.Tasks;

using System.Collections.Concurrent;
using System.Threading.Channels;

using Robust.Shared.GameObjects;

using Content.Shared._Kritters.Serde;
namespace Content.Server._Kritters.Serde;

// A system that exchanges game events amonst itself
public sealed class SerdeSystem : EntitySystem
{

    [Dependency] private readonly ILogManager _logManager = default!;
    private ISawmill _sawmillSerde = default!;
    private ISawmill _sawmillAMQP = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private readonly ConcurrentQueue<(EntityUid, SerdeInEvent)> _inQueue = new();
    private readonly Channel<(EntityUid, SerdeOutEvent)> _outQueue =
        Channel.CreateUnbounded<(EntityUid, SerdeOutEvent)>();

    private float _throttleBuildup = 0f;
    private float _pollingRate = 0.5f;
    private int _myServerID = 12; //TODO: not hardcode

    private IChannel? _amqpChannel = null;
    private IConnection? _amqpConnection = null;

    private bool AMQPReady = false;

    private async void OpenServer(){

        var factory = new ConnectionFactory { };

        var uriString = _cfg.GetCVar(CCVars.AMQPURI);
        _sawmillAMQP.Info("Connecting to broker with URI: " + uriString);
        factory.Uri = new Uri(uriString);

        // TODO: make dynamic

        try
        {
            // Try to establish a connection to your activated broker
            _amqpConnection = await factory.CreateConnectionAsync();
            _sawmillAMQP.Debug("Successfully connected to broker");

            _amqpChannel = await _amqpConnection.CreateChannelAsync();


            string inQueue = $"ss14.{_myServerID}.in";
            string outQueue = $"ss14.{_myServerID}.out";
            const string inRouter = "amq.topic";
            const string outRouter = "amq.topic";

            await _amqpChannel.QueueDeclareAsync(
                queue: inQueue,
                durable: false,
                exclusive: true,
                autoDelete: true,
                arguments: new Dictionary<string, object?> { }
            );

            await _amqpChannel.QueueDeclareAsync(
                queue: outQueue,
                durable: false,
                exclusive: false,
                autoDelete: true,
                arguments: new Dictionary<string, object?> { }
            );

            await _amqpChannel.QueueBindAsync(inQueue, inRouter, $"ss14.{_myServerID}.object.in", null);
            await _amqpChannel.QueueBindAsync(inQueue, inRouter, $"ss14.{_myServerID}.admin.in", null);
            await _amqpChannel.QueueBindAsync(inQueue, inRouter, $"ss14.all.admin.in", null);

            // Modern v7+ Consumer Setup
            var consumer = new AsyncEventingBasicConsumer(_amqpChannel);

            consumer.ReceivedAsync += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);

                    //_sawmillAMQP.Error($"Received {message} from {ea.RoutingKey}");

                    // Format `entId:int:command:int:float:float:rest is text`
                    var messageParts = message.Split(":",7);

                    if (messageParts.Length != 8) {
                        throw new ArgumentOutOfRangeException(nameof(messageParts), "Too few arguments");
                    }

                    EntityUid entity = new EntityUid(int.Parse(messageParts[0]));

                    var inEvent = new SerdeInEvent(
                        int.Parse(messageParts[1]), // ExecutionID
                        messageParts[2], // Command
                        messageParts[7], // Text
                        int.Parse(messageParts[3]), // A
                        int.Parse(messageParts[4]), // B
                        float.Parse(messageParts[5]), // X
                        float.Parse(messageParts[6]) // Y
                    );

                    _inQueue.Enqueue((
                        entity,
                        inEvent
                    ));
                }
                catch (Exception ex)
                {
                    _sawmillAMQP.Error($"incoming message was invalid: {ex.Message}");
                }

                // If your inner processing isn't natively awaited, you must explicitly return a Task:
                await Task.CompletedTask;
            };

            _sawmillAMQP.Info("Subscribed to topics");

            await _amqpChannel.BasicConsumeAsync(queue: inQueue, autoAck: true, consumer: consumer);

            byte[] upMessageBytes = Encoding.UTF8.GetBytes("up");
            await _amqpChannel.BasicPublishAsync(
                exchange: outRouter, // Or outRouter, depending on direction
                routingKey: $"ss14.{_myServerID}.status",
                body: upMessageBytes
            );

            _sawmillAMQP.Info("sent server ready signal");
            AMQPReady = true;

            await foreach (var theEvent in _outQueue.Reader.ReadAllAsync())
            {
                var (entUid, outEvent) = theEvent;
                string Command = outEvent.Command.Replace(":", "_");
                var msg = $"{entUid}:{outEvent.ExecutionID}:{Command}:{outEvent.A}:{outEvent.B}:{outEvent.X}:{outEvent.Y}:{outEvent.Text}";
                byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
                await _amqpChannel.BasicPublishAsync(
                    exchange: outRouter, // Or outRouter, depending on direction
                    routingKey: $"ss14.{_myServerID}.object.out",
                    body: messageBytes
                );
            }
        }
        catch (Exception ex)
        {
            _sawmillAMQP.Error($"Connection failed: {ex.Message}");
            _sawmillAMQP.Error("Make sure your RabbitMQ server is active and running on localhost.");
        }
    }

    // Always subscribe to events here, on initialize
    public override void Initialize()
    {
        base.Initialize();


        // "my_system.debug" is the category name for the logs
        _sawmillSerde = _logManager.GetSawmill("cr_serde.main");
        _sawmillAMQP = _logManager.GetSawmill("cr_serde.amqp");

        // try to start rabbitMQ
        if (_cfg.GetCVar(CCVars.AMQPEnabled))
        {
            _sawmillAMQP.Info("AMQP (RabbitMQ) is enabled");
            Task.Run(async () =>
            {
                OpenServer();
            });
        }
        else
        {
            _sawmillAMQP.Info("AMQP (RabbitMQ) is disabled");
        }

        // Log a debug message
        //_sawmill.Debug("System successfully initialized and ready for testing.");

        // Log a warning message
        //_sawmill.Warning("A minor issue was detected, proceeding anyway.");

        // Subscribe to FooComponent being initialized...
        SubscribeLocalEvent<SerdeComponent, ComponentInit>(OnSerdeInit);
        SubscribeLocalEvent<SerdeComponent, SerdeInEvent>(OnSerdeIn);
        SubscribeLocalEvent<SerdeComponent, SerdeOutEvent>(OnSerdeOut);

        SubscribeLocalEvent<SerdeComponent, MindAddedMessage>(OnMindAdded);
        SubscribeLocalEvent<SerdeComponent, MindRemovedMessage>(OnMindRemoved);

        // Subscribe to FooComponent being interacted on by an user with an item.
        //SubscribeLocalEvent<SerdeComponent, InteractUsingEvent>(Handle);

        // Subscribe to the MoveEvent broadcast event, raised whenever
        // an entity moves... Just an example subscription
        // SubscribeLocalEvent<MoveEvent>(OnEntityMove);
    }

    public override void Update(float frameTime)
    {
        if (AMQPReady){
            _throttleBuildup += frameTime;
            if (_throttleBuildup > _pollingRate) {
                _throttleBuildup -= _pollingRate;

                while (_inQueue.TryDequeue(out var data))
                {
                    //TODO test what happens on non serde or nonexistant objects,
                    // it could go horrably qrong
                    var (entUID, inEvent) = data;
                    if (TryComp<SerdeComponent>(entUID, out var component)) {
                        RaiseLocalEvent(entUID, inEvent);
                    }
                }
            }
        }
    }

    private void OnMindAdded(Entity<SerdeComponent> ent, ref MindAddedMessage _){
        if (ent.Comp.DisableOnTakeover)
        {
            ent.Comp.AcceptingCommands = false;
            RaiseLocalEvent(ent, new SerdeOutEvent(0, "paused", "takeover", 0, 0, 0, 0));
        }
    }

    private void OnMindRemoved(Entity<SerdeComponent> ent, ref MindRemovedMessage _){
        if (ent.Comp.EnableOnRelease)
        {
            ent.Comp.AcceptingCommands = true;
            RaiseLocalEvent(ent, new SerdeOutEvent(0, "resumed", "takeover", 0, 0, 0, 0));
        }
    }

    // This is called when a FooComponent is initialized.
    private void OnSerdeInit(Entity<SerdeComponent> ent, ref ComponentInit _)
    {
         // Initialize your FooComponent here
    }

    private void OnSerdeIn(Entity<SerdeComponent> ent, ref SerdeInEvent serdeEvent)
    {
        if (ent.Comp.DebugLogging)
        {
            _sawmillSerde.Info($"Serde in: {serdeEvent.Command} '{serdeEvent.Text}' {serdeEvent.A} {serdeEvent.B} {serdeEvent.X} {serdeEvent.Y}");
        }
    }

    private void OnSerdeOut(Entity<SerdeComponent> ent, ref SerdeOutEvent serdeEvent)
    {
        if (ent.Comp.DebugLogging)
        {
            // I am not c# skilled enough to compress this line
            _sawmillSerde.Info($"Serde out:  {serdeEvent.Command} '{serdeEvent.Text}' {serdeEvent.A} {serdeEvent.B} {serdeEvent.X} {serdeEvent.Y}");
        }

        if (AMQPReady)
        {
            _sawmillAMQP.Debug("sent to queue");
            // TODO dropped packet detection
            var ok = _outQueue.Writer.TryWrite((ent.Owner, serdeEvent));
        }
    }

    public void CommandRaiseIn(
        Entity<SerdeComponent?> ent,
        //SerdeInEvent inEvent,
        int id,
        string command, string text,
        int a, int b, float x, float y
    )
    {
        RaiseLocalEvent(ent, new SerdeInEvent(id, command, text, a, b, x, y));
    }

    public void CommandRaiseOut(
        Entity<SerdeComponent?> ent,
        //SerdeInEvent inEvent,
        int id,
        string command, string text,
        int a, int b, float x, float y
    )
    {
        RaiseLocalEvent(ent, new SerdeOutEvent(id, command, text, a, b, x, y));
    }
}

public sealed class SerdeInEvent : EntityEventArgs
{
    // command
    public int ExecutionID { get; }
    public string Command { get; }
    public string Text { get; }
    public int A { get; }
    public int B { get; }
    public float X { get; }
    public float Y { get; }

    public SerdeInEvent(int id, string command, string text, int a, int b, float x, float y)
    {
        ExecutionID = id;
        Command = command;
        Text = text;
        A = a;
        B = b;
        X = x;
        Y = y;
    }
}

public sealed class SerdeOutEvent : EntityEventArgs
{

    public int ExecutionID { get; }
    public string Command { get; }
    public string Text { get; }
    public int A { get; }
    public int B { get; }
    public float X { get; }
    public float Y { get; }


    public SerdeOutEvent(int id, string command, string text, int a, int b, float x, float y)
    {
        ExecutionID = id;
        Command = command;
        Text = text;
        A = a;
        B = b;
        X = x;
        Y = y;
    }
}
