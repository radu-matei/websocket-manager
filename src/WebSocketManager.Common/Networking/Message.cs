namespace WebSocketManager.Common
{
    public enum MessageType
    {
        Text,
        MethodInvocation,
        ConnectionEvent,
        MethodReturnValue,
        TextRaw,
    }

    public class Message
    {
        public MessageType MessageType { get; set; }
        public string Data { get; set; }
    }
}