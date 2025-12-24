using System.Collections.Generic;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class BFSAlgorithm : IGraphAlgorithm
	{
		public List<Node> Execute(Graph graph, Node startNode, Node endNode = null)
		{
			// Ziyaret edilenlerin listesi (Sonuç kümesi)
			List<Node> visitedOrder = new List<Node>();

			// Kuyruk yapısı (Sıradakiler)
			Queue<Node> queue = new Queue<Node>();
			HashSet<string> visitedIds = new HashSet<string>(); // Tekrarı önlemek için

			// Başlangıcı ekle
			queue.Enqueue(startNode);
			visitedIds.Add(startNode.Id);

			while (queue.Count > 0)
			{
				Node current = queue.Dequeue();
				visitedOrder.Add(current);

				// Komşuları bul (Yönsüz graf için her iki yönü de kontrol et)
				foreach (var edge in graph.Edges)
				{
					// Eğer kenarın kaynağı bizsek, hedefi komşudur
					if (edge.Source.Id == current.Id && !visitedIds.Contains(edge.Target.Id))
					{
						visitedIds.Add(edge.Target.Id);
						queue.Enqueue(edge.Target);
					}
					// Yönsüz graf olduğu için tam tersini de kontrol et
					else if (edge.Target.Id == current.Id && !visitedIds.Contains(edge.Source.Id))
					{
						visitedIds.Add(edge.Source.Id);
						queue.Enqueue(edge.Source);
					}
				}
			}

			return visitedOrder;
		}
	}
}
