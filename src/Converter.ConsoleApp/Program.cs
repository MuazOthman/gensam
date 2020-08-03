using CommandLine;
using CommandLine.Text;
using Converter.DiagramFileReading;
using Converter.Model;
using System;
using System.Collections.Generic;
using System.IO;

namespace Converter.ConsoleApp
{
    public class Program
    {
        class Options
        {
            [Option("input", Required = true, HelpText = "Input files/folders to be processed.")]
            public IEnumerable<string> InputPaths { get; set; }

            [Option("output", Default = "template.yaml", Required = false, HelpText = "Output file for SAM template.")]
            public string OutputPath { get; set; }

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
            Parser.Default.ParseArguments<Options>(args)
               .WithParsed(RunOptions)
               .WithNotParsed(HandleParseError);
        }
        private static void RunOptions(Options options)
        {
            Console.WriteLine(CopyrightInfo.Default);
            Console.WriteLine(HeadingInfo.Default);
            Console.WriteLine();
            var app = new Application();
            var reader = new Reader(app);
            foreach (var input in options.InputPaths)
            {
                FileAttributes attr = File.GetAttributes(input);

                //detect whether its a directory or file
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    foreach (var ext in options.Extensions)
                        foreach (var file in Directory.GetFiles(input, $"*.{ext}"))
                        {
                            Console.WriteLine($"Reading file: {file}");
                            reader.ReadFile(file);
                        }
                else
                {
                    Console.WriteLine($"Reading file: {input}");
                    reader.ReadFile(input);
                }
            }
            app.Compile();

            var writer = new SamWriting.Writer(app);
            Console.WriteLine($"Writing file: {options.OutputPath}");
            writer.Write(options.OutputPath);
        }

        private static void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var err in errs)
            {
                Console.Error.WriteLine(err);
            }
        }
    }
}