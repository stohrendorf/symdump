using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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

        public void AddNode(INode node) {
            if(!_nodes.Contains(node))
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
            if (_nodes.Contains(oldNode))
            {
                _nodes.Remove(oldNode);
                AddNode(newNode);
                
                var oldEdges = _edges.ToList();
                _edges = new HashSet<IEdge>();
                foreach (var e in oldEdges)
                {
                    if (e.To.Equals(oldNode))
                    {
                        e.To = newNode;
                    }
                    if (e.From.Equals(oldNode))
                    {
                        e.From = newNode;
                    }
                    _edges.Add(e);
                }
            }

            Debug.Assert(!_nodes.Contains(oldNode));
            Debug.Assert(_nodes.Contains(newNode));
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
    }
}
