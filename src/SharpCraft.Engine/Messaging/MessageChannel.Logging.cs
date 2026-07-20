using Microsoft.Extensions.Logging;

namespace SharpCraft.Engine.Messaging;

public partial class MessageChannel
{
    [LoggerMessage(LogLevel.Error, "Handler on channel {channel} threw while processing {messageType}")]
    partial void LogHandlerFailed(string channel, Type messageType, Exception ex);
}
