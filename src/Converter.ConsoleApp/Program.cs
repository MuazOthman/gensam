using CommandLine;
using CommandLine.Text;
using Converter.DiagramFileReading;
using Converter.Model;
using Converter.SamWriting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Converter.ConsoleApp
{
    public class Program
    {
        class Options
        {
            [Option("input", Default = ".", HelpText = "Input files/folders to be processed.")]
            public IEnumerable<string> InputPaths { get; set; }

            [Option("output", Default = "template.yaml", Required = false, HelpText = "Output file for SAM template.")]
            public string OutputPath { get; set; }

            [Option("writersettings", Default = null, Required = false, HelpText = "Path to JSON file with SAM writer settings.")]
            public string WriterSettingsPath { get; set; }

            [Option("exts", Default = new[] { "drawio" }, Required = false, HelpText = "File extensions to use when scanning folders for diagram files.")]
            public IEnumerable<string> Extensions { get; set; }

            [Usage(ApplicationAlias = "gensam")]
            public static IEnumerable<Example> Examples
            {
                get
                {
                    return new List<Example>() {
                        new Example("Generate a SAM template for the combination of all draw.io diagrams on a folder", new Options { InputPaths = new []{"C:\\diagrams" } })
                      };
                }
            }
        }
        private static void Main(string[] args)
        {
            var helpWriter = new StringWriter();
            var parser = new Parser(with => with.HelpWriter = helpWriter);
            parser.ParseArguments<Options>(args)
               .WithParsed(RunOptions)
               .WithNotParsed(errs => HandleParseError(errs, helpWriter));
        }
        private static void RunOptions(Options options)
        {
            Console.WriteLine(CopyrightInfo.Default);
            Console.WriteLine(HeadingInfo.Default);
            Console.WriteLine();

            var writerSettings = string.IsNullOrEmpty(options.WriterSettingsPath)
                ? (WriterSettings)null
                : JsonConvert.DeserializeObject<WriterSettings>(File.ReadAllText(options.WriterSettingsPath));

            var app = new Application();
            var reader = new Reader(app);
            var files = new List<string>();

            foreach (var input in options.InputPaths)
            {
                FileAttributes attr = File.GetAttributes(input);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    foreach (var ext in options.Extensions)
                        foreach (var file in Directory.GetFiles(input, $"*.{ext}", SearchOption.AllDirectories))
                        {
                            Console.WriteLine($"Reading file: {file}");
                            reader.ReadFile(file);
                            files.Add(file);
                        }
                }
                else
                {
                    Console.WriteLine($"Reading file: {input}");
                    reader.ReadFile(input);
                    files.Add(input);
                }
            }
            app.Compile();

            foreach (var c in app.Components)
            {
                Console.WriteLine(c);
                foreach (var conn in c.OutboundConnections)
                {
                    Console.WriteLine($"\t=>{conn.Target} ({conn.Label})");
                }
            }

            var writer = new Writer(app)
            {
                WriterSettings = writerSettings,
                IncludedFiles = files
            };
            Console.WriteLine($"Writing file: {options.OutputPath}");
            writer.Write(options.OutputPath);
        }

        private static void HandleParseError(IEnumerable<Error> errs, TextWriter writer)
        {
            if (errs.IsVersion() || errs.IsHelp())
                Console.WriteLine(writer.ToString());
            else
                Console.Error.WriteLine(writer.ToString());
        }
    }
}