using System;

namespace SocialNetworkGraph.App.Data
{
    public class PerformanceLog
    {
        public string AlgorithmName { get; set; }    // Algoritma adý
        public int NodeCount { get; set; }           // Ýþlem sýrasýndaki düðüm sayýsý
        public int EdgeCount { get; set; }           // Ýþlem sýrasýndaki kenar sayýsý
        public double ExecutionTimeMs { get; set; }  // Çalýþma süresi (milisaniye)
        public string Complexity { get; set; }       // Karmaþýklýk notu (O(n) vb.)
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}