using System;
using System.Collections.Generic;
using System.Linq;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class AStarAlgorithm : IGraphAlgorithm
	{
		public List<Node> Execute(Graph graph, Node startNode, Node endNode = null)
		{
			if (endNode == null) return new List<Node>();

			// A* için gerekli listeler
			var openSet = new List<Node> { startNode };
			var comeFrom = new Dictionary<string, Node>();

			// gScore: Başlangıçtan buraya kadar olan gerçek maliyet
			var gScore = new Dictionary<string, double>();
			// fScore: gScore + Tahmini Kalan Mesafe (Heuristic)
			var fScore = new Dictionary<string, double>();

			// Başlangıç değerlerini ata
			foreach (var node in graph.Nodes)
			{
				gScore[node.Id] = double.MaxValue;
				fScore[node.Id] = double.MaxValue;
			}

			gScore[startNode.Id] = 0;
			fScore[startNode.Id] = Heuristic(startNode, endNode);

			while (openSet.Count > 0)
			{
				// fScore değeri en düşük olanı seç
				openSet.Sort((a, b) => fScore[a.Id].CompareTo(fScore[b.Id]));
				Node current = openSet[0];

				if (current.Id == endNode.Id)
					return ReconstructPath(comeFrom, current); // Hedefe vardık!

				openSet.RemoveAt(0);

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

					if (neighbor != null)
					{
						double tentativeGScore = gScore[current.Id] + edge.Weight;

						if (tentativeGScore < gScore[neighbor.Id])
						{
							comeFrom[neighbor.Id] = current;
							gScore[neighbor.Id] = tentativeGScore;
							fScore[neighbor.Id] = gScore[neighbor.Id] + Heuristic(neighbor, endNode);

							if (!openSet.Any(n => n.Id == neighbor.Id))
								openSet.Add(neighbor);
						}
					}
				}
			}

			return new List<Node>(); // Yol bulunamadı
		}

		// Yolu geriye doğru oluşturma
		private List<Node> ReconstructPath(Dictionary<string, Node> comeFrom, Node current)
		{
			var totalPath = new List<Node> { current };
			while (comeFrom.ContainsKey(current.Id))
			{
				current = comeFrom[current.Id];
				totalPath.Insert(0, current);
			}
			return totalPath;
		}

		// Sezgisel Fonksiyon (Koordinat olmadığı için 0 dönüyoruz)
		private double Heuristic(Node a, Node b)
		{
			return 0;
		}
	}
}
