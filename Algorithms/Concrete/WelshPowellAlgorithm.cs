using System.Collections.Generic;
using System.Linq;
using Microsoft.Msagl.Drawing;
using SocialNetworkGraph.App.Core;

// --- ÇAKIŞMA ÖNLEYİCİLER ---
using CoreGraph = SocialNetworkGraph.App.Core.Graph;
using CoreNode = SocialNetworkGraph.App.Core.Node;
using MsaglColor = Microsoft.Msagl.Drawing.Color;
// ---------------------------

namespace SocialNetworkGraph.App.Algorithms.Concrete
{
    public class WelshPowellAlgorithm
    {
        // İsterler gereği: Her bir ayrık topluluk için ayrı ayrı Welsh-Powell uygulanmalı
        public Dictionary<CoreNode, MsaglColor> Execute(CoreGraph graph)
        {
            var coloring = new Dictionary<CoreNode, MsaglColor>();
            
            // 1. Önce bağlı bileşenleri bul
            var components = new ConnectedComponentsAlgorithm().Execute(graph);
            
            // 2. Her bileşen için ayrı ayrı Welsh-Powell uygula
            var palette = new List<MsaglColor>
            {
                MsaglColor.Red, MsaglColor.Blue, MsaglColor.Green, MsaglColor.Yellow, MsaglColor.Orange,
                MsaglColor.Purple, MsaglColor.Cyan, MsaglColor.Magenta, MsaglColor.Brown, MsaglColor.Pink,
                MsaglColor.LightBlue, MsaglColor.LightGreen, MsaglColor.LightYellow, MsaglColor.LightPink
            };
            
            int globalColorOffset = 0;
            
            foreach (var component in components)
            {
                // Her bileşen için Welsh-Powell uygula
                var componentColoring = ColorComponent(graph, component, palette, globalColorOffset);
                
                // Sonuçları ana coloring'e ekle
                foreach (var item in componentColoring)
                {
                    coloring[item.Key] = item.Value;
                }
                
                // Her bileşen için farklı renk paleti kullanmak için offset artır
                globalColorOffset += componentColoring.Values.Distinct().Count();
            }
            
            return coloring;
        }
        
        // Bir bileşen için Welsh-Powell algoritması
        private Dictionary<CoreNode, MsaglColor> ColorComponent(CoreGraph graph, List<CoreNode> component, List<MsaglColor> palette, int colorOffset)
        {
            var coloring = new Dictionary<CoreNode, MsaglColor>();
            
            // Dereceye göre sırala (yüksek dereceli önce)
            var sortedNodes = component
                .OrderByDescending(n => graph.Edges.Count(e => e.Source == n || e.Target == n))
                .ToList();
            
            int colorIndex = colorOffset;
            
            while (sortedNodes.Count > 0)
            {
                var currentColor = palette[colorIndex % palette.Count];
                var coloredInThisRound = new List<CoreNode>();
                
                foreach (var node in sortedNodes)
                {
                    // Bu düğüm, bu turda renklendirilen düğümlerden herhangi biriyle komşu mu?
                    bool isNeighborToCurrentColor = coloredInThisRound.Any(coloredNode =>
                        IsConnected(graph, node, coloredNode));
                    
                    if (!isNeighborToCurrentColor)
                    {
                        coloring[node] = currentColor;
                        coloredInThisRound.Add(node);
                    }
                }
                
                // Bu turda renklendirilen düğümleri listeden çıkar
                foreach (var n in coloredInThisRound) 
                    sortedNodes.Remove(n);
                
                colorIndex++;
            }
            
            return coloring;
        }

        private bool IsConnected(CoreGraph graph, CoreNode n1, CoreNode n2)
        {
            return graph.Edges.Any(e => (e.Source == n1 && e.Target == n2) || (e.Source == n2 && e.Target == n1));
        }
    }
}
// Refactored by Atakan Cetli 
