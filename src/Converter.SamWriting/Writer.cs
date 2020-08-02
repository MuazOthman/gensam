using Converter.Model;
using DotLiquid;
using DotLiquid.FileSystems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Converter.SamWriting
{
    public class Writer
    {
        private readonly Application application;
        public TemplateOptions TemplateOptions
        {
            get { return templateModel.Options; }
            set { templateModel.Options = value; }
        }

        private readonly TemplateModel templateModel;

        public Writer(Application application)
        {
            this.application = application;
            if (!application.IsCompiled)
                application.Compile();
            templateModel = new TemplateModel();
            PrepareTemplate();
        }

        public void Write(string filePath)
        {
            Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            Template.FileSystem = new EmbeddedFileSystem(GetType().Assembly, "Converter.SamWriting");
            Template t = Template.Parse(GetTemplate());
            var s = t.Render(Hash.FromAnonymousObject(new { model = templateModel }));
            File.WriteAllText(filePath, s);
        }

        private void PrepareTemplate()
        {
            templateModel.HasRestApi = application.Components
                .Any(c => c.Type == ComponentType.RestEndpoint);
            templateModel.IsCorsEnabled = application.Components
                .Where(c => c.Type == ComponentType.RestEndpoint)
                .Any(c => c.InboundConnections.Any());
            templateModel.Tables = application.Components
                .Where(c => c.Type == ComponentType.Table)
                .Select(c => new TableModel
                {
                    Name = c.Name,
                    IsStreamEnabled = c.OutboundConnections.Any()
                })
                .ToList();
            templateModel.Functions = application.Components
                .Where(c => c.Type == ComponentType.Function)
                .Select(c => BuildFunction(c))
                .ToList();
            templateModel.Topics = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .Select(c => c.Name)
                .ToList();
        }

        private FunctionModel BuildFunction(Component component)
        {
            var result = new FunctionModel
            {
                Name = component.Name,
            };
            foreach (var conn in component.OutboundConnections)
            {
                switch (conn.Target.Type)
                {
                    case ComponentType.Browser:
                        // impossible
                        break;
                    case ComponentType.Bucket:
                        break;
                    case ComponentType.EventBus:
                        break;
                    case ComponentType.Function:
                        break;
                    case ComponentType.Queue:
                        break;
                    case ComponentType.RestEndpoint:
                        // impossible
                        break;
                    case ComponentType.Schedule:
                        // impossible
                        break;
                    case ComponentType.Table:
                        result.EnvironmentVariables.Add($"{conn.Target.Name}Name", $"!Ref {conn.Target.Name}");
                        result.Policies.Add("DynamoDBCrudPolicy", $"TableName: !Ref {conn.Target.Name}");
                        break;
                    case ComponentType.Topic:
                        result.EnvironmentVariables.Add($"{conn.Target.Name}Arn", $"!Ref {conn.Target.Name}");
                        result.Policies.Add("SNSPublishMessagePolicy", $"TopicName: !GetAtt {conn.Target.Name}.TopicName");
                        break;
                }
            }
            foreach (var conn in component.InboundConnections)
            {
                switch (conn.Source.Type)
                {
                    case ComponentType.Browser:
                        // impossible
                        break;
                    case ComponentType.Bucket:
                        break;
                    case ComponentType.EventBus:
                        break;
                    case ComponentType.Function:
                        break;
                    case ComponentType.Queue:
                        break;
                    case ComponentType.RestEndpoint:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "_"),
                            Type = "Api",
                            Properties = new Dictionary<string, string>
                            {
                                { "Path", conn.Source.Properties["Endpoint"]},
                                { "Method", conn.Source.Properties["HttpMethod"]}
                            }
                        });
                        if (conn.Source.InboundConnections.Any(conn2 => conn2.Source.Type == ComponentType.Browser))
                        {
                            result.EnvironmentVariables.Add("AllowedDomains", "!Ref AllowedDomains");
                        }
                        break;
                    case ComponentType.Schedule:
                        break;
                    case ComponentType.Table:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "_") + "Stream",
                            Type = "DynamoDB",
                            Properties = new Dictionary<string, string>
                            {
                                { "Stream", $"!GetAtt {conn.Source.Name}.StreamArn"},
                                { "BatchSize", "10"},
                                { "StartingPosition", "TRIM_HORIZON"},
                                { "BisectBatchOnFunctionError", "true"}
                            }
                        });
                        break;
                    case ComponentType.Topic:
                        break;
                }
            }
            return result;
        }

        private string GetTemplate()
        {
            var assembly = GetType().Assembly;
            var resourceName = "Converter.SamWriting.template.liquid";

            using Stream stream = assembly.GetManifestResourceStream(resourceName);
            using StreamReader reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}
