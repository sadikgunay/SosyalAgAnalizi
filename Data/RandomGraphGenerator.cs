using System;
using System.Collections.Generic;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Data
{
    public class RandomGraphGenerator
    {
        private Random _rnd = new Random();

        /// <summary>
        /// TAM OTOMATİK MOD: 
        /// 5 ile 100 arasında rastgele sayıda kişi oluşturur.
        /// Bağlantı yoğunluğunu rastgele belirler.
        /// Özellikleri (Aktiflik, Etkileşim) tamamen rastgele dağıtır.
        /// </summary>
        public Graph GenerateRandomized()
        {
            // 1. Düğüm Sayısını Rastgele Seç (Boş gelmesin diye en az 5, en çok 100)
            int randomNodeCount = _rnd.Next(5, 101);

            // 2. Bağlantı Yoğunluğunu Rastgele Seç (%5 ile %25 arası)
            // Çok yoğun olursa çizim "yumak" olur, %25 ideal üst sınırdır.
            double randomDensity = 0.05 + (_rnd.NextDouble() * 0.20);

            return Generate(randomNodeCount, randomDensity);
        }

        /// <summary>
        /// Belirtilen parametrelere göre graf oluşturur.
        /// </summary>
        public Graph Generate(int nodeCount, double density)
        {
            Graph g = new Graph();

            // ---------------------------------------------------------
            // 1. ADIM: DÜĞÜMLERİ RASTGELE ÖZELLİKLERLE YARAT
            // ---------------------------------------------------------
            for (int i = 1; i <= nodeCount; i++)
            {
                string id = $"Kullanıcı_{i}";
                string name = id;

                // A) AKTİVİTE (0.0 - 1.0 Arası)
                // Math.Round ile 2 basamaklı yapıyoruz (örn: 0.74)
                double act = Math.Round(_rnd.NextDouble(), 2);

                // B) ETKİLEŞİM (1 - 500 Arası Geniş Skala)
                // Etkileşim farkları Ağırlık formülünü doğrudan etkiler. 
                // Geniş aralık veriyoruz ki Dijkstra ve A* farklı yollar seçebilsin.
                double inte = _rnd.Next(1, 500);

                // ConnectionCount başlangıçta 0'dır, bağlantılar kurulunca artacak.
                g.AddNode(new Node(id, name, act, inte, 0));
            }

            // ---------------------------------------------------------
            // 2. ADIM: RASTGELE BAĞLANTILAR KUR (YÖNSÜZ)
            // ---------------------------------------------------------
            var nodes = g.Nodes;

            for (int i = 0; i < nodes.Count; i++)
            {
                // j = i + 1 diyerek geriye dönük bağlamayı engelliyoruz (A-B varsa B-A yapma)
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    // Zar atıyoruz: Eğer şans, yoğunluk oranından küçükse bağla
                    if (_rnd.NextDouble() < density)
                    {
                        // Ağırlığı (Weight) şimdilik 0 veriyoruz.
                        // Aşağıda 'UpdateConnectionCounts' çağırınca formüle göre otomatik hesaplanacak.
                        g.AddEdge(nodes[i], nodes[j], 0);
                    }
                }
            }

            // ---------------------------------------------------------
            // 3. ADIM: SİSTEMİ GÜNCELLE VE AĞIRLIKLARI HESAPLA
            // ---------------------------------------------------------
            // Bu metod; kimin kaç bağlantısı olduğunu sayar VE 
            // Graph.cs içindeki formülü kullanarak kenar ağırlıklarını günceller.
            g.UpdateConnectionCounts();

            return g;
        }
    }
}