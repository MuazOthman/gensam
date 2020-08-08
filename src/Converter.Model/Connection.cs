namespace Converter.Model
{
    public class Connection
    {
        internal Connection(Component source, Component target, string label = "")
        {
            Source = source;
            Target = target;
            Label = label ?? "";
            Source.AddOutboundConnection(this);
            Target.AddInboundConnection(this);
        }

        public Component Source { get; }
        public Component Target { get; }
        public string Label { get; }
    }
}