using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class DijkstraAlgorithm : IGraphAlgorithm
	{
		public List<Node> Execute(Graph graph, Node startNode, Node endNode = null)
		{
			if (endNode == null) return new List<Node>();

			// 1. Hazırlık
			var distances = new Dictionary<string, double>();
			var previous = new Dictionary<string, Node>();
			var nodesQueue = new List<Node>();

			foreach (var node in graph.Nodes)
			{
				if (node.Id == startNode.Id)
					distances[node.Id] = 0;
				else
					distances[node.Id] = double.MaxValue;

				nodesQueue.Add(node);
			}

			// 2. Arama Döngüsü
			while (nodesQueue.Count > 0)
			{
				// Mesafesi en küçük olanı seç
				nodesQueue.Sort((x, y) => distances[x.Id].CompareTo(distances[y.Id]));
				Node current = nodesQueue[0];
				nodesQueue.RemoveAt(0);

				if (current.Id == endNode.Id) break; // Hedefe vardık
				if (distances[current.Id] == double.MaxValue) break; // Ulaşılabilir yol kalmadı

				// Komşuları gez (Yönsüz graf için her iki yönü de kontrol et)
				foreach (var edge in graph.Edges)
				{
					Node neighbor = null;
					// Bu düğümden çıkan kenarlara bak
					if (edge.Source.Id == current.Id)
					{
						neighbor = edge.Target;
					}
					// Yönsüz graf olduğu için bu düğüme gelen kenarlara da bak
					else if (edge.Target.Id == current.Id)
					{
						neighbor = edge.Source;
					}

					if (neighbor != null && nodesQueue.Contains(neighbor))
					{
						double alt = distances[current.Id] + edge.Weight; // Dinamik Ağırlığı Burada Kullanıyoruz!
						if (alt < distances[neighbor.Id])
						{
							distances[neighbor.Id] = alt;
							previous[neighbor.Id] = current;
						}
					}
				}
			}

			// 3. Yolu Geriye Doğru Oluştur (Bitiş -> Başlangıç)
			var path = new List<Node>();
			Node temp = endNode;

			if (previous.ContainsKey(temp.Id) || temp == startNode)
			{
				while (temp != null)
				{
					path.Add(temp);
					if (temp.Id == startNode.Id) break;
					temp = previous.ContainsKey(temp.Id) ? previous[temp.Id] : null;
				}
			}

			path.Reverse(); // Yolu düzelt (Başlangıç -> Bitiş)
			return path;
		}
	}
}
