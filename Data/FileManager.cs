using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Data
{
	public class FileManager
	{
		public Graph LoadGraph(string filePath)
		{
			Graph graph = new Graph();
			string[] lines = File.ReadAllLines(filePath);

			// Düğümleri geçici olarak tutacağımız sözlük
			Dictionary<string, Node> tempNodes = new Dictionary<string, Node>();

			// 1. ADIM: Düğümleri Yarat (Özellikleriyle Birlikte)
			foreach (string line in lines)
			{
				if (string.IsNullOrWhiteSpace(line)) continue;
				if (line.ToLower().StartsWith("dugumid") || line.ToLower().Contains("ozellik")) continue;

				string cleanLine = line.Replace("\"", "");
				string[] parts = cleanLine.Split(new char[] { ';' }); // Noktalı virgül ile ayır

				if (parts.Length >= 4)
				{
					string id = parts[0].Trim();
					double activity = ParseDouble(parts[1]);
					double interaction = ParseDouble(parts[2]);
					int connCount = (int)ParseDouble(parts[3]);

					// Node oluştur (Düğüm bu özellikleri içinde saklayacak)
					Node newNode = new Node(id, id, activity, interaction, connCount);

					if (!tempNodes.ContainsKey(id))
					{
						tempNodes.Add(id, newNode);
						graph.Nodes.Add(newNode);
					}
				}
			}

			// 2. ADIM: Kenarları Oluştur (Ağırlık hesabı OTOMATİK olacak)
			foreach (string line in lines)
			{
				if (string.IsNullOrWhiteSpace(line) || line.ToLower().StartsWith("dugumid")) continue;

				string cleanLine = line.Replace("\"", "");
				string[] parts = cleanLine.Split(new char[] { ';' });

				if (parts.Length >= 5)
				{
					string sourceId = parts[0].Trim();
					string neighborsStr = parts[4].Trim();

					// Komşuları virgül ile ayır
					string[] neighbors = neighborsStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

					if (tempNodes.ContainsKey(sourceId))
					{
						Node sourceNode = tempNodes[sourceId];

						foreach (string targetId in neighbors)
						{
							string tId = targetId.Trim();
							if (tempNodes.ContainsKey(tId))
							{
								Node targetNode = tempNodes[tId];

								Edge newEdge = new Edge(sourceNode, targetNode);

								// Duplicate kontrolü
								if (!graph.Edges.Any(e => 
									(e.Source.Id == sourceNode.Id && e.Target.Id == targetNode.Id) ||
									(e.Source.Id == targetNode.Id && e.Target.Id == sourceNode.Id)))
								{
									graph.Edges.Add(newEdge);
								}
							}
						}
					}
				}
			}

			return graph;
		}

		public void SaveGraph(Graph graph, string filePath)
		{
			// CSV Başlığı (Formatı bozmamak önemli)
			string csvContent = "DugumId;Aktiflik;Etkilesim;BaglantiSayisi;Komsular\n";

			foreach (var node in graph.Nodes)
			{
				// 1. Bu düğümün komşularını bul
				List<string> neighborNames = new List<string>();
				foreach (var edge in graph.Edges)
				{
					// Eğer kaynak bizsek, hedef komşudur
					if (edge.Source.Id == node.Id) neighborNames.Add(edge.Target.Id);
					// Yönsüz olduğu için hedef bizsek, kaynak komşudur
					else if (edge.Target.Id == node.Id) neighborNames.Add(edge.Source.Id);
				}

				// Komşuları virgülle birleştir (örn: "Ali,Veli")
				string neighborsStr = string.Join(",", neighborNames);

				// 2. Satırı oluştur (Noktalı virgül ile ayırarak)
				csvContent += $"{node.Id};{node.Activity};{node.Interaction};{node.ConnectionCount};{neighborsStr}\n";
			}

			// 3. Dosyaya yaz (üzerine yazar)
			File.WriteAllText(filePath, csvContent);
		}

		// JSON Yükleme
		public Graph LoadGraphFromJson(string filePath)
		{
			string jsonContent = File.ReadAllText(filePath);
			var jsonData = JsonSerializer.Deserialize<JsonGraphData>(jsonContent);

			Graph graph = new Graph();
			Dictionary<string, Node> nodeMap = new Dictionary<string, Node>();

			// Düğümleri oluştur
			foreach (var nodeData in jsonData.Nodes)
			{
				Node node = new Node(nodeData.Id, nodeData.Name, nodeData.Activity, nodeData.Interaction, nodeData.ConnectionCount);
				nodeMap[nodeData.Id] = node;
				graph.Nodes.Add(node);
			}

			// Kenarları oluştur
			foreach (var edgeData in jsonData.Edges)
			{
				if (nodeMap.ContainsKey(edgeData.SourceId) && nodeMap.ContainsKey(edgeData.TargetId))
				{
					Node source = nodeMap[edgeData.SourceId];
					Node target = nodeMap[edgeData.TargetId];

					// Duplicate kontrolü
					if (!graph.Edges.Any(e =>
						(e.Source.Id == source.Id && e.Target.Id == target.Id) ||
						(e.Source.Id == target.Id && e.Target.Id == source.Id)))
					{
						graph.Edges.Add(new Edge(source, target));
					}
				}
			}

			return graph;
		}

		// JSON Kaydetme
		public void SaveGraphToJson(Graph graph, string filePath)
		{
			var jsonData = new JsonGraphData
			{
				Nodes = graph.Nodes.Select(n => new JsonNodeData
				{
					Id = n.Id,
					Name = n.Name,
					Activity = n.Activity,
					Interaction = n.Interaction,
					ConnectionCount = n.ConnectionCount
				}).ToList(),
				Edges = graph.Edges.Select(e => new JsonEdgeData
				{
					SourceId = e.Source.Id,
					TargetId = e.Target.Id
				}).ToList()
			};

			var options = new JsonSerializerOptions { WriteIndented = true };
			string jsonContent = JsonSerializer.Serialize(jsonData, options);
			File.WriteAllText(filePath, jsonContent);
		}

		private double ParseDouble(string val)
		{
			val = val.Trim().Replace(".", ",");
			if (double.TryParse(val, out double result)) return result;
			return 0;
		}

		// JSON Serileştirme için yardımcı sınıflar
		private class JsonGraphData
		{
			public List<JsonNodeData> Nodes { get; set; }
			public List<JsonEdgeData> Edges { get; set; }
		}

		private class JsonNodeData
		{
			public string Id { get; set; }
			public string Name { get; set; }
			public double Activity { get; set; }
			public double Interaction { get; set; }
			public int ConnectionCount { get; set; }
		}

		private class JsonEdgeData
		{
			public string SourceId { get; set; }
			public string TargetId { get; set; }
		}
	}
}
