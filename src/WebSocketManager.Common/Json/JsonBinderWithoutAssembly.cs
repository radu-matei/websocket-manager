using Newtonsoft.Json.Serialization;
using System;

namespace WebSocketManager.Common
{
    /// <summary>
    /// Finds types without looking at the assembly.
    /// </summary>
    /// <seealso cref="Newtonsoft.Json.Serialization.ISerializationBinder"/>
    public class JsonBinderWithoutAssembly : ISerializationBinder
    {
        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            typeName = serializedType.FullName;
            assemblyName = null;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            return Type.GetType(typeName);
        }
    }
}