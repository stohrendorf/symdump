using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using exefile.controlflow;
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

        private static IEnumerable<VisEdge> GetEdges(IBlock block, IDictionary<string, VisNode> nodes)
        {
            if (block.TrueExit != null)
                yield return new VisEdge
                {
                    From = nodes[block.GetNodeName()],
                    To = nodes[block.TrueExit.GetNodeName()]
                };

            if (block.FalseExit != null)
                yield return new VisEdge
                {
                    From = nodes[block.GetNodeName()],
                    To = nodes[block.FalseExit.GetNodeName()]
                };
        }

        [HttpGet("decompile/{offset}")]
        public VisGraph Decompile([FromRoute] uint offset)
        {
            var graph = new VisGraph();

            try
            {
                var decompiled = _appState.ExeFile?.Decompile(offset);

                var nodes = decompiled?.Values
                    .Select(v => new VisNode {Id = v.GetNodeName(), Label = v.ToString()})
                    .ToDictionary(v => v.Id, v => v);

                graph.Nodes = nodes?.Values.ToList();

                graph.Edges = decompiled?.Values
                    .SelectMany(v => GetEdges(v, nodes))
                    .ToList();

                return graph;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Decompilation failed");
                logger.Error(ex.StackTrace);
                return graph;
            }
        }
    }
}
