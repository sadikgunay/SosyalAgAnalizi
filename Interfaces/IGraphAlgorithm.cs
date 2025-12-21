using System.Collections.Generic;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Interfaces
{
	public interface IGraphAlgorithm
	{
		// Her algoritma bir "Yürütme" (Execute) iþlevine sahip olmalý.
		// Parametreler: Hangi grafik? Baþlangýç kim? Bitiþ kim (varsa)?
		// Dönüþ: Etkilenen düðümlerin listesi (Yol veya Ziyaret edilenler)
		List<Node> Execute(Graph graph, Node startNode, Node endNode = null);
	}
}