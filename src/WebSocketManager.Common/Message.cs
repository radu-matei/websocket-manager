using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Text;

namespace WebSocketManager.Common
{
  public enum MessageType
  {
    Text,
    ClientMethodInvocation,
    ConnectionEvent
  }

  public class Message
  {
    private static JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings()
    {
      ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public MessageType MessageType { get; set; }
    public string Data { get; set; }

    private ArraySegment<byte>? serialized = null;

    [JsonIgnore]
    public ArraySegment<byte> Serialized
    {
      get
      {
        lock (this)
        {
          if (!serialized.HasValue)
          {
            var serializedMessage = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(this, _jsonSerializerSettings));
            serialized = new ArraySegment<byte>(serializedMessage, 0, serializedMessage.Length);
          }
        }
        return serialized.Value;
      }
    }

  }
}