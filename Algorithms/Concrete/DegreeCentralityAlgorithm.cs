using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class DegreeCentralityAlgorithm
    {
        public Dictionary<Node, int> Execute(Graph graph, int topN = 5)
        {
            var centralityScores = new Dictionary<Node, int>();

            foreach (var node in graph.Nodes)
            {
                // Bağlantı sayısını (Degree) hesapla
                int degree = graph.Edges.Count(e => e.Source == node || e.Target == node);
                centralityScores[node] = degree;
            }

            // Puana göre çoktan aza sırala ve ilk N kişiyi al
            return centralityScores.OrderByDescending(x => x.Value)
                                   .Take(topN)
                                   .ToDictionary(x => x.Key, x => x.Value);
        }
    }
}