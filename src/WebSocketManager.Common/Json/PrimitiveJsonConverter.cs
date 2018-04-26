using Newtonsoft.Json;
using System;
using System.Runtime.Serialization.Formatters;

namespace WebSocketManager.Common
{
    /// <summary>
    /// https://stackoverflow.com/questions/25007001/json-net-does-not-preserve-primitive-type-information-in-lists-or-dictionaries-o?utm_medium=organic&utm_source=google_rich_qa&utm_campaign=google_rich_qa
    /// Shoutouts to Sacrilege for his awesome primitive json converter.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.JsonConverter"/>
    public sealed class PrimitiveJsonConverter : JsonConverter
    {
        public PrimitiveJsonConverter()
        {
        }

        public override bool CanRead
        {
            get
            {
                return false;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.IsPrimitive || objectType == typeof(Guid) || objectType == typeof(string);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            switch (serializer.TypeNameHandling)
            {
                case TypeNameHandling.All:
                    writer.WriteStartObject();
                    writer.WritePropertyName("$type", false);

                    switch (serializer.TypeNameAssemblyFormatHandling)
                    {
                        case TypeNameAssemblyFormatHandling.Full:
                            writer.WriteValue(value.GetType().AssemblyQualifiedName);
                            break;

                        default:
                            writer.WriteValue(value.GetType().FullName);
                            break;
                    }

                    writer.WritePropertyName("$value", false);
                    writer.WriteValue(value);
                    writer.WriteEndObject();
                    break;

                default:
                    writer.WriteValue(value);
                    break;
            }
        }
    }
}