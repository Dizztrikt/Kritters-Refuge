using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     Allows enabling/disabling amqp client
    /// </summary>
    public static readonly CVarDef<bool> AMQPEnabled =
        CVarDef.Create("amqp.enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     The URI rabbitMQ should connect to
    ///     format amqp://user:pass@hostName:port/vhost
    ///     see: https://www.rabbitmq.com/client-libraries/dotnet-api-guide#dotnet-versions
    /// </summary>
    public static readonly CVarDef<string> AMQPURI =
        CVarDef.Create("amqp.uri", "amqp://guest:guest@localhost:5672/", CVar.SERVERONLY);
}
