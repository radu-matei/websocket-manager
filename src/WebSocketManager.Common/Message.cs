namespace WebSocketManager.Common
{
    public enum MessageType
    {
        Text,
        ClientMethodInvocation,
        ConnectionEvent,
        ClientMethodReturnValue
    }

    public class Message
    {
        public MessageType MessageType { get; set; }
        public string Data { get; set; }
    }
}