using System;
using System.Collections.Generic;
using System.Linq;
using core.util;
using exefile.controlflow.cfg;
using frontend.Services;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace frontend.Controllers
{
    [Route("api/assembly")]
    public class AssemblyController : Controller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private readonly AppState _appState;

        public AssemblyController(AppState appState)
        {
            _appState = appState;
        }

        [HttpGet("instructions/{offset}/{length}")]
        public IEnumerable<LineInfo> Instructions([FromRoute] int offset, [FromRoute] int length)
        {
            return _appState.ExeFile.Instructions
                .Where(kv => kv.Key >= offset)
                .OrderBy(kv => kv.Key)
                .Take(length)
                .Select(kv => new LineInfo
                {
                    Text = kv.Value.AsReadable(),
                    Address = kv.Key,
                    JumpTarget = kv.Value.JumpTarget
                });
        }

        [HttpGet("decompile/{offset}")]
        public VisGraph Decompile([FromRoute] uint offset)
        {
            var visGraph = new VisGraph();

            try
            {
                var graph = _appState.ExeFile?.AnalyzeControlFlow(offset);
#if DEBUG
                if (graph != null)
                {
                    Console.WriteLine("Graph nodes:");
                    foreach (var n in graph.Nodes)
                    {
                        n.Dump(new IndentedTextWriter(Console.Out));
                    }
                    Console.WriteLine("Graph edges:");
                    foreach (var e in graph.Edges)
                    {
                        Console.WriteLine(e);
                    }
                }
#endif

                var nodes = graph?.Nodes
                    .ToDictionary(v => v.Id, v => new VisNode {Id = v.Id, Label = v.ToString()});

                visGraph.Nodes = nodes?.Values.ToList();

                visGraph.Edges = graph?.Edges
                    .Select(e => new VisEdge
                    {
                        From = nodes?[e.From.Id],
                        To = nodes?[e.To.Id],
                        Color = (e is TrueEdge) ? "#00a000" : (e is FalseEdge) ? "#ff0000" : "#0000ff"
                    })
                    .ToList();

                return visGraph;
            }

            catch (Exception ex)
            {
                logger.Error(ex, "Decompilation failed");
                logger.Error(ex.StackTrace);
                return visGraph;
            }
        }
    }
}
