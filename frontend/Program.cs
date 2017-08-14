using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace frontend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var nlogConfig = new LoggingConfiguration();
            var consoleTarget = new ColoredConsoleTarget();
            nlogConfig.AddTarget("console", consoleTarget);
            consoleTarget.Layout = @"[${date:format=HH\:mm\:ss.fff} ${pad:padding=5:inner=${level:uppercase=true}}] ${logger} | ${message}";
            var nlogDebugRule = new LoggingRule("*", LogLevel.Debug, consoleTarget);
            nlogConfig.LoggingRules.Add(nlogDebugRule);
            LogManager.Configuration = nlogConfig;
            
            var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>()
                .Build();

            host.Run();
        }
    }
}
