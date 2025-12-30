using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Data
{
    public class BackupItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public int NodeCount { get; set; }
        public int EdgeCount { get; set; }
        public string FilePath { get; set; }
    }

    internal class GraphDataModel
    {
        public List<Node> Nodes { get; set; }
        public List<EdgeDataModel> Edges { get; set; }
    }

    internal class EdgeDataModel
    {
        public string SourceId { get; set; }
        public string TargetId { get; set; }
        public double Weight { get; set; }
    }

    public class BackupManager
    {
        private readonly string _backupFolder;
        private const int MaxBackups = 30;

        public BackupManager()
        {
            // --- DEĞİŞİKLİK BURADA ---
            // Programın çalıştığı yerden (bin/Debug/net...) 3 klasör yukarı çıkarak
            // direkt senin proje ana klasörünü bulur ve oraya 'backups' klasörü açar.
            // Böylece klasörü masaüstünde nereye taşırsan taşı çalışır.

            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\.."));
            _backupFolder = Path.Combine(projectRoot, "backups");

            // Alternatif: Eğer SADECE ve SADECE senin bilgisayarda çalışacaksa direkt bunu da açabilirsin:
            // _backupFolder = @"C:\Users\sadkg\Desktop\SocialNetworkGraph.App\backups";

            // Klasör yoksa oluştur
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
        }

        public string SaveBackup(Graph graph, string backupName = null)
        {
            if (string.IsNullOrWhiteSpace(backupName) || backupName == "Yedek Adı...")
            {
                backupName = $"Yedek_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            CleanOldBackups();

            string backupId = Guid.NewGuid().ToString();

            var graphData = new GraphDataModel
            {
                Nodes = graph.Nodes,
                Edges = graph.Edges.Select(e => new EdgeDataModel
                {
                    SourceId = e.Source.Id,
                    TargetId = e.Target.Id,
                    Weight = e.Weight
                }).ToList()
            };

            string dataFile = Path.Combine(_backupFolder, $"{backupId}.json");
            var options = new JsonSerializerOptions { WriteIndented = true };
            string dataJson = JsonSerializer.Serialize(graphData, options);
            File.WriteAllText(dataFile, dataJson);

            var backupItem = new BackupItem
            {
                Id = backupId,
                Name = backupName,
                CreatedAt = DateTime.Now,
                NodeCount = graph.Nodes.Count,
                EdgeCount = graph.Edges.Count,
                FilePath = dataFile
            };

            string infoFile = Path.Combine(_backupFolder, $"{backupId}.info.json");
            string infoJson = JsonSerializer.Serialize(backupItem, options);
            File.WriteAllText(infoFile, infoJson);

            return backupId;
        }

        public List<BackupItem> GetAllBackups()
        {
            var backups = new List<BackupItem>();

            if (!Directory.Exists(_backupFolder)) return backups;

            foreach (var file in Directory.GetFiles(_backupFolder, "*.info.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var item = JsonSerializer.Deserialize<BackupItem>(json);
                    if (item != null)
                        backups.Add(item);
                }
                catch { }
            }

            return backups.OrderByDescending(b => b.CreatedAt).ToList();
        }

        public Graph LoadBackup(string backupId)
        {
            string fileName = Path.Combine(_backupFolder, $"{backupId}.json");

            if (!File.Exists(fileName)) return null;

            try
            {
                var json = File.ReadAllText(fileName);
                var data = JsonSerializer.Deserialize<GraphDataModel>(json);

                var graph = new Graph();

                foreach (var n in data.Nodes)
                {
                    var newNode = new Node(n.Id, n.Name, n.Activity, n.Interaction, n.ConnectionCount);
                    graph.AddNode(newNode);
                }

                foreach (var e in data.Edges)
                {
                    var s = graph.Nodes.FirstOrDefault(x => x.Id == e.SourceId);
                    var t = graph.Nodes.FirstOrDefault(x => x.Id == e.TargetId);
                    if (s != null && t != null)
                    {
                        graph.AddEdge(s, t, e.Weight);
                    }
                }

                return graph;
            }
            catch
            {
                return null;
            }
        }

        public bool DeleteBackup(string backupId)
        {
            try
            {
                string jsonFile = Path.Combine(_backupFolder, $"{backupId}.json");
                string infoFile = Path.Combine(_backupFolder, $"{backupId}.info.json");

                if (File.Exists(jsonFile)) File.Delete(jsonFile);
                if (File.Exists(infoFile)) File.Delete(infoFile);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private void CleanOldBackups()
        {
            var backups = GetAllBackups();
            if (backups.Count >= MaxBackups)
            {
                var toDelete = backups.Skip(MaxBackups - 1).ToList();
                foreach (var backup in toDelete)
                {
                    DeleteBackup(backup.Id);
                }
            }
        }

        public int GetBackupCount()
        {
            return GetAllBackups().Count;
        }
    }
}// Refactored by Sadik Gunay 
