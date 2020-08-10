using System.Collections.Generic;

namespace Converter.Model
{
    public class Application
    {
        private readonly Dictionary<string, string> _aliases = new Dictionary<string, string>();
        private readonly Dictionary<string, Component> _components = new Dictionary<string, Component>();

        private readonly List<PendingConnection> _pendingConnections = new List<PendingConnection>();

        public IEnumerable<Component> Components => _components.Values;

        public bool IsCompiled { get; private set; }

        public void RegisterComponent(string alias, Component component)
        {
            if (IsCompiled)
                throw new ModelException("Application is already compiled");
            if (_components.ContainsKey(component.Name))
            {
                var existingComponent = _components[component.Name];
                existingComponent.Merge(component);
            }
            else
            {
                _components.Add(component.Name, component);
            }

            _aliases.Add(alias, component.Name);
        }

        public Component GetComponentByName(string name)
        {
            return _components.ContainsKey(name) ? _components[name] : null;
        }

        public Component GetComponentByAlias(string alias)
        {
            return _aliases.ContainsKey(alias) ? GetComponentByName(_aliases[alias]) : null;
        }

        public void RegisterConnection(string sourceAlias, string targetAlias, string label = "")
        {
            if (IsCompiled)
                throw new ModelException("Application is already compiled");
            _pendingConnections.Add(new PendingConnection(sourceAlias, targetAlias, label));
        }

        public void Compile()
        {
            if (IsCompiled)
                throw new ModelException("Application is already compiled");
            foreach (var pendingConnection in _pendingConnections)
            {
                var source = GetComponentByAlias(pendingConnection.SourceAlias);
                if (source == null)
                    continue;
                var target = GetComponentByAlias(pendingConnection.TargetAlias);
                if (target == null)
                    continue;
                _ = new Connection(source, target, pendingConnection.Label);
            }

            IsCompiled = true;
        }
    }
}