using System.Collections.Generic;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class DFSAlgorithm : IGraphAlgorithm
	{
		public List<Node> Execute(Graph graph, Node startNode, Node endNode = null)
		{
			// Ziyaret edilenlerin listesi (Sırasıyla)
			List<Node> visitedOrder = new List<Node>();

			// DFS'nin sırrı "Yığın" (Stack) kullanmasıdır. (BFS kuyruk kullanır)
			Stack<Node> stack = new Stack<Node>();
			HashSet<string> visitedIds = new HashSet<string>(); // Tekrarı önlemek için

			// Başlangıcı yığına at
			stack.Push(startNode);

			while (stack.Count > 0)
			{
				// Yığının tepesindekini al (En son eklenen)
				Node current = stack.Pop();

				// Eğer daha önce gelmediysek işle
				if (!visitedIds.Contains(current.Id))
				{
					visitedIds.Add(current.Id);
					visitedOrder.Add(current);

					// Komşuları bul ve yığına ekle (Yönsüz graf için her iki yönü de kontrol et)
					// Not: Stack yapısı gereği komşuları tersten eklersek, 
					// soldan sağa doğru doğal bir akış elde ederiz ama zorunlu değil.
					foreach (var edge in graph.Edges)
					{
						if (edge.Source.Id == current.Id && !visitedIds.Contains(edge.Target.Id))
						{
							stack.Push(edge.Target);
						}
						// Yönsüz graf olduğu için ters yönü de kontrol et
						else if (edge.Target.Id == current.Id && !visitedIds.Contains(edge.Source.Id))
						{
							stack.Push(edge.Source);
						}
					}
				}
			}

			return visitedOrder;
		}
	}
}
