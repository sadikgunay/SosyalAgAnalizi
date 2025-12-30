using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class DijkstraAlgorithm : IGraphAlgorithm
    {
        public List<Node> Execute(Graph graph, Node startNode, Node endNode)
        {
            // Performans için Dictionary ve HashSet kullanımı
            var distances = new Dictionary<Node, double>();
            var previous = new Dictionary<Node, Node>();
            var unvisited = new HashSet<Node>();

            // 1. Başlangıç Durumu
            foreach (var node in graph.Nodes)
            {
                distances[node] = double.MaxValue;
                unvisited.Add(node);
            }
            distances[startNode] = 0;

            // 2. Ana Döngü
            while (unvisited.Count > 0)
            {
                // En küçük mesafeli düğümü bul (Manuel PriorityQueue mantığı)
                Node current = null;
                double minDist = double.MaxValue;

                foreach (var node in unvisited)
                {
                    if (distances[node] < minDist)
                    {
                        minDist = distances[node];
                        current = node;
                    }
                }

                // Hedef bulunduysa veya gidilecek yol kalmadıysa dur
                if (current == null || current.Id == endNode.Id) break;
                if (minDist == double.MaxValue) break;

                unvisited.Remove(current);

                // Komşuları bul
                var neighbors = graph.Edges.Where(e => e.Source.Id == current.Id || e.Target.Id == current.Id);

                foreach (var edge in neighbors)
                {
                    var neighbor = (edge.Source.Id == current.Id) ? edge.Target : edge.Source;

                    // Zaten ziyaret edildiyse atla
                    if (!unvisited.Contains(neighbor)) continue;

                    // Yeni mesafe hesabı: Mevcut mesafe + Kenar Ağırlığı
                    double newDist = distances[current] + edge.Weight;

                    if (newDist < distances[neighbor])
                    {
                        distances[neighbor] = newDist;
                        previous[neighbor] = current;
                    }
                }
            }

            // 3. Yolu Geriye Doğru Oluştur (Backtracking)
            var path = new List<Node>();
            var curr = endNode;

            // Eğer hedefe hiç ulaşılamadıysa (previous listesinde yoksa) boş dön
            if (!previous.ContainsKey(curr) && curr != startNode) return new List<Node>();

            while (curr != null && previous.ContainsKey(curr))
            {
                path.Add(curr);
                curr = previous[curr];
            }

            // Başlangıç düğümünü de ekle ve ters çevir
            if (curr != null && curr.Id == startNode.Id)
            {
                path.Add(startNode);
                path.Reverse();
                return path;
            }

            return new List<Node>(); // Yol yok
        }
    }
}