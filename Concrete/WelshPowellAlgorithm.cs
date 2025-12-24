using System.Collections.Generic;
using System.Linq;

// --- ÇAKIÞMAYI ÖNLEYEN TAKMA ÝSÝMLER (ALIASES) ---
// Kodun geri kalanýnda "CoreGraph" dediðimizde bizim Graph sýnýfýmýz anlaþýlacak.
using CoreGraph = SocialNetworkGraph.App.Core.Graph;
using CoreNode = SocialNetworkGraph.App.Core.Node;
// "MsaglColor" dediðimizde boyama kütüphanesinin rengi anlaþýlacak.
using MsaglColor = Microsoft.Msagl.Drawing.Color;

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
	public class WelshPowellAlgorithm
	{
		// Renk Paletini "MsaglColor" türünden tanýmlýyoruz
		private readonly List<MsaglColor> _palette = new List<MsaglColor>
		{
			MsaglColor.Red, MsaglColor.Blue, MsaglColor.Green, MsaglColor.Yellow, MsaglColor.Orange,
			MsaglColor.Purple, MsaglColor.Cyan, MsaglColor.Magenta, MsaglColor.Brown, MsaglColor.Pink, MsaglColor.Gray
		};

		// Fonksiyon parametresi olarak bizim "CoreGraph"ýmýzý istiyoruz
		public Dictionary<string, MsaglColor> Execute(CoreGraph graph)
		{
			var nodeColors = new Dictionary<string, MsaglColor>();

			// 1. ADIM: Düðümlerin derecelerini hesapla
			var nodeDegrees = new Dictionary<string, int>();

			foreach (var node in graph.Nodes)
			{
				// CoreNode kullanýyoruz
				int degree = graph.Edges.Count(e => e.Source.Id == node.Id || e.Target.Id == node.Id);
				nodeDegrees[node.Id] = degree;
			}

			// 2. ADIM: Düðümleri derecelerine göre sýrala
			// OrderByDescending LINQ ifadesi CoreNode listesi üzerinde çalýþýr
			var sortedNodes = graph.Nodes.OrderByDescending(n => nodeDegrees[n.Id]).ToList();

			int colorIndex = 0;

			while (sortedNodes.Count > 0)
			{
				MsaglColor currentColor = (colorIndex < _palette.Count) ? _palette[colorIndex] : MsaglColor.Black;

				var firstNode = sortedNodes[0];
				nodeColors[firstNode.Id] = currentColor;
				sortedNodes.RemoveAt(0);

				var nodesToColor = new List<CoreNode>();

				foreach (var candidateNode in sortedNodes.ToList())
				{
					if (IsSafeToColor(graph, candidateNode, currentColor, nodeColors))
					{
						nodeColors[candidateNode.Id] = currentColor;
						nodesToColor.Add(candidateNode);
					}
				}

				foreach (var node in nodesToColor)
				{
					sortedNodes.Remove(node);
				}

				colorIndex++;
			}

			return nodeColors;
		}

		// Yardýmcý Fonksiyon
		private bool IsSafeToColor(CoreGraph graph, CoreNode node, MsaglColor color, Dictionary<string, MsaglColor> currentColors)
		{
			foreach (var edge in graph.Edges)
			{
				string neighborId = null;

				if (edge.Source.Id == node.Id) neighborId = edge.Target.Id;
				else if (edge.Target.Id == node.Id) neighborId = edge.Source.Id;

				if (neighborId != null && currentColors.ContainsKey(neighborId))
				{
					if (currentColors[neighborId] == color)
						return false;
				}
			}
			return true;
		}
	}
}