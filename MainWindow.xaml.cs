using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SocialNetworkGraph.App.Data;
using SocialNetworkGraph.App.Visualization;
using SocialNetworkGraph.App.Core;
using SocialNetworkGraph.App.Algorithms.Concrete;
using SocialNetworkGraph.App.Algorithms.Interfaces;
using Microsoft.Msagl.WpfGraphControl;
using System.Diagnostics; // Kronometre (Stopwatch) için gerekli

// ÇAKIŞMA ÖNLEYİCİ: "MessageBox" dediğimizde WPF olanı anlayacak.
using WpfMsgBox = System.Windows.MessageBox;

namespace SocialNetworkGraph.App
{
	public partial class MainWindow : Window
	{
		private Core.Graph _myGraph;
		private AutomaticGraphLayoutControl _viewer;

		public MainWindow()
		{
			InitializeComponent();
			_myGraph = new Core.Graph();
			SetupViewer();
			UpdateInfoPanel("🚀 Hoş geldiniz! Hızlı Başlangıç butonuna tıklayarak örnek veri ile başlayabilirsiniz.");
		}

		// Hızlı Başlangıç - Örnek Veri Oluştur
		private void BtnQuickStart_Click(object sender, RoutedEventArgs e)
		{
			_myGraph = new Core.Graph();

			// Gerçek hayat senaryosu: Sosyal medya ağı
			var users = new[]
			{
				new Node("Ali", "Ali Yılmaz", 0.9, 150, 5),
				new Node("Ayşe", "Ayşe Demir", 0.8, 120, 4),
				new Node("Mehmet", "Mehmet Kaya", 0.7, 80, 3),
				new Node("Zeynep", "Zeynep Şahin", 0.85, 100, 4),
				new Node("Can", "Can Öztürk", 0.6, 60, 2),
				new Node("Elif", "Elif Arslan", 0.75, 90, 3),
				new Node("Burak", "Burak Çelik", 0.65, 70, 2),
				new Node("Selin", "Selin Yıldız", 0.8, 110, 4)
			};

			foreach (var user in users)
			{
				_myGraph.Nodes.Add(user);
			}

			// Bağlantılar oluştur
			_myGraph.AddEdge(users[0], users[1]); // Ali - Ayşe
			_myGraph.AddEdge(users[0], users[2]); // Ali - Mehmet
			_myGraph.AddEdge(users[0], users[3]); // Ali - Zeynep
			_myGraph.AddEdge(users[1], users[3]); // Ayşe - Zeynep
			_myGraph.AddEdge(users[1], users[4]); // Ayşe - Can
			_myGraph.AddEdge(users[2], users[5]); // Mehmet - Elif
			_myGraph.AddEdge(users[3], users[5]); // Zeynep - Elif
			_myGraph.AddEdge(users[3], users[6]); // Zeynep - Burak
			_myGraph.AddEdge(users[5], users[7]); // Elif - Selin
			_myGraph.AddEdge(users[6], users[7]); // Burak - Selin

			RefreshGraphDisplay();
			ShowSuccessMessage("Hızlı Başlangıç", 
				$"✅ Örnek sosyal ağ oluşturuldu!\n\n" +
				$"📊 {_myGraph.Nodes.Count} kullanıcı\n" +
				$"🔗 {_myGraph.Edges.Count} bağlantı\n\n" +
				$"💡 Şimdi algoritmaları test edebilirsiniz!");
		}

		private void SetupViewer()
		{
			_viewer = new AutomaticGraphLayoutControl();

			// TIKLAMA OLAYINI BAĞLIYORUZ
			_viewer.MouseDown += Viewer_MouseDown;

			if (GraphContainer != null)
			{
				GraphContainer.Child = _viewer;
			}
		}

