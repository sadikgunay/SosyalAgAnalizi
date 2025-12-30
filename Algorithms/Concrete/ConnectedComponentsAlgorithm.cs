using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class ConnectedComponentsAlgorithm
    {
        public List<List<Node>> Execute(Graph graph)
        {
            var components = new List<List<Node>>();
            var visited = new HashSet<Node>();

            foreach (var node in graph.Nodes)
            {
                if (!visited.Contains(node))
                {
                    // Yeni bir ada (topluluk) bulduk
                    // Bu ada içindeki herkesi bulmak için BFS kullanıyoruz
                    var component = new BFSAlgorithm().Execute(graph, node);

                    // Bulunanları genel ziyaret listesine ekle
                    foreach (var visitedNode in component)
                    {
                        visited.Add(visitedNode);
                    }

                    components.Add(component);
                }
            }
            return components;
        }
    }
}