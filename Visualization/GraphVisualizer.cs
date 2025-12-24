using System;
using Microsoft.Msagl.Drawing;
using CoreGraph = SocialNetworkGraph.App.Core.Graph;

namespace SocialNetworkGraph.App.Visualization
{
	public class GraphVisualizer
	{
		public Graph CreateMsaglGraph(CoreGraph ourGraph)
		{
			Graph msaglGraph = new Graph("socialNetwork");
			msaglGraph.Attr.LayerDirection = LayerDirection.LR; // Sol-sağ yerleşim
			msaglGraph.Attr.MinNodeHeight = 40;
			msaglGraph.Attr.MinNodeWidth = 40;

			// 1. Düğümleri Ekle (Daha büyük ve görsel)
			foreach (var node in ourGraph.Nodes)
			{
				var msaglNode = msaglGraph.AddNode(node.Id);
				msaglNode.LabelText = node.Name;
				msaglNode.Attr.Shape = Shape.Circle;
				msaglNode.Attr.FillColor = Microsoft.Msagl.Drawing.Color.LightBlue;
				msaglNode.Attr.LineWidth = 2;
				msaglNode.Label.FontSize = 12;
				msaglNode.Label.FontColor = Microsoft.Msagl.Drawing.Color.Black;
			}

			// 2. Kenarları Ekle (Ağırlıkları göster)
			foreach (var edge in ourGraph.Edges)
			{
				var msaglEdge = msaglGraph.AddEdge(edge.Source.Id, edge.Target.Id);
				
				// Ağırlığı 4 haneli göster
				msaglEdge.LabelText = edge.Weight.ToString("0.0000");
				msaglEdge.Label.FontSize = 9;
				msaglEdge.Label.FontColor = Microsoft.Msagl.Drawing.Color.DarkGray;
				
				// Yönsüz graf için ok yok
				msaglEdge.Attr.ArrowheadAtTarget = ArrowStyle.None;
				
				// Ağırlığa göre çizgi kalınlığı (yüksek ağırlık = kalın çizgi)
				msaglEdge.Attr.LineWidth = 1.0 + (edge.Weight * 2.0);
				
				// Ağırlığa göre renk (yüksek ağırlık = koyu, düşük = açık)
				if (edge.Weight > 0.7)
					msaglEdge.Attr.Color = Microsoft.Msagl.Drawing.Color.DarkGreen;
				else if (edge.Weight > 0.4)
					msaglEdge.Attr.Color = Microsoft.Msagl.Drawing.Color.DarkBlue;
				else
					msaglEdge.Attr.Color = Microsoft.Msagl.Drawing.Color.LightGray;
			}

			return msaglGraph;
		}
	}
}
