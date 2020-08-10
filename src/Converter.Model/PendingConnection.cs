namespace Converter.Model
{
    internal class PendingConnection
    {
        public PendingConnection(string sourceAlias, string targetAlias, string label)
        {
            SourceAlias = sourceAlias;
            TargetAlias = targetAlias;
            Label = label;
        }

        public string SourceAlias { get; }

        public string TargetAlias { get; }
        public string Label { get; }
    }
}