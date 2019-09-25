using System;
using System.Collections.Generic;
using System.IO;
using Mono.Options;
using NLog;
using NLog.Config;
using NLog.Targets;
using symdump.exefile;
using symdump.exefile.util;
using symdump.symfile;

namespace symdump
{
    internal static class Program
    {
        private static readonly ILogger logger = LogManager.GetCurrentClassLogger();

        private static void ConfigureLogging()
        {
            var config = new LoggingConfiguration();

            var consoleTarget = new ColoredConsoleTarget("target1")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${level} ${message} ${exception}"
            };
            config.AddTarget(consoleTarget);
            config.AddRuleForAllLevels(consoleTarget); // all to console
            LogManager.Configuration = config;
        }

        private static void Main(string[] args)
        {
            ConfigureLogging();

            string flatFilename = null;
            string structuredOut = null;
            string disassemblyFile = null;

            var options = new OptionSet
            {
                {"f|flat=", "Dump flat output to file", _ => flatFilename = _},
                {"o|out=", "Dump structured output to file", _ => structuredOut = _},
                {"d|disassembly=", "Disassemble associated .exe when available to file", _ => disassemblyFile = _}
            };

            IList<string> extraArgs;
            try
            {
                extraArgs = options.Parse(args);
            }
            catch (OptionException e)
            {
                logger.Error(e.Message);
                options.WriteOptionDescriptions(Console.Out);
                Console.WriteLine("Note that '-' is a valid filename, resulting in outputting to the console");
                return;
            }

            if (extraArgs.Count != 1)
            {
                logger.Error("Please provide a .SYM file for processing");
                options.WriteOptionDescriptions(Console.Out);
                Console.WriteLine("Note that '-' is a valid filename, resulting in outputting to the console");
                return;
            }

            if (flatFilename != null)
            {
                logger.Info($"Dumping {extraArgs[0]} to {flatFilename} in flat format");
                using (var fs = new FileStream(extraArgs[0], FileMode.Open))
                {
                    new SymFile(new BinaryReader(fs), flatFilename);
                }
            }

            if (structuredOut == null && disassemblyFile == null)
                return;

            SymFile symFile;
            using (var fs = new FileStream(extraArgs[0], FileMode.Open))
            {
                symFile = new SymFile(new BinaryReader(fs), null);
                if (structuredOut != null)
                {
                    logger.Info($"Dumping {extraArgs[0]} to {structuredOut} in structured format");
                    using (var outFs = structuredOut == "-" ? Console.Out : File.CreateText(structuredOut))
                    {
                        symFile.Dump(outFs);
                    }
                }
            }

            if (disassemblyFile == null)
                return;

            var exeFilename = Path.ChangeExtension(extraArgs[0], "EXE");
            if (!File.Exists(exeFilename))
            {
                logger.Warn($"EXE file {exeFilename} does not exist, skipping disassembly");
                return;
            }

            logger.Info($"Dumping {exeFilename} disassembly to {disassemblyFile}");
            using (var fs = new EndianBinaryReader(new FileStream(exeFilename, FileMode.Open)))
            {
                var exeFile = new ExeFile(fs, symFile);
                exeFile.Disassemble();
                using (var outFs = disassemblyFile == "-" ? Console.Out : File.CreateText(disassemblyFile))
                {
                    exeFile.Dump(outFs);
                }
            }
        }
    }
}
