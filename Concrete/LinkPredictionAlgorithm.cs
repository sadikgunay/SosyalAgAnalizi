using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class LinkPredictionAlgorithm
    {
        public List<string> Execute(Graph graph)
        {
            var suggestions = new List<dynamic>();

            // Tüm ikilileri kontrol et
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                for (int j = i + 1; j < graph.Nodes.Count; j++)
                {
                    var n1 = graph.Nodes[i];
                    var n2 = graph.Nodes[j];

                    // Zaten baðlýlarsa geç
                    bool isConnected = graph.Edges.Any(e =>
                        (e.Source == n1 && e.Target == n2) ||
                        (e.Source == n2 && e.Target == n1));

                    if (!isConnected)
                    {
                        // Ortak komþularý bul
                        var n1Neighbors = GetNeighbors(graph, n1);
                        var n2Neighbors = GetNeighbors(graph, n2);
                        var common = n1Neighbors.Intersect(n2Neighbors).ToList();

                        if (common.Count > 0)
                        {
                            suggestions.Add(new
                            {
                                Source = n1.Name,
                                Target = n2.Name,
                                Score = common.Count
                            });
                        }
                    }
                }
            }

            // En çok ortak arkadaþý olandan aza doðru sýrala
            return suggestions.OrderByDescending(x => x.Score)
                              .Take(5)
                              .Select(x => $"{x.Source} - {x.Target} (Ortak Arkadaþ: {x.Score})")
                              .ToList<string>();
        }

        private List<string> GetNeighbors(Graph graph, Node n)
        {
            var neighbors = new List<string>();
            foreach (var edge in graph.Edges)
            {
                if (edge.Source.Id == n.Id) neighbors.Add(edge.Target.Id);
                else if (edge.Target.Id == n.Id) neighbors.Add(edge.Source.Id);
            }
            return neighbors;
        }
    }
}