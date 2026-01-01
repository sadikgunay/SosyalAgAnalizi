using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class BFSAlgorithm
    {
        public List<Node> Execute(Graph graph, Node startNode)
        {
            var visited = new HashSet<Node>();
            var queue = new Queue<Node>();
            var resultOrder = new List<Node>();

            visited.Add(startNode);
            queue.Enqueue(startNode);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                resultOrder.Add(current);

                // Komþularý bul
                var neighbors = graph.Edges
                    .Where(e => e.Source == current || e.Target == current)
                    .Select(e => e.Source == current ? e.Target : e.Source);

                foreach (var neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }
            return resultOrder;
        }
    }
}