		// Tıklama Olayı Fonksiyonu
		// Tıklama Olayı Fonksiyonu (DÜZELTİLMİŞ VERSİYON)
		private void Viewer_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// WPF Mantığı: Tıklanan görsel parçayı (FrameworkElement) yakala
			if (e.OriginalSource is FrameworkElement clickedElement)
			{
				// MSAGL, çizdiği balonların "Tag" özelliğine asıl Node nesnesini saklar.
				// Tıklanan şeyin bir Node olup olmadığını kontrol ediyoruz:
				if (clickedElement.Tag is Microsoft.Msagl.Drawing.Node msaglNode)
				{
					// Bizim kendi veritabanımızdan (Core.Node) bu kişiyi bulalım
					var myNode = _myGraph.Nodes.Find(n => n.Id == msaglNode.Id);

					if (myNode != null)
					{
						string info = $"👤 KİŞİ BİLGİLERİ\n\n" +
									  $"Ad: {myNode.Name}\n" +
									  $"Aktiflik Puanı: {myNode.Activity}\n" +
									  $"Etkileşim Puanı: {myNode.Interaction}\n" +
									  $"Bağlantı Özelliği: {myNode.ConnectionCount}\n\n" +
									  $"Güncellemek için 'Bilgi Güncelleme' panelini kullanabilirsiniz.";

						// Mesaj göster
						WpfMsgBox.Show(info, "Seçilen Kişi");

						// Bilgileri düğüm ekleme kutularına otomatik doldur (Kolaylık olsun)
						if (TxtAddNodeName != null)
						{
							TxtAddNodeName.Text = myNode.Name;
							TxtAddNodeAct.Text = myNode.Activity.ToString();
							TxtAddNodeInt.Text = myNode.Interaction.ToString();
							TxtAddNodeConn.Text = myNode.ConnectionCount.ToString();
						}
					}
				}
			}
		}

