namespace SocialNetworkGraph.App.Core
{
	public class Node
	{
		public string Id { get; set; }
		public string Name { get; set; }

		// Proje Ýsterlerine Göre Yeni Özellikler
		public double Activity { get; set; }
		public double Interaction { get; set; }
		public int ConnectionCount { get; set; }

		// Constructor (Yapýcý Metot)
		public Node(string id, string name, double activity, double interaction, int connectionCount)
		{
			Id = id;
			Name = name;
			Activity = activity;
			Interaction = interaction;
			ConnectionCount = connectionCount;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}