using System.Collections.Generic;

namespace SocialNetworkGraph.App.Data
{
    // Memento Pattern'in basitle�tirilmi� hali: JSON string olarak sakla
    public class UndoManager
    {
        private Stack<string> _history = new Stack<string>();
        private FileManager _fileManager = new FileManager();

        public void SaveState(Core.Graph graph)
        {
            // Grafi�i ge�ici bir dosyaya kaydeder gibi JSON stringe �eviriyoruz
            // (FileManager s�n�f�na k���k bir ekleme yapmadan, 
            // var olan SaveGraphToJson metodunu string d�nd�recek �ekilde kullanmak daha temiz olurdu
            // ama var olan yap�y� bozmamak i�in dosyaya yaz�p okuma hilesi veya
            // direkt serile�tirme yapabiliriz. �imdilik basit tutal�m:)

            // Not: FileManager'daki JSON serile�tirme kodunu buraya kopyal�yoruz pratiklik i�in.
            var data = new
            {
                Nodes = new List<dynamic>(),
                Edges = new List<dynamic>()
            };

            foreach (var n in graph.Nodes)
                data.Nodes.Add(new { n.Id, n.Name, n.Activity, n.Interaction, n.ConnectionCount });

            foreach (var e in graph.Edges)
                data.Edges.Add(new { Source = e.Source.Id, Target = e.Target.Id, Weight = e.Weight });

            string json = System.Text.Json.JsonSerializer.Serialize(data);
            _history.Push(json);
        }

        public Core.Graph Undo()
        {
            if (_history.Count == 0) return null;

            string json = _history.Pop();
            // Geri y�kleme (FileManager'daki Load mant���n�n ayn�s�)
            return new FileManager().LoadGraphFromJsonString(json);
        }
    }
}