		// --- ALGORİTMA: A* (A-Star) ---
		private void BtnRunAStar_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TxtSource.Text) || string.IsNullOrEmpty(TxtTarget.Text))
			{
				ShowWarningMessage("Eksik Bilgi", "A* algoritması için hem Kaynak hem Hedef kişi girilmelidir.");
				return;
			}

			Node startNode = GetOrCreateNode(TxtSource.Text.Trim());
			Node endNode = GetOrCreateNode(TxtTarget.Text.Trim());

			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			IGraphAlgorithm aStar = new AStarAlgorithm();
			List<Node> path = aStar.Execute(_myGraph, startNode, endNode);

			// SAYAÇ DURDUR
			sw.Stop();

			if (path.Count == 0)
			{
				ShowErrorMessage("A* Algoritması", "Kaynak ve hedef arasında yol bulunamadı!");
				return;
			}

			HighlightNodes(path, Microsoft.Msagl.Drawing.Color.Purple);

			// Toplam ağırlık hesapla
			double totalWeight = 0;
			for (int i = 0; i < path.Count - 1; i++)
			{
				var edge = _myGraph.Edges.FirstOrDefault(e =>
					(e.Source.Id == path[i].Id && e.Target.Id == path[i + 1].Id) ||
					(e.Source.Id == path[i + 1].Id && e.Target.Id == path[i].Id));
				if (edge != null) totalWeight += edge.Weight;
			}

			string result = $"⭐ A* Algoritması Sonuçları:\n\n" +
						   $"📍 Yol: {string.Join(" → ", path.Select(n => n.Name))}\n" +
						   $"📏 Adım Sayısı: {path.Count - 1}\n" +
						   $"⚖️ Toplam Ağırlık: {totalWeight:F4}\n" +
						   $"⏱️ Çalışma Süresi: {sw.ElapsedMilliseconds} ms ({sw.ElapsedTicks} ticks)";

			ShowSuccessMessage("A* Algoritması", result);
		}

		// --- ALGORİTMA: Bağlı Bileşenler (Topluluk Bulma) ---
		private void BtnComponents_Click(object sender, RoutedEventArgs e)
		{
			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			var algorithm = new ConnectedComponentsAlgorithm();
			var components = algorithm.Execute(_myGraph);

			// SAYAÇ DURDUR
			sw.Stop();

			RefreshGraphDisplay();

			Random rnd = new Random();
			string stats = $"TOPLAM {components.Count} ADET AYRIK TOPLULUK BULUNDU:\n\n";
			int count = 1;

			foreach (var component in components)
			{
				var randomColor = new Microsoft.Msagl.Drawing.Color(
					(byte)rnd.Next(50, 200), (byte)rnd.Next(50, 200), (byte)rnd.Next(50, 200));

				stats += $"{count}. Grup: {component.Count} Kişi\n";

				foreach (var node in component)
				{
					var msaglNode = _viewer.Graph.FindNode(node.Id);
					if (msaglNode != null)
					{
						msaglNode.Attr.FillColor = randomColor;
						msaglNode.Attr.LineWidth = 2;
					}
				}
				count++;
			}

			_viewer.Graph = _viewer.Graph;
			
			string fullStats = $"🧩 Bağlı Bileşenler Analizi:\n\n" + stats + 
							   $"\n⏱️ Hesaplama Süresi: {sw.ElapsedMilliseconds} ms\n" +
							   $"📊 Toplam Düğüm Sayısı: {_myGraph.Nodes.Count}";
			
			ShowSuccessMessage("Topluluk Analizi", fullStats);
		}

		private void RefreshGraphDisplay()
		{
			if (_myGraph == null) return;

			GraphVisualizer visualizer = new GraphVisualizer();
			var msaglGraph = visualizer.CreateMsaglGraph(_myGraph);
			_viewer.Graph = msaglGraph;

			if (TxtStats != null)
			{
				TxtStats.Text = $"📊 Düğüm: {_myGraph.Nodes.Count} | Kenar: {_myGraph.Edges.Count}";
			}

			UpdateInfoPanel();
		}

		// Bilgi Paneli Güncelleme
		private void UpdateInfoPanel(string customMessage = null)
		{
			if (TxtInfoPanel == null) return;

			if (!string.IsNullOrEmpty(customMessage))
			{
				TxtInfoPanel.Text = customMessage;
				return;
			}

			if (_myGraph == null || _myGraph.Nodes.Count == 0)
			{
				TxtInfoPanel.Text = "ℹ️ Bilgi: Graf boş. CSV/JSON yükleyin veya düğüm ekleyin.";
				return;
			}

			// Ortalama değerleri hesapla
			double avgActivity = _myGraph.Nodes.Average(n => n.Activity);
			double avgInteraction = _myGraph.Nodes.Average(n => n.Interaction);
			int totalConnections = _myGraph.Edges.Count;

			TxtInfoPanel.Text = $"📊 Graf İstatistikleri:\n" +
								$"   • Toplam Düğüm: {_myGraph.Nodes.Count}\n" +
								$"   • Toplam Bağlantı: {totalConnections}\n" +
								$"   • Ortalama Aktiflik: {avgActivity:F2}\n" +
								$"   • Ortalama Etkileşim: {avgInteraction:F2}\n" +
								$"💡 İpucu: Düğümlere tıklayarak detaylı bilgi görebilirsiniz.";
		}

		// Geliştirilmiş Mesaj Gösterme
		private void ShowSuccessMessage(string title, string message)
		{
			WpfMsgBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
			UpdateInfoPanel($"✅ {title}: {message}");
		}

		private void ShowErrorMessage(string title, string message)
		{
			WpfMsgBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
			UpdateInfoPanel($"❌ {title}: {message}");
		}

		private void ShowWarningMessage(string title, string message)
		{
			WpfMsgBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
			UpdateInfoPanel($"⚠️ {title}: {message}");
		}

		// --- DOSYA YÜKLEME ---
		private void BtnLoadGraph_Click(object sender, RoutedEventArgs e)
		{
			FileManager fileManager = new FileManager();
			string path = "social_network.csv";

			if (!System.IO.File.Exists(path))
			{
				ShowErrorMessage("Dosya Bulunamadı", $"'{path}' dosyası bulunamadı!\n\nLütfen dosyanın proje klasöründe olduğundan emin olun.");
				return;
			}

			try
			{
				_myGraph = fileManager.LoadGraph(path);
				RefreshGraphDisplay();
				ShowSuccessMessage("Dosya Yüklendi", $"CSV dosyası başarıyla yüklendi!\n\nYüklenen: {_myGraph.Nodes.Count} düğüm, {_myGraph.Edges.Count} bağlantı");
			}
			catch (Exception ex)
			{
				ShowErrorMessage("Yükleme Hatası", $"Dosya yüklenirken hata oluştu:\n{ex.Message}");
			}
		}

		// --- EKLEME: Yeni Bağlantı ---
		// --- EKLEME: Yeni Bağlantı (Self-Loop Korumalı) ---
		private void BtnAddEdge_Click(object sender, RoutedEventArgs e)
		{
			if (TxtSource == null || TxtTarget == null) return;

			string sourceName = TxtSource.Text.Trim();
			string targetName = TxtTarget.Text.Trim();

			if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetName))
			{
				ShowWarningMessage("Eksik Bilgi", "Lütfen Kaynak ve Hedef isimlerini girin.");
				return;
			}

			// İSTER: Self-Loop (Kendine Bağlantı) Engelleme
			if (sourceName.ToLower() == targetName.ToLower())
			{
				ShowErrorMessage("Geçersiz İşlem", "Bir kişi kendine bağlanamaz (Self-Loop yasak)!");
				return;
			}

			Node sourceNode = GetOrCreateNode(sourceName);
			Node targetNode = GetOrCreateNode(targetName);

			// Daha önce böyle bir kenar var mı kontrolü (Duplicate Engelleme)
			bool exists = _myGraph.Edges.Exists(edge =>
				(edge.Source == sourceNode && edge.Target == targetNode) ||
				(edge.Source == targetNode && edge.Target == sourceNode));

			if (exists)
			{
				ShowWarningMessage("Bağlantı Mevcut", "Bu bağlantı zaten mevcut!");
				return;
			}

			Edge newEdge = new Edge(sourceNode, targetNode);
			_myGraph.Edges.Add(newEdge);

			RefreshGraphDisplay();
			AutoSave();

			ShowSuccessMessage("Bağlantı Eklendi", $"{sourceNode.Name} ↔ {targetNode.Name} bağlantısı oluşturuldu!\nAğırlık: {newEdge.Weight:F4}");

			TxtSource.Text = "";
			TxtTarget.Text = "";
		}

		// --- SİLME: Düğüm ---
		private void BtnDeleteNode_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TxtAddNodeName.Text)) 
			{
				ShowWarningMessage("Eksik Bilgi", "Lütfen silinecek düğümün ismini girin.");
				return;
			}

			string nodeName = TxtAddNodeName.Text.Trim();
			Node nodeToRemove = _myGraph.Nodes.Find(n => n.Id.ToLower() == nodeName.ToLower());

			if (nodeToRemove == null)
			{
				ShowErrorMessage("Bulunamadı", $"'{nodeName}' isimli düğüm bulunamadı!");
				return;
			}

			// Bağlantıları sil
			for (int i = _myGraph.Edges.Count - 1; i >= 0; i--)
			{
				var edge = _myGraph.Edges[i];
				if (edge.Source.Id == nodeToRemove.Id || edge.Target.Id == nodeToRemove.Id)
				{
					_myGraph.Edges.RemoveAt(i);
				}
			}

			_myGraph.Nodes.Remove(nodeToRemove);
			RefreshGraphDisplay();
			AutoSave();
			ShowSuccessMessage("Silindi", $"{nodeName} ve tüm bağlantıları silindi.");
			TxtAddNodeName.Text = "";
		}

		// --- SİLME: Kenar ---
		private void BtnDeleteEdge_Click(object sender, RoutedEventArgs e)
		{
			string sourceName = TxtSource.Text.Trim();
			string targetName = TxtTarget.Text.Trim();

			if (string.IsNullOrEmpty(sourceName) || string.IsNullOrEmpty(targetName))
			{
				ShowWarningMessage("Eksik Bilgi", "Bağlantıyı silmek için Kaynak ve Hedef kutularını doldurun.");
				return;
			}

			var edgeToRemove = _myGraph.Edges.Find(edge =>
				edge.Source.Id.ToLower() == sourceName.ToLower() &&
				edge.Target.Id.ToLower() == targetName.ToLower());

			if (edgeToRemove == null)
			{
				edgeToRemove = _myGraph.Edges.Find(edge =>
					edge.Source.Id.ToLower() == targetName.ToLower() &&
					edge.Target.Id.ToLower() == sourceName.ToLower());
			}

			if (edgeToRemove != null)
			{
				_myGraph.Edges.Remove(edgeToRemove);
				RefreshGraphDisplay();
				AutoSave();
				ShowSuccessMessage("Bağlantı Silindi", $"{sourceName} ↔ {targetName} bağlantısı koparıldı.");
				TxtSource.Text = "";
				TxtTarget.Text = "";
			}
			else
			{
				ShowErrorMessage("Bulunamadı", "Böyle bir bağlantı bulunamadı.");
			}
		}

		// --- TEMİZLEME ---
		private void BtnClear_Click(object sender, RoutedEventArgs e)
		{
			_myGraph = new Core.Graph();
			RefreshGraphDisplay();
		}

		// --- ALGORİTMA: BFS ---
		private void BtnRunBFS_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TxtSource.Text))
			{
				WpfMsgBox.Show("Lütfen 'Kaynak Kişi' kutusuna bir isim yazın.");
				return;
			}

			string startName = TxtSource.Text.Trim();
			Node startNode = GetOrCreateNode(startName);

			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			IGraphAlgorithm bfs = new BFSAlgorithm();
			List<Node> visitedNodes = bfs.Execute(_myGraph, startNode);

			// SAYAÇ DURDUR
			sw.Stop();

			HighlightNodes(visitedNodes, Microsoft.Msagl.Drawing.Color.LightGreen);

			string result = $"🔍 BFS (Breadth-First Search) Sonuçları:\n\n" +
						   $"📍 Başlangıç Düğümü: {startNode.Name}\n" +
						   $"👥 Erişilen Düğüm Sayısı: {visitedNodes.Count}\n" +
						   $"📊 Toplam Düğüm: {_myGraph.Nodes.Count}\n" +
						   $"📈 Erişim Oranı: {(visitedNodes.Count * 100.0 / _myGraph.Nodes.Count):F1}%\n" +
						   $"⏱️ Çalışma Süresi: {sw.ElapsedMilliseconds} ms\n\n" +
						   $"🔗 Erişilen Düğümler: {string.Join(", ", visitedNodes.Select(n => n.Name))}";

			ShowSuccessMessage("BFS Algoritması", result);
		}

		// --- ALGORİTMA: DFS (Performans Ölçümlü) ---
		private void BtnRunDFS_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TxtSource.Text))
			{
				WpfMsgBox.Show("Lütfen 'Kaynak Kişi' kutusuna bir isim yazın.");
				return;
			}
			string startName = TxtSource.Text.Trim();

			Node startNode = _myGraph.Nodes.Find(n => n.Id.ToLower() == startName.ToLower());
			if (startNode == null)
			{
				ShowErrorMessage("Bulunamadı", $"'{startName}' isimli kişi grafikte bulunamadı!\n\nLütfen önce bu kişiyi ekleyin.");
				return;
			}

			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			IGraphAlgorithm dfs = new DFSAlgorithm();
			List<Node> visitedNodes = dfs.Execute(_myGraph, startNode);

			// SAYAÇ DURDUR
			sw.Stop();

			HighlightNodes(visitedNodes, Microsoft.Msagl.Drawing.Color.Orange);

			string result = $"🕵️ DFS (Depth-First Search) Sonuçları:\n\n" +
						   $"📍 Başlangıç Düğümü: {startNode.Name}\n" +
						   $"👥 Erişilen Düğüm Sayısı: {visitedNodes.Count}\n" +
						   $"📊 Toplam Düğüm: {_myGraph.Nodes.Count}\n" +
						   $"📈 Erişim Oranı: {(visitedNodes.Count * 100.0 / _myGraph.Nodes.Count):F1}%\n" +
						   $"⏱️ Çalışma Süresi: {sw.ElapsedMilliseconds} ms\n\n" +
						   $"🔗 Erişilen Düğümler: {string.Join(", ", visitedNodes.Select(n => n.Name))}";

			ShowSuccessMessage("DFS Algoritması", result);
		}

		// --- ALGORİTMA: Dijkstra ---
		private void BtnRunDijkstra_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TxtSource.Text) || string.IsNullOrEmpty(TxtTarget.Text))
			{
				ShowWarningMessage("Eksik Bilgi", "Dijkstra algoritması için hem Kaynak hem Hedef kişi girilmelidir.");
				return;
			}

			Node startNode = GetOrCreateNode(TxtSource.Text.Trim());
			Node endNode = GetOrCreateNode(TxtTarget.Text.Trim());
			
			if (startNode == null || endNode == null)
			{
				ShowErrorMessage("Hata", "Düğüm bulunamadı veya oluşturulamadı!");
				return;
			}

			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			IGraphAlgorithm dijkstra = new DijkstraAlgorithm();
			List<Node> path = dijkstra.Execute(_myGraph, startNode, endNode);

			// SAYAÇ DURDUR
			sw.Stop();

			if (path.Count == 0)
			{
				ShowErrorMessage("Dijkstra Algoritması", "Kaynak ve hedef arasında yol bulunamadı!");
				return;
			}

			HighlightNodes(path, Microsoft.Msagl.Drawing.Color.Red);

			// Toplam ağırlık hesapla
			double totalWeight = 0;
			for (int i = 0; i < path.Count - 1; i++)
			{
				var edge = _myGraph.Edges.FirstOrDefault(e =>
					(e.Source.Id == path[i].Id && e.Target.Id == path[i + 1].Id) ||
					(e.Source.Id == path[i + 1].Id && e.Target.Id == path[i].Id));
				if (edge != null) totalWeight += edge.Weight;
			}

			string result = $"🚀 Dijkstra Algoritması Sonuçları:\n\n" +
						   $"📍 Yol: {string.Join(" → ", path.Select(n => n.Name))}\n" +
						   $"📏 Adım Sayısı: {path.Count - 1}\n" +
						   $"⚖️ Toplam Ağırlık (Maliyet): {totalWeight:F4}\n" +
						   $"⏱️ Çalışma Süresi: {sw.ElapsedMilliseconds} ms";

			ShowSuccessMessage("Dijkstra Algoritması", result);
		}

		// --- ALGORİTMA: Welsh-Powell (Renklendirme) ---
		private void BtnWelshPowell_Click(object sender, RoutedEventArgs e)
		{
			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			WelshPowellAlgorithm algorithm = new WelshPowellAlgorithm();
			var colorMap = algorithm.Execute(_myGraph);

			// SAYAÇ DURDUR
			sw.Stop();

			RefreshGraphDisplay();

			foreach (var kvp in colorMap)
			{
				var msaglNode = _viewer.Graph.FindNode(kvp.Key);
				if (msaglNode != null)
				{
					msaglNode.Attr.FillColor = kvp.Value;
					msaglNode.Label.FontColor = Microsoft.Msagl.Drawing.Color.White;
				}
			}
			_viewer.Graph = _viewer.Graph;

			string stats = "BOYAMA SONUCU:\n";
			// Renk gruplarını say
			var groups = new Dictionary<string, int>();
			foreach (var c in colorMap.Values)
			{
				string cName = c.ToString();
				if (!groups.ContainsKey(cName)) groups[cName] = 0;
				groups[cName]++;
			}
			foreach (var g in groups) stats += $"{g.Key}: {g.Value} Kişi\n";

			string fullStats = $"🎨 Welsh-Powell Renklendirme Sonuçları:\n\n" + stats + 
							   $"\n⏱️ Algoritma Süresi: {sw.ElapsedMilliseconds} ms\n" +
							   $"📊 Toplam Renk Sayısı: {groups.Count}";
			
			ShowSuccessMessage("Welsh-Powell Renklendirme", fullStats);
		}

		// --- ANALİZ: Degree Centrality ---
		private void BtnAnalysis_Click(object sender, RoutedEventArgs e)
		{
			if (_myGraph.Nodes.Count == 0)
			{
				ShowWarningMessage("Boş Graf", "Grafik boş. Lütfen önce düğüm ekleyin veya dosya yükleyin.");
				return;
			}

			// SAYAÇ BAŞLAT
			Stopwatch sw = new Stopwatch();
			sw.Start();

			var algorithm = new DegreeCentralityAlgorithm();
			var topNodes = algorithm.Execute(_myGraph, 5);

			// SAYAÇ DURDUR
			sw.Stop();

			string report = "🏆 EN POPÜLER 5 KİŞİ\n-------------------\n";
			foreach (var item in topNodes)
			{
				report += $"{item.Key.Name} -> {item.Value} Bağlantı\n";

				var msaglNode = _viewer.Graph.FindNode(item.Key.Id);
				if (msaglNode != null) msaglNode.Attr.LineWidth = 4;
			}
			_viewer.Graph = _viewer.Graph;

			string fullReport = $"📊 Merkezilik (Degree Centrality) Analizi:\n\n" + report + 
								 $"\n⏱️ Analiz Süresi: {sw.ElapsedMilliseconds} ms\n" +
								 $"💡 Not: En yüksek dereceli düğümler en etkili kullanıcılardır.";
			
			ShowSuccessMessage("Merkezilik Analizi", fullReport);
		}


		private void BtnExport_Click(object sender, RoutedEventArgs e)
		{
			try
			{
				FileManager fm = new FileManager();
				fm.SaveGraph(_myGraph, "saved_graph.csv");
				ShowSuccessMessage("Kaydedildi", $"Grafik 'saved_graph.csv' olarak kaydedildi!\n\nKaydedilen: {_myGraph.Nodes.Count} düğüm, {_myGraph.Edges.Count} bağlantı");
			}
			catch (Exception ex)
			{
				WpfMsgBox.Show("Kaydetme hatası: " + ex.Message);
			}
		}

		// JSON Yükleme
		private void BtnLoadJson_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.OpenFileDialog
			{
				Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
				Title = "JSON Dosyası Seç"
			};

			if (dialog.ShowDialog() == true)
			{
				try
				{
					FileManager fileManager = new FileManager();
					_myGraph = fileManager.LoadGraphFromJson(dialog.FileName);
					RefreshGraphDisplay();
					WpfMsgBox.Show("JSON dosyası başarıyla yüklendi!");
				}
				catch (Exception ex)
				{
					ShowErrorMessage("Yükleme Hatası", $"JSON dosyası yüklenirken hata oluştu:\n{ex.Message}");
				}
			}
		}

		// JSON Kaydetme
		private void BtnExportJson_Click(object sender, RoutedEventArgs e)
		{
			var dialog = new Microsoft.Win32.SaveFileDialog
			{
				Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
				Title = "JSON Olarak Kaydet",
				FileName = "graph.json"
			};

			if (dialog.ShowDialog() == true)
			{
				try
				{
					FileManager fm = new FileManager();
					fm.SaveGraphToJson(_myGraph, dialog.FileName);
					ShowSuccessMessage("Kaydedildi", $"Grafik '{dialog.FileName}' olarak kaydedildi!\n\nKaydedilen: {_myGraph.Nodes.Count} düğüm, {_myGraph.Edges.Count} bağlantı");
				}
				catch (Exception ex)
				{
					WpfMsgBox.Show("Kaydetme hatası: " + ex.Message);
				}
			}
		}

		// Düğüm Ekleme
		private void BtnAddNode_Click(object sender, RoutedEventArgs e)
		{
			if (string.IsNullOrEmpty(TxtAddNodeName.Text.Trim()))
			{
				ShowWarningMessage("Eksik Bilgi", "Lütfen bir isim girin.");
				return;
			}

			string nodeName = TxtAddNodeName.Text.Trim();

			// Aynı isimde düğüm var mı kontrol et
			if (_myGraph.Nodes.Any(n => n.Id.ToLower() == nodeName.ToLower()))
			{
				ShowWarningMessage("Düğüm Mevcut", "Bu isimde bir kişi zaten mevcut!");
				return;
			}

			try
			{
				double activity = string.IsNullOrEmpty(TxtAddNodeAct.Text) ? 0.5 : double.Parse(TxtAddNodeAct.Text.Replace(".", ","));
				double interaction = string.IsNullOrEmpty(TxtAddNodeInt.Text) ? 5 : double.Parse(TxtAddNodeInt.Text.Replace(".", ","));
				int connCount = string.IsNullOrEmpty(TxtAddNodeConn.Text) ? 0 : int.Parse(TxtAddNodeConn.Text);

				Node newNode = new Node(nodeName, nodeName, activity, interaction, connCount);
				_myGraph.Nodes.Add(newNode);

				RefreshGraphDisplay();
				AutoSave();

				ShowSuccessMessage("Düğüm Eklendi", $"{nodeName} başarıyla eklendi!\n\nÖzellikler:\n• Aktiflik: {activity}\n• Etkileşim: {interaction}\n• Bağlantı Sayısı: {connCount}");
				
				// Formu temizle
				TxtAddNodeName.Text = "";
				TxtAddNodeAct.Text = "";
				TxtAddNodeInt.Text = "";
				TxtAddNodeConn.Text = "";
			}
			catch
			{
				WpfMsgBox.Show("Lütfen sayısal değerleri doğru giriniz.");
			}
		}

		private void BtnMatrix_Click(object sender, RoutedEventArgs e)
		{
			string matrix = "KOMŞULUK MATRİSİ\n\n";
			var nodes = _myGraph.Nodes;

			// Başlık satırı
			matrix += "       ";
			foreach (var n in nodes) matrix += $"{n.Name.Substring(0, Math.Min(3, n.Name.Length))}  ";
			matrix += "\n";

			// Matris satırları
			foreach (var rowNode in nodes)
			{
				matrix += $"{rowNode.Name.Substring(0, Math.Min(5, rowNode.Name.Length)),-5} |";

				foreach (var colNode in nodes)
				{
					// Bağlantı var mı?
					bool connected = _myGraph.Edges.Exists(edge =>
						(edge.Source == rowNode && edge.Target == colNode) ||
						(edge.Source == colNode && edge.Target == rowNode));

					matrix += (connected ? " 1   " : " 0   ");
				}
				matrix += "\n";
			}

			WpfMsgBox.Show(matrix, "Matris Görünümü");
		}

		private void BtnUpdateNode_Click(object sender, RoutedEventArgs e)
		{
			string name = TxtAddNodeName.Text.Trim();
			if (string.IsNullOrEmpty(name))
			{
				ShowWarningMessage("Eksik Bilgi", "Lütfen güncellenecek düğümün ismini girin.");
				return;
			}

			var node = _myGraph.Nodes.Find(n => n.Id.ToLower() == name.ToLower());
			if (node == null)
			{
				ShowErrorMessage("Bulunamadı", $"'{name}' isimli kişi bulunamadı!");
				return;
			}

			try
			{
				// Yeni değerleri al
				node.Activity = double.Parse(TxtAddNodeAct.Text.Replace(".", ","));
				node.Interaction = double.Parse(TxtAddNodeInt.Text.Replace(".", ","));
				node.ConnectionCount = int.Parse(TxtAddNodeConn.Text);

				// DİKKAT: Özellikler değiştiği için ağırlıkları (Weight) yeniden hesaplamalıyız!
				// Bu düğüme bağlı tüm kenarları bul ve güncelle
				foreach (var edge in _myGraph.Edges)
				{
					if (edge.Source == node || edge.Target == node)
					{
						// Edge sınıfı ağırlığı dinamik hesaplıyordu (CalculateWeight), 
						// ama nesne referansları aynı olduğu için MSAGL tarafını güncellemeliyiz.
						// Ağırlık özelliği 'get' metodunda otomatik hesaplanıyor zaten.
					}
				}

				RefreshGraphDisplay();
				AutoSave();
				ShowSuccessMessage("Güncellendi", $"{name} başarıyla güncellendi!\n\nYeni Özellikler:\n• Aktiflik: {node.Activity}\n• Etkileşim: {node.Interaction}\n• Bağlantı Sayısı: {node.ConnectionCount}\n\nGrafikteki ağırlıklar otomatik olarak yenilendi.");
			}
			catch
			{
				ShowErrorMessage("Geçersiz Değer", "Lütfen sayısal değerleri doğru giriniz.");
			}
		}

		// YARDIMCI FONKSİYONLAR
		private Node GetOrCreateNode(string name)
		{
			if (string.IsNullOrEmpty(name)) return null;

			string searchName = name.Trim().ToLower();
			
			// Önce ID ile ara
			foreach (var node in _myGraph.Nodes)
			{
				if (node.Id.ToLower() == searchName) return node;
			}
			
			// Sonra Name ile ara (tam isim veya kısmi eşleşme)
			foreach (var node in _myGraph.Nodes)
			{
				if (node.Name.ToLower() == searchName || 
					node.Name.ToLower().Contains(searchName) ||
					searchName.Contains(node.Name.ToLower()))
				{
					return node;
				}
			}
			
			// Bulunamadıysa varsayılan özelliklerle yeni düğüm oluştur
			Node newNode = new Node(name, name, 0.5, 5, 1);
			_myGraph.Nodes.Add(newNode);
			return newNode;
		}

		private void HighlightNodes(List<Node> nodes, Microsoft.Msagl.Drawing.Color color)
		{
			RefreshGraphDisplay();
			foreach (var node in nodes)
			{
				var msaglNode = _viewer.Graph.FindNode(node.Id);
				if (msaglNode != null)
				{
					msaglNode.Attr.FillColor = color;
				}
			}
			_viewer.Graph = _viewer.Graph;
		}
		// Bu fonksiyonu her değişiklikten sonra çağıracağız
		private void AutoSave()
		{
			try
			{
				FileManager fm = new FileManager();
				// Dosya adının yüklediğin dosyayla aynı olduğundan emin ol
				fm.SaveGraph(_myGraph, "social_network.csv");
			}
			catch (Exception ex)
			{
				// Arka planda hata olursa kullanıcıyı rahatsız etmeyelim veya loglayalım
				System.Diagnostics.Debug.WriteLine("Otomatik kayıt hatası: " + ex.Message);
			}
		}
	}
}