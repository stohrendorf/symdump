using System.IO;
using core.util;
using exefile;
using NLog;
using NLog.Config;
using NLog.Targets;
using symfile;

namespace symdump
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            var nlogConfig = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            nlogConfig.AddTarget("console", consoleTarget);
            consoleTarget.Layout =
                @"[${date:format=HH\:mm\:ss.fff} ${pad:padding=5:inner=${level:uppercase=true}}] ${logger} | ${message} ${exception:format=tostring}";
            var nlogDebugRule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            nlogConfig.LoggingRules.Add(nlogDebugRule);
            LogManager.Configuration = nlogConfig;


            SymFile symFile;
            using (var fs = new FileStream(args[0], FileMode.Open))
            {
                symFile = new SymFile(new BinaryReader(fs));
                //symFile.dump(Console.Out);
            }

            var exeFilename = Path.ChangeExtension(args[0], "EXE");

            if (!File.Exists(exeFilename))
                return;

            using (var fs = new EndianBinaryReader(new FileStream(exeFilename, FileMode.Open)))
            {
                var exeFile = new ExeFile(fs, symFile);
                exeFile.Disassemble();
                //exeFile.Decompile(symFile.Functions.Skip(200).First().Address);
            }
        }
    }
}
