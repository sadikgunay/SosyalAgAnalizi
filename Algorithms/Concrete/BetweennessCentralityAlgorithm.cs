using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class BetweennessCentralityAlgorithm
    {
        // Basitleþtirilmiþ Köprü Analizi:
        // Herkesin herkese giden en kýsa yolunu bul.
        // Kimin üzerinden çok geçiliyorsa o "Köprü"dür.
        public Dictionary<Node, int> Execute(Graph graph)
        {
            var traffic = new Dictionary<Node, int>();
            foreach (var n in graph.Nodes) traffic[n] = 0;

            var dijkstra = new DijkstraAlgorithm();

            // Örnekleme: Çok büyük graflarda yavaþlamasýn diye ilk 20 düðüm arasý bakýlýr
            // Küçük graflarda hepsi taranýr.
            var sampleNodes = graph.Nodes.Take(30).ToList();

            for (int i = 0; i < sampleNodes.Count; i++)
            {
                for (int j = i + 1; j < sampleNodes.Count; j++)
                {
                    var path = dijkstra.Execute(graph, sampleNodes[i], sampleNodes[j]);

                    // Yol üzerindeki ara duraklarýn puanýný artýr (Baþlangýç ve Bitiþ hariç)
                    if (path.Count > 2)
                    {
                        for (int k = 1; k < path.Count - 1; k++)
                        {
                            traffic[path[k]]++;
                        }
                    }
                }
            }

            return traffic.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }
    }
}// Updated by Atakan Cetli 
