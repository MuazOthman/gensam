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
                ApplyPrefix();
            }
        }

        private void ApplyPrefix()
        {
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

        public bool IsEmpty
        {
            get
            {
                switch (Type)
                {
                    case "String": return string.IsNullOrEmpty(StringValue);
                    case "Map": return !MapValue.Any();
                    case "List": return !ListValue.Any();
                }
                return true;
            }
        }

        public void Add(YamlValue item)
        {
            if (Type != "List")
                throw new YamlValueException($"Cannot add an item to YamlValue with type: {Type}");
            listValue.Add(item);
            ApplyPrefix();
        }

        public void Add(string key, YamlValue value)
        {
            if (Type != "Map")
                throw new YamlValueException($"Cannot add a key-value pair to YamlValue with type: {Type}");
            mapValue.Add(key, value);
            ApplyPrefix();
        }

        private string prefix;
        private readonly List<YamlValue> listValue;
        private readonly Dictionary<string, YamlValue> mapValue;
        public IEnumerable<YamlValue> ListValue => listValue;
        public IReadOnlyDictionary<string, YamlValue> MapValue => mapValue;

        public YamlValue(string stringValue)
        {
            Type = "String";
            StringValue = stringValue;
            this.listValue = null;
            this.mapValue = null;
        }

        public YamlValue(List<YamlValue> listValue)
        {
            this.listValue = listValue;
            Type = "List";
            StringValue = null;
            this.mapValue = null;
        }

        public YamlValue(IEnumerable<string> listValue)
        {
            this.listValue = listValue.Select(s => new YamlValue(s)).ToList();
            Type = "List";
            StringValue = null;
            this.mapValue = null;
        }

        public YamlValue(Dictionary<string, YamlValue> mapValue)
        {
            this.mapValue = mapValue;
            this.listValue = null;
            Type = "Map";
            StringValue = null;
        }

        public YamlValue(Dictionary<string, string> mapValue)
        {
            this.mapValue = mapValue.ToDictionary(p => p.Key, p => new YamlValue(p.Value));
            this.listValue = null;
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
