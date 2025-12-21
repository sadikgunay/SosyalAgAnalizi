using System.Collections.Generic;
using System.Linq;

namespace SocialNetworkGraph.App.Core
{
	public class Graph
	{
		// Düğümleri ve Kenarları tutan listeler
		public List<Node> Nodes { get; private set; }
		public List<Edge> Edges { get; private set; }

		public Graph()
		{
			Nodes = new List<Node>();
			Edges = new List<Edge>();
		}

		// Grafiğe yeni düğüm ekleme
		public void AddNode(Node node)
		{
			// Aynı Id'ye sahip düğüm var mı kontrol et (çakışmayı önle)
			if (!Nodes.Any(n => n.Id == node.Id))
			{
				Nodes.Add(node);
			}
		}

		// İki düğüm arasına kenar (bağlantı) ekleme
		public void AddEdge(Node source, Node target)
		{
			// Kendine bağlantı (Loop) engelleme
			if (source == target) return;

			// Yönsüz graf için duplicate kontrolü (her iki yönü de kontrol et)
			bool exists = Edges.Any(e =>
				(e.Source.Id == source.Id && e.Target.Id == target.Id) ||
				(e.Source.Id == target.Id && e.Target.Id == source.Id));

			if (!exists)
			{
				Edges.Add(new Edge(source, target));
			}
		}

		// Grafiği temizleme (Yeni dosya yüklerken lazım olacak)
		public void Clear()
		{
			Nodes.Clear();
			Edges.Clear();
		}
	}
}
