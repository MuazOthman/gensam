namespace Converter.Model
{
    public class Connection
    {
        internal Connection(Component source, Component target, string label = "")
        {
            Source = source;
            Target = target;
            Source.AddOutboundConnection(this);
            Target.AddInboundConnection(this);
            Label = label;
        }

        public Component Source { get; }
        public Component Target { get; }
        public string Label { get; }
    }
}