using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Core; // Bizim Node ve Graph sınıflarımız

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class DegreeCentralityAlgorithm
	{
		// Geriye "Kişi" ve "Bağlantı Sayısı" çiftlerinden oluşan bir liste döndürür
		public List<KeyValuePair<Node, int>> Execute(Graph graph, int topN = 5)
		{
			var centralityScores = new Dictionary<Node, int>();

			// 1. Herkesin bağlantı sayısını (Derecesini) hesapla
			foreach (var node in graph.Nodes)
			{
				// Bu düğümün dahil olduğu (kaynak veya hedef) kenar sayısını bul
				int degree = graph.Edges.Count(e => e.Source.Id == node.Id || e.Target.Id == node.Id);
				centralityScores.Add(node, degree);
			}

			// 2. Puana göre Büyükten Küçüğe sırala ve ilk N kişiyi al
			var sortedList = centralityScores.OrderByDescending(x => x.Value)
											 .Take(topN)
											 .ToList();

			return sortedList;
		}
	}
}