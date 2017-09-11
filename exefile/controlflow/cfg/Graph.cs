using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;

namespace exefile.controlflow.cfg
{
    public class Graph : IGraph
    {
        private readonly ISet<INode> _nodes = new HashSet<INode>();
        private ISet<IEdge> _edges = new HashSet<IEdge>();

        public IEnumerable<INode> Nodes => _nodes;
        public IEnumerable<IEdge> Edges => _edges;

        public IEnumerable<IEdge> GetIns(INode node)
            => _edges.Where(e => e.To.Equals(node));

        public IEnumerable<IEdge> GetOuts(INode node)
            => _edges.Where(e => e.From.Equals(node));

        public void AddNode(INode node)
        {
            _nodes.Add(node);
        }

        public void RemoveNode(INode node)
        {
            Debug.Assert(_nodes.Contains(node));

            _nodes.Remove(node);
            var old = _edges;
            _edges = new HashSet<IEdge>();
            foreach (var e in old.Where(e => !e.To.Equals(node) && !e.From.Equals(node)))
            {
                _edges.Add(e);
            }

            Debug.Assert(!_nodes.Contains(node));
        }

        public void ReplaceNode(INode oldNode, INode newNode)
        {
            if (!_nodes.Contains(oldNode))
                return;
            
            _nodes.Remove(oldNode);
            AddNode(newNode);

            var oldEdges = _edges.ToList();
            _edges = new HashSet<IEdge>();
            foreach (var e in oldEdges)
            {
                var from = e.From.Equals(oldNode) ? newNode : e.From;
                var to = e.To.Equals(oldNode) ? newNode : e.To;
                _edges.Add(e.CloneTyped(from, to));
            }
        }

        public void AddEdge(IEdge edge)
        {
            Debug.Assert(_nodes.Contains(edge.From));
            Debug.Assert(_nodes.Contains(edge.To));
            _edges.Add(edge);
        }

        public void RemoveEdge(IEdge edge)
        {
            Debug.Assert(_nodes.Contains(edge.From));
            Debug.Assert(_nodes.Contains(edge.To));
            Debug.Assert(_edges.Contains(edge));
            _edges.Remove(edge);
        }

        public bool Contains(INode node) => _nodes.Contains(node);

        public bool Validate()
        {
            return _edges.Select(e => e.From).All(n => _nodes.Contains(n))
                   && _edges.Select(e => e.To).All(n => _nodes.Contains(n))
                   && _nodes.All(n => _edges.Any(e => n.Equals(e.From) || n.Equals(e.To)));
        }

        /// <summary>
        /// Ensures that each conditional node has the same type of incoming conditional edges,
        /// i.e. all incoming boolean edges are either <see cref="TrueEdge"/> or <see cref="FalseEdge"/>,
        /// but not both.  Note that <see cref="AlwaysEdge"/> are not considered here.
        /// </summary>
        public void MakeUniformBooleanEdges()
        {
            var nodes = GetTopologicallyOrdered();
            // can't use foreach because we're modifying the collection in the loop.
            for (int i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                var incoming = node.Ins.ToList();
                if (incoming.Count < 2
                    || !incoming.All(e => e is TrueEdge || e is FalseEdge))
                    continue;

                // either use the color of an incoming edge that has a source node that's already inverted,
                // or make all edges a FalseEdge
                bool color = incoming.FirstOrDefault(e => e.From is NotNode) is TrueEdge;

                foreach (var e in incoming)
                {
                    var incomingColor = e is TrueEdge;
                    if (incomingColor == color)
                        continue;

                    nodes[nodes.IndexOf(e.From)] = new NotNode(e.From);
                }
                Debug.Assert(node.Ins.All(e => e is TrueEdge) || node.Ins.All(e => e is FalseEdge));
            }
        }

        /// <summary>
        /// Re-orders edges so that <see cref="TrueEdge"/>s are first, then <see cref="FalseEdge"/>s, then all other types.
        /// </summary>
        /// <param name="edges">The edges to reorder.</param>
        /// <returns>The topologically ordered edges.</returns>
        private static IEnumerable<IEdge> GetTopologicallyOrdered(ICollection<IEdge> edges)
        {
            return edges
                .Where(e => e is TrueEdge)
                .Concat(edges.Where(e => e is FalseEdge))
                .Concat(edges.Where(e => !(e is TrueEdge || e is FalseEdge)));
        }

        /// <inheritdoc cref="GetTopologicallyOrdered(System.Collections.Generic.ICollection{exefile.controlflow.cfg.IEdge})"/>
        private static IEnumerable<IEdge> GetTopologicallyOrdered(IEnumerable<IEdge> edges)
        {
            return GetTopologicallyOrdered(edges.ToList());
        }

        /// <summary>
        /// Creates a topologically ordered list of nodes of the graph.
        /// </summary>
        /// <returns>The topologically ordered node list.</returns>
        private IList<INode> GetTopologicallyOrdered()
        {
            Debug.Assert(_nodes.Count(n => n is EntryNode) == 1);
            var entry = _nodes.First(n => n is EntryNode);
            var result = new List<INode>();
            GetTopologicallyOrdered(entry, new HashSet<INode>(), result);
            result.Reverse();
            return result;
        }

        private static void GetTopologicallyOrdered([NotNull] INode node, [NotNull] ISet<INode> seen,
            [NotNull] ICollection<INode> result)
        {
            if (!seen.Add(node))
                return;
            foreach (var successor in GetTopologicallyOrdered(node.Outs).Select(e => e.To))
            {
                GetTopologicallyOrdered(successor, seen, result);
            }
            result.Add(node);
        }
    }
}
