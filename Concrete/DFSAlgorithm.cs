using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class DFSAlgorithm
    {
        public List<Node> Execute(Graph graph, Node startNode)
        {
            var visited = new HashSet<Node>();
            var stack = new Stack<Node>();
            var resultOrder = new List<Node>();

            stack.Push(startNode);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!visited.Contains(current))
                {
                    visited.Add(current);
                    resultOrder.Add(current);

                    var neighbors = graph.Edges
                        .Where(e => e.Source == current || e.Target == current)
                        .Select(e => e.Source == current ? e.Target : e.Source);

                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            stack.Push(neighbor);
                        }
                    }
                }
            }
            return resultOrder;
        }
    }
}