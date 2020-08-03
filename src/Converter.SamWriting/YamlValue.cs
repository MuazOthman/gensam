using DotLiquid;
using System.Collections.Generic;
using System.Linq;

namespace Converter.SamWriting
{
    public class YamlValue : Drop
    {
        public string Type { get; }
        public string StringValue { get; }
        public string Prefix
        {
            get => prefix;
            set
            {
                prefix = value;
                if (ListValue != null)
                    foreach (var item in ListValue)
                    {
                        item.Prefix = Prefix + "  - ";
                    }
                if (MapValue != null)
                    foreach (var item in MapValue)
                    {
                        if (item.Value.Type == "List")
                            item.Value.Prefix = Prefix;
                        else
                            item.Value.Prefix = "  " + Prefix;
                    }
            }
        }

        private string prefix;


        public YamlValue(string stringValue)
        {
            Type = "String";
            StringValue = stringValue;
            ListValue = null;
            MapValue = null;
        }

        public List<YamlValue> ListValue { get; }

        public YamlValue(List<YamlValue> listValue)
        {
            ListValue = listValue;
            Type = "List";
            StringValue = null;
            MapValue = null;
        }

        public YamlValue(IEnumerable<string> listValue)
        {
            ListValue = listValue.Select(s => new YamlValue(s)).ToList();
            Type = "List";
            StringValue = null;
            MapValue = null;
        }

        public Dictionary<string, YamlValue> MapValue { get; }

        public YamlValue(Dictionary<string, YamlValue> mapValue)
        {
            MapValue = mapValue;
            ListValue = null;
            Type = "Map";
            StringValue = null;
        }

        public YamlValue(Dictionary<string, string> mapValue)
        {
            MapValue = mapValue.ToDictionary(p => p.Key, p => new YamlValue(p.Value));
            ListValue = null;
            Type = "Map";
            StringValue = null;
        }

        public static implicit operator YamlValue(string value)
        {
            return new YamlValue(value);
        }

        public static implicit operator YamlValue(List<string> listValue)
        {
            return new YamlValue(listValue.AsEnumerable());
        }

        public static implicit operator YamlValue(string[] listValue)
        {
            return new YamlValue(listValue);
        }

        public static implicit operator YamlValue(Dictionary<string, YamlValue> mapValue)
        {
            return new YamlValue(mapValue);
        }

        public static implicit operator YamlValue(Dictionary<string, string> mapValue)
        {
            return new YamlValue(mapValue);
        }
    }
}
