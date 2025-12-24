using System.Collections.Generic;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class ConnectedComponentsAlgorithm
	{
		// Geriye "Listelerin Listesi"ni döndürür. Her iç liste bir adacıktır.
		public List<List<Node>> Execute(Graph graph)
		{
			var components = new List<List<Node>>();
			var visited = new HashSet<string>();

			foreach (var node in graph.Nodes)
			{
				if (!visited.Contains(node.Id))
				{
					// Yeni bir adacık bulduk!
					var newComponent = new List<Node>();

					// BFS ile bu adacıktaki herkesi bulalım
					var queue = new Queue<Node>();
					queue.Enqueue(node);
					visited.Add(node.Id);

					while (queue.Count > 0)
					{
						var current = queue.Dequeue();
						newComponent.Add(current);

						// Komşuları gez
						foreach (var edge in graph.Edges)
						{
							// Yönsüz graf mantığıyla (Gidip-Gelinebilen her yer aynı adadadır)
							Node neighbor = null;
							if (edge.Source.Id == current.Id) neighbor = edge.Target;
							else if (edge.Target.Id == current.Id) neighbor = edge.Source;

							if (neighbor != null && !visited.Contains(neighbor.Id))
							{
								visited.Add(neighbor.Id);
								queue.Enqueue(neighbor);
							}
						}
					}
					components.Add(newComponent);
				}
			}
			return components;
		}
	}
}