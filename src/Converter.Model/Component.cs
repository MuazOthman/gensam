using System;
using System.Collections.Generic;
using System.Linq;

namespace Converter.Model
{
    public class Component
    {
        private readonly Dictionary<ComponentType, ComponentTypeHandler> _componentTypeHandlers =
            new Dictionary<ComponentType, ComponentTypeHandler>
            {
                {
                    ComponentType.RestEndpoint,
                    (name, props) =>
                    {
                        var splitIndex = name.IndexOf(' ');
                        if (splitIndex < 0)
                            throw new ArgumentException(
                                $"Invalid RestEndpoint name '{name}': an empty space is expected between HTTP Method and endpoint",
                                nameof(name));
                        var httpMethod = name.Substring(0, splitIndex).ToUpper();
                        if (!new[] {"GET", "POST", "PUT", "DELETE", "PATCH"}.Contains(httpMethod))
                            throw new ArgumentException(
                                $"Invalid RestEndpoint name '{name}': invalid or unsupported HTTP Method '{httpMethod}'",
                                nameof(name));
                        var endpoint = name.Substring(splitIndex + 1);
                        if (!endpoint.StartsWith("/"))
                            throw new ArgumentException(
                                $"Invalid RestEndpoint name '{name}': endpoint '{endpoint}' must start with '/'",
                                nameof(name));
                        props.Add("HttpMethod", httpMethod);
                        props.Add("Endpoint", endpoint);
                        return props;
                    }
                },
                {
                    ComponentType.Function,
                    (name, props) =>
                    {
                        if (props.ContainsKey("Runtime"))
                        {
                            if (!new[] {"default", "nodejs12.x", "dotnetcore2.1", "dotnetcore3.1"}.Contains(
                                props["Runtime"]))
                                throw new ArgumentException(
                                    $"Invalid or unsupported function runtime '{props["Runtime"]}'", nameof(props));
                        }
                        else
                        {
                            props.Add("Runtime", "default");
                        }

                        return props;
                    }
                }
            };

        private readonly List<Connection> _inboundConnections = new List<Connection>();
        private readonly List<Connection> _outboundConnections = new List<Connection>();
        private readonly Dictionary<string, string> _properties;

        public Component(string name, ComponentType type, Dictionary<string, string> properties = null)
        {
            Name = name;
            _properties = properties ?? new Dictionary<string, string>();
            Type = type;
            if (_componentTypeHandlers.ContainsKey(type)) _properties = _componentTypeHandlers[type](Name, _properties);
        }

        public IReadOnlyDictionary<string, string> Properties => _properties;

        public string Name { get; }
        public ComponentType Type { get; }
        public IReadOnlyList<Connection> InboundConnections => _inboundConnections.AsReadOnly();

        public IReadOnlyList<Connection> OutboundConnections => _outboundConnections.AsReadOnly();

        internal void Merge(Component another)
        {
            // check for conflicts in type
            if (Type != another.Type)
                throw new Exception(
                    $"Failed merging component: name '{another.Name}' is shared by more than one resource of different types");
            // check for conflicts in properties
            foreach (var k in _properties.Keys)
                if (another._properties.ContainsKey(k) && another._properties[k] != _properties[k])
                    throw new Exception(
                        $"Failed merging conflicted property values '{_properties[k]}' and '{another._properties[k]}' for property '{k}'");
            // merge properties
            foreach (var k in another._properties.Keys)
                if (!_properties.ContainsKey(k))
                    _properties.Add(k, another._properties[k]);
            // merge connections
            foreach (var connection in another.InboundConnections) AddInboundConnection(connection);
            foreach (var connection in another.OutboundConnections) AddOutboundConnection(connection);
        }

        internal void AddOutboundConnection(Connection connection)
        {
            var connectionExists =
                _outboundConnections.Any(
                    c => c.Target.Name == connection.Target.Name
                         && c.Label == connection.Label
                );
            if (!connectionExists)
                _outboundConnections.Add(connection);
        }

        internal void AddInboundConnection(Connection connection)
        {
            var connectionExists =
                _inboundConnections.Any(
                    c => c.Source.Name == connection.Source.Name
                         && c.Label == connection.Label
                );
            if (!connectionExists)
                _inboundConnections.Add(connection);
        }
    }

    internal delegate Dictionary<string, string> ComponentTypeHandler(string name,
        Dictionary<string, string> properties);
}