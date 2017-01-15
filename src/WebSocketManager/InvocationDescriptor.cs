using Newtonsoft.Json;

namespace WebSocketManager
{
    public class InvocationDescriptor
    {
        [JsonProperty("methodName")]
        public string MethodName { get; set; }

        [JsonPropertyAttribute("arguments")]
        public object[] Arguments { get; set; }
    }
}