using System;
using Microsoft.Msagl.Drawing;
using CoreGraph = SocialNetworkGraph.App.Core.Graph;
using MsaglColor = Microsoft.Msagl.Drawing.Color;

namespace SocialNetworkGraph.App.Visualization
{
    public class GraphVisualizer
    {
        // --- TASARIM PALETİ ---
        private static readonly MsaglColor NodeFillDefault = new MsaglColor(30, 41, 59);     // Koyu Mavi
        private static readonly MsaglColor NodeBorderDefault = new MsaglColor(56, 189, 248);   // Açık Mavi

        private static readonly MsaglColor NodeHubFill = new MsaglColor(124, 58, 237);       // Mor
        private static readonly MsaglColor NodeHubBorder = new MsaglColor(167, 139, 250);    // Açık Mor

        private static readonly MsaglColor TextColor = MsaglColor.White;

        // Bağlantı Renkleri
        private static readonly MsaglColor EdgeStrong = new MsaglColor(34, 211, 238);        // Cyan (Parlak)
        private static readonly MsaglColor EdgeMedium = new MsaglColor(148, 163, 184);       // Gri-Mavi
        private static readonly MsaglColor EdgeWeak = new MsaglColor(71, 85, 105);           // Koyu Gri

        public Graph CreateMsaglGraph(CoreGraph ourGraph)
        {
            var msaglGraph = new Graph("network");
            msaglGraph.Attr.BackgroundColor = MsaglColor.Transparent;

            // 1. Düğümleri Çiz
            foreach (var node in ourGraph.Nodes)
            {
                var n = msaglGraph.AddNode(node.Id);
                n.LabelText = node.Name;
                n.UserData = node;

                ApplyNodeStyle(n, node);
            }

            // 2. Bağlantıları Çiz
            foreach (var edge in ourGraph.Edges)
            {
                var e = msaglGraph.AddEdge(edge.Source.Id, edge.Target.Id);
                ApplyEdgeStyle(e, edge.Weight);
            }

            return msaglGraph;
        }

        private void ApplyNodeStyle(Node n, dynamic coreNode)
        {
            // Düğüm Boyutları
            double sizeFactor = 1.0 + (Math.Log10(coreNode.ConnectionCount + 1) * 0.6);

            n.Label.FontSize = (int)(7 * sizeFactor) + 4; // Yazılar daha büyük
            n.Attr.LineWidth = 2.5; // Düğüm çerçevesi daha kalın
            n.Label.FontColor = TextColor;

            // Herkes DAİRE (Kare yok)
            n.Attr.Shape = Shape.Circle;

            if (coreNode.ConnectionCount > 10)
            {
                n.Attr.FillColor = NodeHubFill;
                n.Attr.Color = NodeHubBorder;
            }
            else
            {
                n.Attr.FillColor = NodeFillDefault;
                n.Attr.Color = NodeBorderDefault;
            }
        }

        private void ApplyEdgeStyle(Edge e, double weight)
        {
            e.Attr.ArrowheadAtTarget = ArrowStyle.None;

            // --- CİDDİ KALINLAŞTIRMA YAPILDI ---
            // Artık en ince çizgi bile 2.0 birim. Tıklaması garanti.

            if (weight < 2.0)
            {
                // ÇOK GÜÇLÜ BAĞ (Kankalar)
                e.Attr.LineWidth = 5.0;      // Çok Kalın
                e.Attr.Color = EdgeStrong;
            }
            else if (weight < 15.0)
            {
                // ORTA BAĞ
                e.Attr.LineWidth = 3.5;      // Kalın
                e.Attr.Color = EdgeMedium;
            }
            else
            {
                // ZAYIF / UZAK BAĞ
                // Tıklanabilirlik için en az 2.0 yaptık
                e.Attr.LineWidth = 2.0;      // Standart Kalınlık
                e.Attr.Color = EdgeWeak;
            }
        }
    }
}
