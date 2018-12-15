using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace core.cfg
{
    public class TarjanLengauer
    {
        public TarjanLengauer(IGraph graph)
        {
            Dfs(graph, out var vertices, out var semi, out var dfsParents);

            var buckets = new Dictionary<INode, ISet<INode>>();
            var roots = new Dictionary<INode, INode>();
            var labels = vertices.ToDictionary(v => v, v => v);

            for (var i = vertices.Count - 1; i >= 1; --i)
            {
                var cursor = vertices[i];
                foreach (var predecessor in cursor.Ins.Select(e => e.From))
                {
                    var u = Eval(predecessor, roots, semi, labels);
                    if (semi[u] < semi[cursor])
                        semi[cursor] = semi[u];

                    if (!buckets.TryGetValue(vertices[semi[cursor]], out var bucket))
                        buckets[vertices[semi[cursor]]] = bucket = new HashSet<INode>();
                    bucket.Add(cursor);

                    // Link(parent[w], w);
                    roots[cursor] = dfsParents[cursor];
                    if (buckets.ContainsKey(dfsParents[cursor]))
                        foreach (var v2 in buckets[dfsParents[cursor]])
                        {
                            var u2 = Eval(v2, roots, semi, labels);
                            Dominators[v2] = semi[u2] < semi[v2] ? u2 : dfsParents[cursor];
                        }

                    buckets.Remove(dfsParents[cursor]);
                }
            }

            for (var i = 1; i < vertices.Count; ++i)
            {
                var cursor = vertices[i];
                if (!Dominators[cursor].Equals(vertices[semi[cursor]]))
                    Dominators[cursor] = Dominators[Dominators[cursor]];
            }

            Dominators.Remove(graph.Nodes.First(n => n is EntryNode));
        }

        public IDictionary<INode, INode> Dominators { get; } = new Dictionary<INode, INode>();

        private static void Dfs(IGraph graph, out IList<INode> vertex, out IDictionary<INode, int> semi,
            out IDictionary<INode, INode> dfsParent)
        {
            Debug.Assert(graph.Nodes.Count(n => n is EntryNode) == 1);
            var nodeCount = graph.Nodes.Count();
            vertex = new List<INode>(nodeCount);
            semi = new Dictionary<INode, int>(nodeCount);
            dfsParent = new Dictionary<INode, INode>(nodeCount);

            Dfs(vertex, semi, dfsParent, graph.Nodes.First(n => n is EntryNode));
        }

        private static void Dfs(ICollection<INode> vertices, IDictionary<INode, int> semi,
            IDictionary<INode, INode> dfsParents,
            INode v)
        {
            semi[v] = vertices.Count;
            vertices.Add(v);
            foreach (var w in v.Outs.Select(e => e.To))
            {
                if (semi.ContainsKey(w))
                {
                    Debug.Assert(dfsParents.ContainsKey(w));
                    continue;
                }

                Debug.Assert(!dfsParents.ContainsKey(w));
                dfsParents[w] = v;
                Dfs(vertices, semi, dfsParents, w);
            }
        }

        private static INode Eval(INode v, IDictionary<INode, INode> roots, IDictionary<INode, int> semi,
            IDictionary<INode, INode> labels)
        {
            if (!roots.ContainsKey(v))
                return v;

            UpdateRoots(v, roots, semi, labels);
            return labels[v];
        }

        private static void UpdateRoots(INode cursor, IDictionary<INode, INode> roots, IDictionary<INode, int> semi,
            IDictionary<INode, INode> labels)
        {
            var r = roots[cursor];
            if (!roots.ContainsKey(r))
                return;

            UpdateRoots(r, roots, semi, labels);
            roots[cursor] = roots[r];
            if (semi[labels[r]] < semi[labels[cursor]])
                labels[cursor] = labels[r];
        }
    }
}