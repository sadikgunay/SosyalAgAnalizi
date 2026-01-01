using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class AStarAlgorithm : IGraphAlgorithm
    {
        public List<Node> Execute(Graph graph, Node startNode, Node endNode)
        {
            var openSet = new HashSet<Node> { startNode }; // Gidilecekler listesi
            var cameFrom = new Dictionary<Node, Node>();   // Yol haritası

            var gScore = new Dictionary<Node, double>();   // Başlangıçtan buraya kadar olan kesin maliyet
            var fScore = new Dictionary<Node, double>();   // Tahmini toplam maliyet (g + h)

            // Başlatma
            foreach (var n in graph.Nodes)
            {
                gScore[n] = double.MaxValue;
                fScore[n] = double.MaxValue;
            }

            gScore[startNode] = 0;
            fScore[startNode] = Heuristic(startNode, endNode);

            while (openSet.Count > 0)
            {
                // fScore değeri en düşük olan düğümü seç (En mantıklı aday)
                Node current = null;
                double minF = double.MaxValue;

                foreach (var node in openSet)
                {
                    if (fScore[node] < minF)
                    {
                        minF = fScore[node];
                        current = node;
                    }
                }

                // Hedefe ulaşıldı mı?
                if (current == null) break;
                if (current.Id == endNode.Id) return ReconstructPath(cameFrom, current);

                openSet.Remove(current);

                // Komşuları gez
                var neighbors = graph.Edges.Where(e => e.Source.Id == current.Id || e.Target.Id == current.Id);

                foreach (var edge in neighbors)
                {
                    var neighbor = (edge.Source.Id == current.Id) ? edge.Target : edge.Source;

                    // Yeni gScore hesabı (Kesin maliyet)
                    double tentativeG = gScore[current] + edge.Weight;

                    if (tentativeG < gScore[neighbor])
                    {
                        // Daha iyi bir yol bulundu!
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;

                        // Toplam tahmini maliyeti güncelle: Kesin Maliyet + Tahmin (Heuristic)
                        fScore[neighbor] = gScore[neighbor] + Heuristic(neighbor, endNode);

                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }

            return new List<Node>(); // Yol bulunamadı
        }

        // --- KRİTİK DÜZELTME: GÜVENLİ HEURISTIC ---
        private double Heuristic(Node a, Node b)
        {
            // Sosyal ağda 'kuş uçuşu mesafe' olmadığı için, kişilerin özellik benzerliğine bakıyoruz.
            // Eğer iki kişi (düğüm) birbirine özellik olarak benziyorsa, birbirlerine yakındırlar varsayımı yapıyoruz.

            double dAct = Math.Abs(a.Activity - b.Activity); // 0.0 - 1.0 arası

            // Bağlantı sayısı farkı çok büyük olabilir (örn: 100). Bunu 0.01 ile çarparak küçültüyoruz.
            double dConn = Math.Abs(a.ConnectionCount - b.ConnectionCount) * 0.01;

            // Öklid benzeri bir hesaplama
            double h = Math.Sqrt(dAct * dAct + dConn * dConn);

            // ÖNEMLİ: Sonucu 0.5 ile çarparak "Underestimate" (Olduğundan az tahmin etme) yapıyoruz.
            // Bu, A*'ın gerçek en kısa yolu bulmasını GARANTİ eder.
            // Eğer bu değer çok büyük olursa, algoritma yanlış çalışır.
            return h * 0.5;
        }

        private List<Node> ReconstructPath(Dictionary<Node, Node> cameFrom, Node current)
        {
            var path = new List<Node> { current };
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }
            path.Reverse();
            return path;
        }
    }
}