using System;
using System.Collections.Generic;
using System.Linq;

namespace SocialNetworkGraph.App.Core
{
    public class Graph
    {
        public List<Node> Nodes { get; private set; }
        public List<Edge> Edges { get; private set; }

        public Graph()
        {
            Nodes = new List<Node>();
            Edges = new List<Edge>();
        }

        // PROJE İSTERİ 4.3: Dinamik Ağırlık Hesaplama
        // Formül: 1 + Sqrt((Fark1)^2 + (Fark2)^2 + (Fark3)^2)
        public static double CalculateDynamicWeight(Node n1, Node n2)
        {
            // 1. Özellik Farklarının Karesi (Aktiflik, Etkileşim, Bağlantı Sayısı)
            double dAct = Math.Pow(n1.Activity - n2.Activity, 2);
            double dInt = Math.Pow(n1.Interaction - n2.Interaction, 2);
            double dConn = Math.Pow(n1.ConnectionCount - n2.ConnectionCount, 2);

            // 2. Öklid Benzeri Uzaklık Hesabı
            double distance = Math.Sqrt(dAct + dInt + dConn);

            // 3. Final Ağırlık (Benzer özellik = Düşük Maliyet, Farklı özellik = Yüksek Maliyet)
            // PDF'teki formül: 1 + Uzaklık
            return Math.Round(1.0 + distance, 4);
        }

        public void AddNode(Node node)
        {
            // Aynı ID'ye sahip düğüm eklenmesini engelle
            if (!Nodes.Any(n => n.Id == node.Id))
            {
                Nodes.Add(node);
            }
        }

        public void AddEdge(Node source, Node target, double weight = 0)
        {
            if (source == target) return; // Self-loop engelleme (Kendine bağ yasak)

            // PROJE İSTERİ 3.1: Yönsüz Bağlantı Kontrolü
            // A->B varsa B->A ekleme, çünkü aynı şeydir.
            bool exists = Edges.Any(e =>
                (e.Source.Id == source.Id && e.Target.Id == target.Id) ||
                (e.Source.Id == target.Id && e.Target.Id == source.Id));

            if (!exists)
            {
                // Eğer dışarıdan özel bir ağırlık verilmediyse (0 ise), formüle göre otomatik hesapla
                if (weight == 0)
                {
                    weight = CalculateDynamicWeight(source, target);
                }

                Edges.Add(new Edge(source, target, weight));

                // Bağlantı eklendiği için düğümlerin bağlantı sayılarını güncelle
                UpdateConnectionCounts();
            }
        }

        // ConnectionCount özelliğini, mevcut kenar sayısına göre otomatik güncelle
        public void UpdateConnectionCounts()
        {
            // 1. Önce herkesi sıfırla
            foreach (var node in Nodes)
            {
                node.ConnectionCount = 0;
            }

            // 2. Her kenar için uçlardaki düğümlerin sayacını artır
            foreach (var edge in Edges)
            {
                edge.Source.ConnectionCount++;
                edge.Target.ConnectionCount++;
            }

            // 3. Bağlantı sayıları değiştiği için ağırlıkları da güncellemek gerekir (Dinamik Yapı)
            // Çünkü formülde (ConnectionCount farkı) kullanılıyor.
            foreach (var edge in Edges)
            {
                edge.Weight = CalculateDynamicWeight(edge.Source, edge.Target);
            }
        }

        public void Clear()
        {
            Nodes.Clear();
            Edges.Clear();
        }
    }
}