using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Codeplex.Data
{
    public class DynamicJson : DynamicObject
    {
        private enum JsonType
        {
            @string, number, boolean, @object, array, @null
        }

        #region Reader

        private static dynamic ToValue(XElement element)
        {
            var type = (JsonType)Enum.Parse(typeof(JsonType), element.Attribute("type").Value);
            switch (type)
            {
                case JsonType.boolean:
                    return (bool)element;
                case JsonType.number:
                    return (double)element;
                case JsonType.@string:
                    return (string)element;
                case JsonType.@object:
                    return new DynamicJson(element);
                case JsonType.array:
                    return element.Elements().Select(x => ToValue(x)).ToArray();
                case JsonType.@null:
                default:
                    return null;
            }
        }

        /// <summary>from JsonSring to DynamicJson</summary>
        public static dynamic Parse(string json)
        {
            using (var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.Unicode.GetBytes(json), XmlDictionaryReaderQuotas.Max))
            {
                return ToValue(XElement.Load(jsonReader));
            }
        }

        // represents JavaScript Object

        readonly XElement xml;
        string jsonCache;

        public DynamicJson(XElement element)
        {
            xml = element;
        }

        public bool IsDefined(string name)
        {
            return (xml.Element(name) != null);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            var element = xml.Element(binder.Name);
            if (element == null)
            {
                result = null;
                return false;
            }

            result = ToValue(element);
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return xml.Elements().Select(x => x.Name.LocalName);
        }

        public override string ToString()
        {
            if (jsonCache != null) return jsonCache;

            // <foo type="null"></foo> is can't serialize. replace to <foo type="null" />
            foreach (var elem in xml.Descendants().Where(x => x.Attribute("type").Value == "null"))
            {
                elem.RemoveNodes();
            }
            using (var ms = new MemoryStream())
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode))
            {
                new XStreamingElement("root", new XAttribute("type", "object"), xml.Elements()).WriteTo(writer);
                writer.Flush();
                jsonCache = Encoding.Unicode.GetString(ms.ToArray());
                return jsonCache;
            }
        }

        #endregion

        #region Writer

        private static JsonType GetJsonType(object obj)
        {
            if (obj == null) return JsonType.@null;

            switch (Type.GetTypeCode(obj.GetType()))
            {
                case TypeCode.Boolean:
                    return JsonType.boolean;
                case TypeCode.String:
                case TypeCode.Char:
                case TypeCode.DateTime:
                    return JsonType.@string;
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    return JsonType.number;
                case TypeCode.Object:
                    return (obj is IEnumerable) ? JsonType.array : JsonType.@object;
                case TypeCode.DBNull:
                case TypeCode.Empty:
                default:
                    return JsonType.@null;
            }
        }

        private static XAttribute CreateTypeAttr(JsonType type)
        {
            return new XAttribute("type", type.ToString());
        }

        private static object CreateJsonNode(object obj)
        {
            var type = GetJsonType(obj);
            switch (type)
            {
                case JsonType.@string:
                case JsonType.number:
                    return obj;
                case JsonType.boolean:
                    return obj.ToString().ToLower();
                case JsonType.@object:
                    return CreateXObject(obj);
                case JsonType.array:
                    return CreateXArray(obj as IEnumerable);
                case JsonType.@null:
                default:
                    return null;
            }
        }

        private static IEnumerable<XStreamingElement> CreateXArray<T>(T obj) where T : IEnumerable
        {
            return obj.Cast<object>()
                .Select(o => new XStreamingElement("item", CreateTypeAttr(GetJsonType(o)), CreateJsonNode(o)));
        }

        private static IEnumerable<XStreamingElement> CreateXObject(object obj)
        {
            return obj.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(pi => new { Name = pi.Name, Value = pi.GetValue(obj, null) })
                .Select(a => new XStreamingElement(a.Name, CreateTypeAttr(GetJsonType(a.Value)), CreateJsonNode(a.Value)));
        }

        /// <summary>return JsonSring from primitive or IEnumerable or Object(Key:public property name, Value:property value)</summary>
        public static string Save(object obj)
        {
            var element = new XStreamingElement("root", CreateTypeAttr(GetJsonType(obj)), CreateJsonNode(obj));
            using (var ms = new MemoryStream())
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode))
            {
                element.WriteTo(writer);
                writer.Flush();
                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }

        #endregion
    }
}