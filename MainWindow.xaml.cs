using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Windows.Threading;
using System.Windows.Media;

// Alias tanımları (Kod karmaşasını ve çakışmaları önler)
using Brushes = System.Windows.Media.Brushes;
using MsaglColor = Microsoft.Msagl.Drawing.Color;
using MsaglEdge = Microsoft.Msagl.Drawing.Edge;
using MsaglNode = Microsoft.Msagl.Drawing.Node;

using Microsoft.Msagl.WpfGraphControl;
using Microsoft.Msagl.Drawing;
using SocialNetworkGraph.App.Core;
using SocialNetworkGraph.App.Data;
using SocialNetworkGraph.App.Visualization;
using SocialNetworkGraph.App.Algorithms.Concrete;
using SocialNetworkGraph.App.Algorithms.Interfaces;

using WpfMsgBox = System.Windows.MessageBox;
using CoreNode = SocialNetworkGraph.App.Core.Node;

namespace SocialNetworkGraph.App
{
    public enum StatusType { Info, Success, Warning, Error }
    public partial class MainWindow : Window
    {
        private Core.Graph _myGraph;
        private AutomaticGraphLayoutControl _viewer;
        private GraphVisualizer _visualizer;
        private UndoManager _undoManager;
        private Data.BackupManager _backupManager;

        private DispatcherTimer _simTimer;
        private List<string> _infectedNodeIds;
        private Random _rnd = new Random();

        public MainWindow()
        {
            InitializeComponent();
            _myGraph = new Core.Graph();
            _visualizer = new GraphVisualizer();
            _undoManager = new UndoManager();
            _backupManager = new Data.BackupManager();

            _simTimer = new DispatcherTimer();
            _simTimer.Interval = TimeSpan.FromSeconds(1);
            _simTimer.Tick += SimTimer_Tick;

            SetupViewer();
            SetupBackupPlaceholder();
        }

