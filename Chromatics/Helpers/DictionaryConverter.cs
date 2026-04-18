using Chromatics.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    /// <summary>Deserializes dictionaries.</summary>
    public class DictionaryConverter : JsonConverter
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, Tuple<Type, Type>> resolvedTypes = new System.Collections.Concurrent.ConcurrentDictionary<Type, Tuple<Type, Type>>();

        /// <summary>If this converter is able to handle a given conversion.</summary>
        /// <param name="objectType">The type to be handled.</param>
        /// <returns>Returns if this converter is able to handle a given conversion.</returns>
        public override bool CanConvert(Type objectType)
        {
            if (resolvedTypes.ContainsKey(objectType)) return true;

            var result = typeof(IDictionary).IsAssignableFrom(objectType) || objectType.IsOfType(typeof(IDictionary));

            if (result) //check key is string or enum because it comes from Jvascript object which forces the key to be a string
            {
                if (objectType.IsGenericType && objectType.GetGenericArguments()[0] != typeof(string) && !objectType.GetGenericArguments()[0].IsEnum)
                    result = false;
            }

            return result;
        }

        /// <summary>Converts from serialized to object.</summary>
        /// <param name="reader">The reader.</param>
        /// <param name="objectType">The destination type.</param>
        /// <param name="existingValue">The existing value.</param>
        /// <param name="serializer">The serializer.</param>
        /// <returns>Returns the deserialized instance as per the actual target type.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Type keyType = null;
            Type valueType = null;

            if (resolvedTypes.ContainsKey(objectType))
            {
                keyType = resolvedTypes[objectType].Item1;
                valueType = resolvedTypes[objectType].Item2;
            }
            else
            {
                //dictionary type
                var dictionaryTypes = objectType.GetInterfaces()
                                                .Where(z => z == typeof(IDictionary) || z == typeof(IDictionary<,>))
                                                .ToList();

                if (objectType.IsInterface)
                    dictionaryTypes.Add(objectType);
                else
                    dictionaryTypes.Insert(0, objectType);

                var dictionaryType = dictionaryTypes.Count == 1
                                     ? dictionaryTypes[0]
                                     : dictionaryTypes.Where(z => z.IsGenericTypeDefinition)
                                                      .FirstOrDefault();

                if (dictionaryType == null) dictionaryTypes.First();

                keyType = !dictionaryType.IsGenericType
                              ? typeof(object)
                              : dictionaryType.GetGenericArguments()[0];

                valueType = !dictionaryType.IsGenericType
                                ? typeof(object)
                                : dictionaryType.GetGenericArguments()[1];

                resolvedTypes[objectType] = new Tuple<Type, Type>(keyType, valueType);
            }

            // Load JObject from stream
            var jObject = JObject.Load(reader);

            return jObject.Children()
                          .OfType<JProperty>()
                          .Select(z => new { Key = z.Name, Value = serializer.Deserialize(z.Value.CreateReader(), valueType) })
                          .Select(z => new
                           {
                               Key = keyType.IsEnum
                                     ? System.Enum.Parse(keyType, z.Key)
                                     : z.Key,

                               Value = z.Value.Cast(valueType)
                           })
                          .ToDictionary(z => z.Key, keyType, w => w.Value, valueType);        
        }

        /// <summary>Serializes an object with default settings.</summary>
        /// <param name="writer">The writer.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="serializer">The serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }
}
