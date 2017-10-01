using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using core.util;
using exefile.controlflow.cfg;
using exefile.dataflow;
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
            return _appState.ExeFile.RelocatedInstructions
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

        private string ToLabel(INode node)
        {
            var sb = new StringBuilder();
            if(_appState.SymFile != null)
                node.Dump(new IndentedTextWriter(new StringWriter(sb)), new DataFlowState(_appState.SymFile));
            else
                node.Dump(new IndentedTextWriter(new StringWriter(sb)), null);
            return sb.ToString();
        }
        
        [HttpGet("decompile/{offset}")]
        public VisGraph Decompile([FromRoute] uint offset)
        {
            var visGraph = new VisGraph();

            try
            {
                var graph = _appState.ExeFile?.AnalyzeControlFlow(offset);

                IDictionary<INode, INode> doms = null;
                if (graph != null)
                {
                    doms = new TarjanLengauer(graph).Dominators;
                }

                var nodes = graph?.Nodes
                    .ToDictionary(v => v.Id, v => new VisNode
                    {
                        Id = v.Id,
                        Label = ToLabel(v),
                        Color = v is EntryNode ? "#00c000" : v is ExitNode ? "#c00000" : doms.Values.Contains(v) ? "#00c0ff" : "#c0c0ff"
                    });

                visGraph.Nodes = nodes?.Values.ToList();

                visGraph.Edges = graph?.Edges
                    .Select(e => new VisEdge
                    {
                        From = nodes[e.From.Id],
                        To = nodes[e.To.Id],
                        Color = (e is TrueEdge) ? "#00a000" : (e is FalseEdge) ? "#ff0000" : "#0000ff",
                        Dashes = e is CaseEdge
                    })
                    .ToList();

#if false
                if (doms == null)
                    return visGraph;
                
                foreach (var d in doms)
                {
                    visGraph.Edges.Add(new VisEdge
                    {
                        From = nodes[d.Value.Id],
                        To = nodes[d.Key.Id],
                        Color = "#404040",
                        Dashes = true,
                        Smooth = new VisEdge.VisSmooth
                        {
                            Enabled = false
                        }
                    });
                }
#endif

                return visGraph;
            }

            catch (Exception ex)
            {
                logger.Error(ex, "Control flow analysis failed");
                return visGraph;
            }
        }
    }
}