        private void SetupBackupPlaceholder()
        {
            TxtBackupName.GotFocus += (s, e) => { if (TxtBackupName.Text == "Yedek Adı...") { TxtBackupName.Text = ""; TxtBackupName.Foreground = Brushes.White; } };
            TxtBackupName.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(TxtBackupName.Text)) { TxtBackupName.Text = "Yedek Adı..."; TxtBackupName.Foreground = Brushes.Gray; } };
            TxtBackupName.Text = "Yedek Adı..."; TxtBackupName.Foreground = Brushes.Gray;
        }

        private void SetupViewer()
        {
            _viewer = new AutomaticGraphLayoutControl();

            // --- ENTEGRE EDİLEN ÖZELLİK 1: Tıklama Garantisi ---
            // MouseDown yerine PreviewMouseLeftButtonUp kullanıyoruz.
            // Bu, MSAGL'in tıklamayı yutmasını engeller.
            _viewer.PreviewMouseLeftButtonUp += Viewer_PreviewMouseLeftButtonUp;
            _viewer.MouseRightButtonUp += Viewer_MouseRightButtonUp;

            GraphContainer.Child = _viewer;
        }

        private void SaveStateForUndo() => _undoManager.SaveState(_myGraph);

        private void RefreshGraph()
        {
            if (_myGraph == null) return;
            var msaglGraph = _visualizer.CreateMsaglGraph(_myGraph);
            _viewer.Graph = msaglGraph;
            ApplyFilter();
            TxtDetails.Text = $"{_myGraph.Nodes.Count} Düğüm | {_myGraph.Edges.Count} Kenar";
        }

        private void ResetVisuals()
        {
            if (_viewer.Graph == null) return;
            var defFill = new MsaglColor(30, 41, 59);
            var defBorder = new MsaglColor(34, 211, 238);
            foreach (var n in _viewer.Graph.Nodes) { n.Attr.FillColor = defFill; n.Attr.Color = defBorder; }
            foreach (var e in _viewer.Graph.Edges) { e.Attr.Color = new MsaglColor(71, 85, 105); }
        }

        private void FilterSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ApplyFilter();

        private void ApplyFilter()
        {
            if (_viewer == null || _viewer.Graph == null || _myGraph == null) return;

            // Filtreleme mantığını iyileştirdik: Max ağırlığa göre oranla
            double maxWeight = _myGraph.Edges.Any() ? _myGraph.Edges.Max(x => x.Weight) : 1.0;
            double threshold = maxWeight * FilterSlider.Value;

            foreach (var edge in _viewer.Graph.Edges)
            {
                var coreEdge = _myGraph.Edges.FirstOrDefault(x =>
                    (x.Source.Id == edge.Source && x.Target.Id == edge.Target) ||
                    (x.Source.Id == edge.Target && x.Target.Id == edge.Source));

                if (coreEdge != null)
                {
                    edge.IsVisible = coreEdge.Weight >= threshold;
                }
                else
                {
                    edge.IsVisible = false;
                }
            }
            _viewer.Graph = _viewer.Graph;
        }

        // --- ENTEGRE EDİLEN ÖZELLİK 2: Güçlendirilmiş Hit-Test ---
        private object GetMsaglObjectFromClick(object originalSource)
        {
            var current = originalSource as DependencyObject;
            int depth = 0;
            // Derinliği 50'ye çıkardık ve Viewer dışına taşmayı engelledik
            while (current != null && current != _viewer && depth < 50)
            {
                if (current is FrameworkElement element)
                {
                    // Hem Tag hem DataContext kontrolü yapıyoruz
                    if (element.Tag is IViewerNode vn) return vn.Node;
                    if (element.Tag is IViewerEdge ve) return ve.Edge;
                    if (element.DataContext is IViewerNode vnDc) return vnDc.Node;
                    if (element.DataContext is IViewerEdge veDc) return veDc.Edge;
                }
                try { current = VisualTreeHelper.GetParent(current); } catch { break; }
                depth++;
            }
            return null;
        }

        // --- ENTEGRE EDİLEN ÖZELLİK 3: Sol Tık Olayı ---
        private void Viewer_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hitObject = GetMsaglObjectFromClick(e.OriginalSource);

            if (hitObject is MsaglNode msaglNode)
            {
                if (msaglNode.UserData is CoreNode cn)
                {
                    // 1. Editör panelini doldur
                    FillNodePanel(cn);
                    TxtStatus.Text = $"SEÇİLDİ: {cn.Name}";

                    // 2. BİLGİ PANELİNİ AÇ (İsteğiniz üzerine eklendi)
                    ShowLegend("DÜĞÜM DETAYI",
                        $"📌 Ad: {cn.Name}\n" +
                        $"⚡ Aktivite: {cn.Activity:F2}\n" +
                        $"🔄 Etkileşim: {cn.Interaction:F2}\n" +
                        $"🔗 Bağlantılar: {cn.ConnectionCount}");

                    // 3. Görsel vurgulama (Cyan yap)
                    msaglNode.Attr.FillColor = MsaglColor.Cyan;
                    _viewer.Graph = _viewer.Graph; // Ekranı yenile
                }
            }
            // Kenar tıklama veya boşluk tıklama buraya eklenebilir ama genelde sol tık seçim içindir.
        }

        // --- ENTEGRE EDİLEN ÖZELLİK 4: Sağ Tık Olayı ---
        private void Viewer_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var hitObject = GetMsaglObjectFromClick(e.OriginalSource);

            if (hitObject is MsaglNode msaglNode && msaglNode.UserData is CoreNode cn)
            {
                OpenNodeContextMenu(cn);
            }
            else if (hitObject is MsaglEdge msaglEdge)
            {
                OpenEdgeContextMenu(msaglEdge);
            }
            else if (hitObject == null)
            {
                OpenCanvasContextMenu();
            }
        }

        // --- MENÜLER ---
        private void OpenEdgeContextMenu(MsaglEdge e)
        {
            ContextMenu cm = new ContextMenu();
            MenuItem d = new MenuItem { Header = "✂️ Bağlantıyı Kopar", Foreground = Brushes.Red };
            d.Click += (o, args) => {
                SaveStateForUndo();
                string sId = e.Source; string tId = e.Target;
                var edgeToRemove = _myGraph.Edges.FirstOrDefault(x => (x.Source.Id == sId && x.Target.Id == tId) || (x.Source.Id == tId && x.Target.Id == sId));
                if (edgeToRemove != null)
                {
                    _myGraph.Edges.Remove(edgeToRemove);
                    _myGraph.UpdateConnectionCounts();
                }
                e.IsVisible = false;
                _viewer.Graph = _viewer.Graph;
                TxtStatus.Text = "Bağlantı koptu.";
            };
            cm.Items.Add(d); cm.IsOpen = true;
        }

        private void OpenNodeContextMenu(CoreNode n)
        {
            ContextMenu cm = new ContextMenu();
            MenuItem s = new MenuItem { Header = "🎯 Kaynak Yap" }; s.Click += (o, e) => TxtSource.Text = n.Id;
            MenuItem t = new MenuItem { Header = "🏁 Hedef Yap" }; t.Click += (o, e) => TxtTarget.Text = n.Id;
            MenuItem d = new MenuItem { Header = "🗑️ Sil", Foreground = Brushes.Red }; d.Click += (o, e) => { SaveStateForUndo(); DeleteNode(n.Id); };
            cm.Items.Add(s); cm.Items.Add(t); cm.Items.Add(new Separator()); cm.Items.Add(d); cm.IsOpen = true;
        }

        private void OpenCanvasContextMenu()
        {
            ContextMenu cm = new ContextMenu();
            MenuItem a = new MenuItem { Header = "✨ Yeni Kişi" }; a.Click += (o, e) => { TxtNodeName.Text = "Yeni_" + (_myGraph.Nodes.Count + 1); };
            cm.Items.Add(a); cm.IsOpen = true;
        }
        // XAML tarafında tanımlı ama kodda eksik olan Search butonu olayı
        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void PerformSearch()
        {
            string q = TxtSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(q)) return;

            var fn = _myGraph.Nodes.FirstOrDefault(n => n.Name.ToLower().Contains(q));

            if (fn != null)
            {
                ResetVisuals();
                var vn = _viewer.Graph.FindNode(fn.Id);

                if (vn != null)
                    vn.Attr.FillColor = Microsoft.Msagl.Drawing.Color.Cyan;

                _viewer.Graph = _viewer.Graph;
                FillNodePanel(fn);
                UpdateStatus($"Bulundu: {fn.Name}", StatusType.Success);
            }
            else
            {
                UpdateStatus("Kullanıcı bulunamadı.", StatusType.Error);
            }
        }

        // --- DİĞER FONKSİYONLAR ---
        private void BtnUndo_Click(object sender, RoutedEventArgs e) { var p = _undoManager.Undo(); if (p != null) { _myGraph = p; RefreshGraph(); ShowLegend("GERİ ALINDI", "Döndü."); } else WpfMsgBox.Show("Yok."); }

        // --- RASTGELE AĞ BUTONU (DÜZELTİLDİ) ---
        private void BtnRandomGraph_Click(object sender, RoutedEventArgs e)
        {
            SaveStateForUndo();

            // YENİ KOD: Parametre vermeden çağırıyoruz, 0-100 arası rastgele yapıyor.
            _myGraph = new RandomGraphGenerator().GenerateRandomized();

            RefreshGraph();

            // Hem durum çubuğuna hem de sağdaki bilgi paneline yazdıralım
            UpdateStatus($"Rastgele ağ oluşturuldu. Düğüm Sayısı: {_myGraph.Nodes.Count}", StatusType.Success);
            ShowLegend("RASTGELE", $"Oluşturulan Düğüm Sayısı: {_myGraph.Nodes.Count}");
        }

        private void TxtSearch_KeyDown(object sender, System.Windows.Input.KeyEventArgs e) { if (e.Key == System.Windows.Input.Key.Enter) PerformSearch(); }

        // Analizler
        private void BtnLinkPrediction_Click(object sender, RoutedEventArgs e) { var sw = Stopwatch.StartNew(); var s = new LinkPredictionAlgorithm().Execute(_myGraph); sw.Stop(); ShowLegend("ÖNERİLER", (s.Count > 0 ? string.Join("\n", s) : "Yok") + $"\n{GetElapsedTime(sw)}"); }
        private void BtnBetweenness_Click(object sender, RoutedEventArgs e) { var sw = Stopwatch.StartNew(); var r = new BetweennessCentralityAlgorithm().Execute(_myGraph); sw.Stop(); ResetVisuals(); int c = 0; foreach (var kv in r) { if (c++ >= 3) break; var n = _viewer.Graph.FindNode(kv.Key.Id); if (n != null) { n.Attr.Shape = Shape.Diamond; n.Attr.FillColor = MsaglColor.Magenta; } } _viewer.Graph = _viewer.Graph; ShowLegend("KÖPRÜLER", string.Join("\n", r.Take(3).Select(x => $"{x.Key.Name} ({x.Value})")) + $"\n{GetElapsedTime(sw)}"); }
        private void BtnAnalysis_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var results = new DegreeCentralityAlgorithm().Execute(_myGraph);
            sw.Stop();

            ResetVisuals();
            foreach (var item in results)
            {
                var n = _viewer.Graph.FindNode(item.Key.Id);
                if (n != null) n.Attr.Color = MsaglColor.Gold;
            }
            _viewer.Graph = _viewer.Graph;

            var tableText = "EN YÜKSEK 5 DÜĞÜM:\n─────────────────────\n";
            int rank = 1;
            foreach (var item in results.Take(5))
            {
                tableText += $"{rank}. {item.Key.Name}\n   Derece: {item.Value}\n";
                rank++;
            }
            tableText += $"─────────────────────\nSüre: {GetElapsedTime(sw)}";

            ShowLegend("DEGREE CENTRALITY", tableText);
        }
        private void BtnComponents_Click(object sender, RoutedEventArgs e) { var sw = Stopwatch.StartNew(); var c = new ConnectedComponentsAlgorithm().Execute(_myGraph); sw.Stop(); ResetVisuals(); Random r = new Random(); foreach (var l in c) { var col = new MsaglColor((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255)); foreach (var n in l) { var vn = _viewer.Graph.FindNode(n.Id); if (vn != null) vn.Attr.FillColor = col; } } _viewer.Graph = _viewer.Graph; ShowLegend("TOPLULUK", $"Grup: {c.Count}\n{GetElapsedTime(sw)}"); }
        private void BtnWelshPowell_Click(object sender, RoutedEventArgs e)
        {
            var sw = Stopwatch.StartNew();
            var coloring = new WelshPowellAlgorithm().Execute(_myGraph);
            sw.Stop();

            ResetVisuals();
            foreach (var item in coloring)
            {
                var n = _viewer.Graph.FindNode(item.Key.Id);
                if (n != null) n.Attr.FillColor = item.Value;
            }
            _viewer.Graph = _viewer.Graph;

            var tableText = "WELSH-POWELL BOYAMA:\n─────────────────────\n";
            int colorIndex = 1;
            var colorGroups = coloring.GroupBy(x => x.Value).ToList();
            foreach (var group in colorGroups)
            {
                var color = group.Key;
                tableText += $"Renk {colorIndex}: RGB({color.R},{color.G},{color.B})\n  Düğümler: {string.Join(", ", group.Select(x => x.Key.Name))}\n";
                colorIndex++;
            }
            tableText += $"─────────────────────\nToplam Renk: {colorGroups.Count}\nSüre: {GetElapsedTime(sw)}";

            ShowLegend("WELSH-POWELL", tableText);
        }

        // Algoritmalar
        private void RunAlgo(IGraphAlgorithm alg, string name, MsaglColor color)
        {
            var s = _myGraph.Nodes.FirstOrDefault(x => x.Id == TxtSource.Text); var t = _myGraph.Nodes.FirstOrDefault(x => x.Id == TxtTarget.Text);
            if (s == null || t == null) return; var sw = Stopwatch.StartNew(); var p = alg.Execute(_myGraph, s, t); sw.Stop();
            if (p.Count > 0) { ResetVisuals(); foreach (var n in p) { var vn = _viewer.Graph.FindNode(n.Id); if (vn != null) vn.Attr.FillColor = color; } for (int i = 0; i < p.Count - 1; i++) { var u = p[i].Id; var v = p[i + 1].Id; var ed = _viewer.Graph.Edges.FirstOrDefault(x => (x.Source == u && x.Target == v) || (x.Source == v && x.Target == u)); if (ed != null) ed.Attr.Color = color; } _viewer.Graph = _viewer.Graph; ShowLegend(name, $"Adım: {p.Count - 1}\nMaliyet: {CalculatePathCost(p):F2}\n{GetElapsedTime(sw)}"); } else WpfMsgBox.Show("Yol yok.");
        }
        private void BtnRunDijkstra_Click(object sender, RoutedEventArgs e) => RunAlgo(new DijkstraAlgorithm(), "Dijkstra", MsaglColor.Red);
        private void BtnRunAStar_Click(object sender, RoutedEventArgs e) => RunAlgo(new AStarAlgorithm(), "A*", MsaglColor.Orange);
        private void BtnRunBFS_Click(object sender, RoutedEventArgs e) { var s = _myGraph.Nodes.FirstOrDefault(n => n.Id == TxtSource.Text); if (s != null) { var sw = Stopwatch.StartNew(); var r = new BFSAlgorithm().Execute(_myGraph, s); sw.Stop(); ResetVisuals(); foreach (var n in r) { var vn = _viewer.Graph.FindNode(n.Id); if (vn != null) vn.Attr.FillColor = MsaglColor.LightGreen; } _viewer.Graph = _viewer.Graph; ShowLegend("BFS", $"Erişilen: {r.Count}\n{GetElapsedTime(sw)}"); } }
        private void BtnRunDFS_Click(object sender, RoutedEventArgs e) { var s = _myGraph.Nodes.FirstOrDefault(n => n.Id == TxtSource.Text); if (s != null) { var sw = Stopwatch.StartNew(); var r = new DFSAlgorithm().Execute(_myGraph, s); sw.Stop(); ResetVisuals(); foreach (var n in r) { var vn = _viewer.Graph.FindNode(n.Id); if (vn != null) vn.Attr.FillColor = MsaglColor.LightBlue; } _viewer.Graph = _viewer.Graph; ShowLegend("DFS", $"Erişilen: {r.Count}\n{GetElapsedTime(sw)}"); } }

        // Diğer
        private string GetElapsedTime(Stopwatch sw) => sw.Elapsed.TotalMilliseconds < 0.1 ? $"{sw.ElapsedTicks} Ticks" : $"{sw.Elapsed.TotalMilliseconds:F4} ms";
        private double CalculatePathCost(List<CoreNode> path) { double c = 0; for (int i = 0; i < path.Count - 1; i++) { var e = _myGraph.Edges.FirstOrDefault(x => (x.Source == path[i] && x.Target == path[i + 1]) || (x.Source == path[i + 1] && x.Target == path[i])); if (e != null) c += e.Weight; } return c; }
        private void BtnSimulate_Click(object sender, RoutedEventArgs e) { if (_simTimer.IsEnabled) { _simTimer.Stop(); BtnSimulate.Content = "BAŞLAT"; return; } if (_myGraph.Nodes.All(n => n.Id != TxtSource.Text)) return; ResetVisuals(); _infectedNodeIds = new List<string> { TxtSource.Text }; var sn = _viewer.Graph.FindNode(TxtSource.Text); if (sn != null) sn.Attr.FillColor = MsaglColor.Red; _viewer.Graph = _viewer.Graph; _simTimer.Start(); BtnSimulate.Content = "DURDUR"; }
        private void SimTimer_Tick(object sender, EventArgs e) { var ni = new List<string>(); bool c = false; foreach (var id in _infectedNodeIds) { var nbs = _myGraph.Edges.Where(ed => ed.Source.Id == id || ed.Target.Id == id).Select(ed => ed.Source.Id == id ? ed.Target.Id : ed.Source.Id); foreach (var nid in nbs) { if (!_infectedNodeIds.Contains(nid) && !ni.Contains(nid) && _rnd.NextDouble() < 0.3) { ni.Add(nid); c = true; var n = _viewer.Graph.FindNode(nid); if (n != null) n.Attr.FillColor = MsaglColor.OrangeRed; } } } if (c) { _infectedNodeIds.AddRange(ni); _viewer.Graph = _viewer.Graph; TxtStatus.Text = $"Enfekte: {_infectedNodeIds.Count}"; } }
        private void BtnReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var html = GenerateHtmlReport();
                var saveDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "HTML Files|*.html",
                    FileName = "graph_report.html"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    File.WriteAllText(saveDialog.FileName, html);
                    WpfMsgBox.Show("HTML raporu oluşturuldu!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                WpfMsgBox.Show($"Rapor oluşturulurken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenerateHtmlReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang='tr'>");
            sb.AppendLine("<head>");
            sb.AppendLine("<meta charset='UTF-8'>");
            sb.AppendLine("<title>Graf Analiz Raporu</title>");
            sb.AppendLine("<style>");
            sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; background: #f5f5f5; }");
            sb.AppendLine("h1, h2 { color: #333; }");
            sb.AppendLine("table { border-collapse: collapse; width: 100%; margin: 20px 0; background: white; }");
            sb.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            sb.AppendLine("th { background-color: #4CAF50; color: white; }");
            sb.AppendLine("tr:nth-child(even) { background-color: #f2f2f2; }");
            sb.AppendLine(".section { background: white; padding: 20px; margin: 20px 0; border-radius: 5px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }");
            sb.AppendLine("</style>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h1>Graf Analiz Raporu</h1>");
            sb.AppendLine($"<p><strong>Düğüm Sayısı:</strong> {_myGraph.Nodes.Count}</p>");
            sb.AppendLine($"<p><strong>Kenar Sayısı:</strong> {_myGraph.Edges.Count}</p>");
            sb.AppendLine("</div>");

            // Komşuluk Matrisi
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Komşuluk Matrisi</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th></th>");
            foreach (var node in _myGraph.Nodes.OrderBy(n => n.Id))
            {
                sb.AppendLine($"<th>{node.Id}</th>");
            }
            sb.AppendLine("</tr>");

            foreach (var node1 in _myGraph.Nodes.OrderBy(n => n.Id))
            {
                sb.AppendLine($"<tr><th>{node1.Id}</th>");
                foreach (var node2 in _myGraph.Nodes.OrderBy(n => n.Id))
                {
                    bool connected = _myGraph.Edges.Any(e =>
                        (e.Source.Id == node1.Id && e.Target.Id == node2.Id) ||
                        (e.Source.Id == node2.Id && e.Target.Id == node1.Id));
                    var weight = _myGraph.Edges.FirstOrDefault(e =>
                        (e.Source.Id == node1.Id && e.Target.Id == node2.Id) ||
                        (e.Source.Id == node2.Id && e.Target.Id == node1.Id))?.Weight ?? 0;

                    if (node1.Id == node2.Id)
                        sb.AppendLine("<td>-</td>");
                    else
                        sb.AppendLine($"<td>{(connected ? weight.ToString("F4") : "0")}</td>");
                }
                sb.AppendLine("</tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            // Komşuluk Listesi
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Komşuluk Listesi</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Düğüm</th><th>Komşular</th></tr>");
            foreach (var node in _myGraph.Nodes.OrderBy(n => n.Id))
            {
                var neighbors = _myGraph.Edges
                    .Where(e => e.Source.Id == node.Id || e.Target.Id == node.Id)
                    .Select(e => e.Source.Id == node.Id ? e.Target.Id : e.Source.Id)
                    .OrderBy(id => id)
                    .ToList();
                sb.AppendLine($"<tr><td>{node.Id}</td><td>{string.Join(", ", neighbors)}</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            // Degree Centrality
            var degreeResults = new DegreeCentralityAlgorithm().Execute(_myGraph);
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Degree Centrality - En Yüksek 5 Düğüm</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Sıra</th><th>Düğüm</th><th>Derece</th></tr>");
            int rank = 1;
            foreach (var item in degreeResults.Take(5))
            {
                sb.AppendLine($"<tr><td>{rank}</td><td>{item.Key.Name}</td><td>{item.Value}</td></tr>");
                rank++;
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            // Welsh-Powell Boyama Tablosu
            var coloring = new WelshPowellAlgorithm().Execute(_myGraph);
            sb.AppendLine("<div class='section'>");
            sb.AppendLine("<h2>Welsh-Powell Graf Boyama Sonuçları</h2>");
            sb.AppendLine("<table>");
            sb.AppendLine("<tr><th>Düğüm</th><th>Renk (RGB)</th></tr>");
            foreach (var item in coloring.OrderBy(x => x.Key.Id))
            {
                var color = item.Value;
                sb.AppendLine($"<tr><td>{item.Key.Name}</td><td style='background-color: rgb({color.R},{color.G},{color.B});'>RGB({color.R}, {color.G}, {color.B})</td></tr>");
            }
            sb.AppendLine("</table>");
            sb.AppendLine("</div>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");
            return sb.ToString();
        }
        private void BtnClear_Click(object sender, RoutedEventArgs e) { SaveStateForUndo(); _myGraph.Clear(); RefreshGraph(); LegendPanel.Visibility = Visibility.Collapsed; }
        private void BtnLoadGraph_Click(object sender, RoutedEventArgs e) { try { SaveStateForUndo(); _myGraph = new FileManager().LoadGraph("social_network.csv"); RefreshGraph(); } catch { } }
        private void BtnLoadJson_Click(object sender, RoutedEventArgs e) { var d = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json" }; if (d.ShowDialog() == true) { SaveStateForUndo(); _myGraph = new FileManager().LoadGraphFromJson(d.FileName); RefreshGraph(); } }
        private void BtnExport_Click(object sender, RoutedEventArgs e) { var fm = new FileManager(); fm.SaveGraph(_myGraph, "saved.csv"); fm.SaveGraphToJson(_myGraph, "saved.json"); WpfMsgBox.Show("Kaydedildi"); }

        // Yedek Yönetimi
        private void BtnSaveBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string backupName = TxtBackupName.Text.Trim();
                if (string.IsNullOrWhiteSpace(backupName) || backupName == "Yedek Adı...")
                {
                    backupName = $"Yedek_{DateTime.Now:yyyyMMdd_HHmmss}";
                }

                string backupId = _backupManager.SaveBackup(_myGraph, backupName);
                TxtBackupName.Text = "Yedek Adı...";
                TxtBackupName.Foreground = Brushes.Gray;
                string backupPath = Path.Combine(Directory.GetCurrentDirectory(), "backups");
                WpfMsgBox.Show($"Yedek kaydedildi!\n\nAdı: {backupName}\nKonum: {backupPath}\nToplam Yedek: {_backupManager.GetBackupCount()}/30", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);

                if (BackupListPanel.Visibility == Visibility.Visible)
                {
                    RefreshBackupList();
                }
            }
            catch (Exception ex)
            {
                WpfMsgBox.Show($"Yedek kaydedilirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Bildirim kutusunu (LegendPanel) kapatan özel kod
        private void BtnCloseLegend_Click(object sender, RoutedEventArgs e)
        {
            if (LegendPanel != null)
            {
                LegendPanel.Visibility = Visibility.Collapsed;
            }
        }
        private void BtnShowBackups_Click(object sender, RoutedEventArgs e)
        {
            if (BackupListPanel.Visibility == Visibility.Visible)
            {
                BackupListPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                BackupListPanel.Visibility = Visibility.Visible;
                RefreshBackupList();
            }
        }

        private void RefreshBackupList()
        {
            BackupListContainer.Children.Clear();

            var backups = _backupManager.GetAllBackups();

            if (backups.Count == 0)
            {
                var emptyText = new TextBlock
                {
                    Text = "Henüz yedek yok.",
                    Foreground = Brushes.Gray,
                    FontSize = 11,
                    Margin = new Thickness(0, 5, 0, 5)
                };
                BackupListContainer.Children.Add(emptyText);
                return;
            }

            foreach (var backup in backups)
            {
                var border = new Border
                {
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 41, 59)),
                    BorderBrush = new SolidColorBrush(System.Windows.Media.Color.FromRgb(71, 85, 105)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Margin = new Thickness(0, 0, 0, 8),
                    Padding = new Thickness(10)
                };

                var stackPanel = new StackPanel();

                var nameText = new TextBlock
                {
                    Text = backup.Name,
                    Foreground = Brushes.White,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                stackPanel.Children.Add(nameText);

                var infoText = new TextBlock
                {
                    Text = $"📊 {backup.NodeCount} Düğüm | {backup.EdgeCount} Kenar\n🕐 {backup.CreatedAt:dd.MM.yyyy HH:mm:ss}",
                    Foreground = Brushes.Gray,
                    FontSize = 10,
                    Margin = new Thickness(0, 0, 0, 8)
                };
                stackPanel.Children.Add(infoText);

                // ENTEGRE DÜZELTME: Tam ad alanı (namespace) kullanarak Orientation çakışmasını çözdük
                var buttonPanel = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };

                // ENTEGRE DÜZELTME: Tam ad alanı kullanarak Button çakışmasını çözdük
                var loadBtn = new System.Windows.Controls.Button
                {
                    Content = "📥 YÜKLE",
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129)),
                    Foreground = Brushes.White,
                    FontSize = 10,
                    Padding = new Thickness(8, 4, 8, 4),
                    Margin = new Thickness(0, 0, 5, 0),
                    Tag = backup.Id
                };
                loadBtn.Click += BtnLoadBackup_Click;
                buttonPanel.Children.Add(loadBtn);

                var deleteBtn = new System.Windows.Controls.Button
                {
                    Content = "🗑️ SİL",
                    Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(239, 68, 68)),
                    Foreground = Brushes.White,
                    FontSize = 10,
                    Padding = new Thickness(8, 4, 8, 4),
                    Tag = backup.Id
                };
                deleteBtn.Click += BtnDeleteBackup_Click;
                buttonPanel.Children.Add(deleteBtn);

                stackPanel.Children.Add(buttonPanel);
                border.Child = stackPanel;
                BackupListContainer.Children.Add(border);
            }

            var countText = new TextBlock
            {
                Text = $"Toplam: {backups.Count}/30 yedek",
                Foreground = Brushes.Gray,
                FontSize = 9,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            BackupListContainer.Children.Add(countText);
        }

        private void BtnLoadBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ENTEGRE DÜZELTME: Sender cast işlemi
                var button = sender as System.Windows.Controls.Button;
                if (button?.Tag is string backupId)
                {
                    SaveStateForUndo();
                    var loadedGraph = _backupManager.LoadBackup(backupId);
                    if (loadedGraph != null)
                    {
                        _myGraph = loadedGraph;
                        RefreshGraph();
                        WpfMsgBox.Show("Yedek yüklendi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        WpfMsgBox.Show("Yedek yüklenemedi!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMsgBox.Show($"Yedek yüklenirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnDeleteBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ENTEGRE DÜZELTME: Sender cast işlemi
                var button = sender as System.Windows.Controls.Button;
                if (button?.Tag is string backupId)
                {
                    var result = WpfMsgBox.Show("Bu yedeği silmek istediğinize emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        if (_backupManager.DeleteBackup(backupId))
                        {
                            RefreshBackupList();
                            WpfMsgBox.Show("Yedek silindi!", "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            WpfMsgBox.Show("Yedek silinemedi!", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WpfMsgBox.Show($"Yedek silinirken hata: {ex.Message}", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FillNodePanel(CoreNode n)
        {
            _myGraph.UpdateConnectionCounts();

            TxtNodeName.Text = n.Name;
            TxtAct.Text = n.Activity.ToString("F2");
            TxtInt.Text = n.Interaction.ToString("F2");
            TxtConn.Text = n.ConnectionCount.ToString();
        }
        private void ShowLegend(string t, string d) { LegendPanel.Visibility = Visibility.Visible; LegendTitle.Text = t; LegendText.Text = d; }
        private void BtnAddNode_Click(object sender, RoutedEventArgs e)
        {
            SaveStateForUndo();
            double.TryParse(TxtAct.Text, out double a);
            double.TryParse(TxtInt.Text, out double i);
            int.TryParse(TxtConn.Text, out int c);

            var n = _myGraph.Nodes.FirstOrDefault(x => x.Id == TxtNodeName.Text)
                ?? new CoreNode(TxtNodeName.Text, TxtNodeName.Text, a, i, c);

            bool isUpdate = _myGraph.Nodes.Contains(n);
            n.Activity = a;
            n.Interaction = i;
            n.ConnectionCount = c;

            _myGraph.AddNode(n);

            _myGraph.UpdateConnectionCounts();

            if (isUpdate)
            {
                foreach (var edge in _myGraph.Edges.Where(e => e.Source.Id == n.Id || e.Target.Id == n.Id))
                {
                    var otherNode = edge.Source.Id == n.Id ? edge.Target : edge.Source;
                    edge.Weight = Core.Graph.CalculateDynamicWeight(n, otherNode);
                }
            }

            RefreshGraph();
        }
        private void BtnAddEdge_Click(object sender, RoutedEventArgs e)
        {
            var s = _myGraph.Nodes.FirstOrDefault(x => x.Id == TxtSource.Text);
            var t = _myGraph.Nodes.FirstOrDefault(x => x.Id == TxtTarget.Text);
            if (s != null && t != null)
            {
                if (s.Id == t.Id)
                {
                    WpfMsgBox.Show("Self-loop yasak.");
                    return;
                }

                bool exists = _myGraph.Edges.Any(ed =>
                    (ed.Source.Id == s.Id && ed.Target.Id == t.Id) ||
                    (ed.Source.Id == t.Id && ed.Target.Id == s.Id));

                if (exists)
                {
                    WpfMsgBox.Show("Bu bağlantı zaten var.");
                    return;
                }

                SaveStateForUndo();
                double w = CalculateDynamicWeight(s, t);
                _myGraph.AddEdge(s, t, w);
                RefreshGraph();
                ShowLegend("BAĞLANTI", $"Maliyet: {w:F4}");
            }
            else
            {
                WpfMsgBox.Show("Seçim yap.");
            }
        }
        private double CalculateDynamicWeight(CoreNode n1, CoreNode n2)
        {
            return Core.Graph.CalculateDynamicWeight(n1, n2);
        }
        private void DeleteNode(string id)
        {
            var n = _myGraph.Nodes.FirstOrDefault(x => x.Id == id);
            if (n != null)
            {
                _myGraph.Nodes.Remove(n);
                _myGraph.Edges.RemoveAll(x => x.Source.Id == id || x.Target.Id == id);
                _myGraph.UpdateConnectionCounts();
                RefreshGraph();
            }
        }
        // Durum çubuğunu güncelleyen yardımcı metot
        private void UpdateStatus(string msg, StatusType type)
        {
            if (TxtStatus == null) return;

            TxtStatus.Text = msg;
            if (TxtStatusIcon != null)
            {
                switch (type)
                {
                    case StatusType.Info:
                        TxtStatusIcon.Text = "ℹ️";
                        TxtStatus.Foreground = System.Windows.Media.Brushes.White;
                        break;
                    case StatusType.Success:
                        TxtStatusIcon.Text = "🟢";
                        TxtStatus.Foreground = System.Windows.Media.Brushes.LightGreen;
                        break;
                    case StatusType.Warning:
                        TxtStatusIcon.Text = "⚠️";
                        TxtStatus.Foreground = System.Windows.Media.Brushes.Orange;
                        break;
                    case StatusType.Error:
                        TxtStatusIcon.Text = "⛔";
                        TxtStatus.Foreground = System.Windows.Media.Brushes.Red;
                        break;
                }
            }
        }
    }
}