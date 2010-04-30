/*--------------------------------------------------------------------------
* DynamicJson
* ver 1.0.0.0 (Apr. 30th, 2010)
*
* created and maintained by neuecc <ils@neue.cc>
* licensed under Microsoft Public License(Ms-PL)
* http://neue.cc/
* http://dynamicjson.codeplex.com/
*--------------------------------------------------------------------------*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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

        // public static methods

        /// <summary>from JsonSring to DynamicJson</summary>
        public static dynamic Parse(string json)
        {
            using (var jsonReader = JsonReaderWriterFactory.CreateJsonReader(Encoding.Unicode.GetBytes(json), XmlDictionaryReaderQuotas.Max))
            {
                return ToValue(XElement.Load(jsonReader));
            }
        }

        /// <summary>create JsonSring from primitive or IEnumerable or Object({public property name:property value})</summary>
        public static string Serialize(object obj)
        {
            return CreateJsonString(new XStreamingElement("root", CreateTypeAttr(GetJsonType(obj)), CreateJsonNode(obj)));
        }

        // private static methods

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
                    return new DynamicJson(element, type);
                case JsonType.array:
                    return new DynamicJson(element, type);
                case JsonType.@null:
                default:
                    return null;
            }
        }

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

        private static string CreateJsonString(XStreamingElement element)
        {
            using (var ms = new MemoryStream())
            using (var writer = JsonReaderWriterFactory.CreateJsonWriter(ms, Encoding.Unicode))
            {
                element.WriteTo(writer);
                writer.Flush();
                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }

        // dynamic structure represents JavaScript Object/Array

        readonly XElement xml;
        readonly JsonType jsonType;

        /// <summary>create blank JSObject</summary>
        public DynamicJson()
        {
            xml = new XElement("root", CreateTypeAttr(JsonType.@object));
            jsonType = JsonType.@object;
        }

        private DynamicJson(XElement element, JsonType type)
        {
            Debug.Assert(type != JsonType.@object || type != JsonType.array);

            xml = element;
            jsonType = type;
        }

        public bool IsObject { get { return jsonType == JsonType.@object; } }

        public bool IsArray { get { return jsonType == JsonType.array; } }

        public int Length { get { return GetDynamicMemberNames().Count(); } }

        /// <summary>has property or not</summary>
        public bool IsDefined(string name)
        {
            return IsObject && (xml.Element(name) != null);
        }

        /// <summary>delete property</summary>
        public void Delete(string name)
        {
            var elem = xml.Element(name);
            if (elem != null) elem.Remove();
        }

        private object DeserializeObject(Type targetType)
        {
            var result = Activator.CreateInstance(targetType);
            var dict = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .ToDictionary(pi => pi.Name, pi => pi);
            foreach (var item in xml.Elements())
            {
                PropertyInfo propertyInfo;
                if (!dict.TryGetValue(item.Name.LocalName, out propertyInfo)) continue;
                var value = ToValue(item);
                if (value is DynamicJson)
                {
                    var json = ((DynamicJson)value);
                    value = (json.IsArray)
                        ? json.DeserializeArray(propertyInfo.PropertyType)
                        : json.DeserializeObject(propertyInfo.PropertyType);
                }
                propertyInfo.SetValue(result, Convert.ChangeType(value, propertyInfo.PropertyType), null);
            }
            return result;
        }

        private object DeserializeArray(Type targetType)
        {
            
            if (targetType.IsArray)
            {
                var elemType = targetType.GetElementType();
                dynamic array = Array.CreateInstance(elemType, xml.Elements().Count());
                var index = 0;
                foreach (var item in xml.Elements()) array[index++] = Convert.ChangeType(ToValue(item),elemType);
                return array;
            }
            else
            {
                var elemType = targetType.GetGenericArguments()[0];
                dynamic list = Activator.CreateInstance(targetType);
                foreach (var item in xml.Elements()) list.Add(Convert.ChangeType(ToValue(item), elemType));
                return list;
            }
        }

        /// <summary>mapping by Public PropertyName</summary>
        public T Deserialize<T>() where T : new()
        {
            if (IsArray) throw new InvalidOperationException();

            return (T)DeserializeObject(typeof(T));
        }

        private bool TryGet(XElement element, out object result)
        {
            if (element == null)
            {
                result = null;
                return false;
            }

            result = ToValue(element);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            return (IsArray)
                ? TryGet(xml.Elements().ElementAtOrDefault((int)indexes[0]), out result)
                : TryGet(xml.Element((string)indexes[0]), out result);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            return (IsArray)
                ? TryGet(xml.Elements().ElementAtOrDefault(int.Parse(binder.Name)), out result)
                : TryGet(xml.Element(binder.Name), out result);
        }

        private bool TrySet(string name, object value)
        {
            var type = GetJsonType(value);
            var element = xml.Element(name);
            if (element == null)
            {
                xml.Add(new XElement(name, CreateTypeAttr(type), CreateJsonNode(value)));
            }
            else
            {
                element.Attribute("type").Value = type.ToString();
                element.ReplaceNodes(CreateJsonNode(value));
            }

            return true;
        }

        private bool TrySet(int index, object value)
        {
            var type = GetJsonType(value);
            var e = xml.Elements().ElementAtOrDefault(index);
            if (e == null)
            {
                xml.Add(new XElement("item", CreateTypeAttr(type), CreateJsonNode(value)));
            }
            else
            {
                e.Attribute("type").Value = type.ToString();
                e.ReplaceNodes(CreateJsonNode(value));
            }

            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            return (IsArray)
                ? TrySet((int)indexes[0], value)
                : TrySet((string)indexes[0], value);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            return (IsArray)
                ? TrySet(int.Parse(binder.Name), value)
                : TrySet(binder.Name, value);
        }

        public override bool TryConvert(ConvertBinder binder, out object result)
        {
            if (!IsArray || binder.Type != typeof(object[]))
            {
                result = null;
                return false;
            }

            result = xml.Elements().Select(x => ToValue(x)).Cast<object>().ToArray();
            return true;
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return (jsonType == JsonType.@object)
                ? xml.Elements().Select(x => x.Name.LocalName)
                : xml.Elements().Select((x, i) => i.ToString());
        }

        /// <summary>Serialize to JsonString</summary>
        public override string ToString()
        {
            // <foo type="null"></foo> is can't serialize. replace to <foo type="null" />
            foreach (var elem in xml.Descendants().Where(x => x.Attribute("type").Value == "null"))
            {
                elem.RemoveNodes();
            }
            return CreateJsonString(new XStreamingElement("root", CreateTypeAttr(jsonType), xml.Elements()));
        }
    }
}