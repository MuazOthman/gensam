using Converter.DiagramFileReading;
using Converter.Model;
using System;

namespace Converter.ConsoleApp
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var app = new Application();
            var reader = new Reader(app);
            reader.ReadFile("web-app1.drawio");
            app.Compile();
            foreach (var c in app.Components)
            {
                Console.WriteLine(c);
                foreach (var conn in c.OutboundConnections)
                {
                    Console.WriteLine($"\t{conn.Label} => {conn.Target}");
                }
            }

            var writer = new SamWriting.Writer(app);
            writer.Write("example.yaml");
        }
    }
}