using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Extensions
{
    public static class JsonConverterExtensions
    {
        /// <summary>
        /// Indicates if a particular object instance at some point inherits from a specific type or implements a specific interface.
        /// </summary>
        /// <param name="sourceType">The System.Type to be evaluated.</param>
        /// <param name="typeToTestFor">The System.Type to test for.</param>
        /// <returns>Returns a boolean indicating if a particular object instance at some point inherits from a specific type or implements a specific interface.</returns>
        public static bool IsOfType(this System.Type sourceType, System.Type typeToTestFor)
        {
          if (sourceType == null) throw new System.ArgumentNullException("baseType", "Cannot test if object IsOfType() with a null base type");

            if (typeToTestFor == null) throw new System.ArgumentNullException("targetType", "Cannot test if object IsOfType() with a null target type");

            if (object.ReferenceEquals(sourceType, typeToTestFor)) return true;

            if (typeToTestFor.IsInterface)
                return sourceType.GetInterfaces().Contains(typeToTestFor)
                       ? true
                       : false;

            while (sourceType != null && sourceType != typeof(object))
            {
                sourceType = sourceType.BaseType;
                if (sourceType == typeToTestFor)
                    return true;
            }

            return false;
        }

        /// <summary>Casts an object to another type.</summary>
        /// <param name="obj">The object to cast.</param>
        /// <param name="type">The end type to cast to.</param>
        /// <returns>Returns the casted object.</returns>
        public static object Cast(this object obj, Type type)
        {
            var dataParam = Expression.Parameter(obj == null ? typeof(object) : obj.GetType(), "data");
            var body = Expression.Block(Expression.Convert(dataParam, type));
            var run = Expression.Lambda(body, dataParam).Compile();
            return run.DynamicInvoke(obj);
        }

        /// <summary>Creates a late-bound dictionary.</summary>
        /// <typeparam name="T">The type of elements.</typeparam>
        /// <param name="enumeration">The enumeration.</param>
        /// <param name="keySelector">The function that produces the key.</param>
        /// <param name="keyType">The type of key.</param>
        /// <param name="valueSelector">The function that produces the value.</param>
        /// <param name="valueType">The type of value.</param>
        /// <returns>Returns the late-bound typed dictionary.</returns>
        public static IDictionary ToDictionary<T>(this IEnumerable<T> enumeration, Func<T, object> keySelector, Type keyType, Func<T, object> valueSelector, Type valueType)
        {
            if (enumeration == null) return null;

            var dictionaryClosedType = typeof(Dictionary<,>).MakeGenericType(new Type[] { keyType, valueType });
            var dictionary = (IDictionary)Activator.CreateInstance(dictionaryClosedType);

            foreach (var e in enumeration)
            {
                if (!dictionary.Contains(keySelector(e)))
                {
                    dictionary.Add(keySelector(e), valueSelector(e));
                }
            }

            return dictionary;
        }
    }
}
