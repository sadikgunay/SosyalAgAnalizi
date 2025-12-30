namespace SocialNetworkGraph.App.Core
{
    public class Node
    {
        public string Id { get; set; }
        public string Name { get; set; }

        // Proje Ýsterleri: Sayýsal Özellikler
        public double Activity { get; set; }
        public double Interaction { get; set; }
        public int ConnectionCount { get; set; }

        // EKLEME: Bu boþ satýr, JSON iþlemlerinin hatasýz çalýþmasýný garantiler.
        public Node() { }

        public Node(string id, string name, double activity, double interaction, int connectionCount)
        {
            Id = id;
            Name = name;
            Activity = activity;
            Interaction = interaction;
            ConnectionCount = connectionCount;
        }

        // Listelerde "Bu düðüm zaten var mý?" kontrolünün doðru çalýþmasý için:
        public override bool Equals(object obj)
        {
            if (obj is Node other)
            {
                // ID'leri aynýysa, bunlar ayný kiþidir.
                return this.Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id != null ? Id.GetHashCode() : 0;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}