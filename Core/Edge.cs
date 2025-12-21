using System;

namespace SocialNetworkGraph.App.Core
{
	public class Edge
	{
		public Node Source { get; private set; } // Kaynak Düğüm
		public Node Target { get; private set; } // Hedef Düğüm

		// Ağırlık dışarıdan set edilemez, sadece hesaplanır (Read-Only Logic)
		public double Weight => CalculateWeight();

		public Edge(Node source, Node target)
		{
			Source = source;
			Target = target;
		}

		/// <summary>
		/// İki kullanıcı arasındaki benzerlik/mesafeyi hesaplayan formül.
		/// Proje gereksinimlerine göre: Ağırlık = 1 / (1 + sqrt((Aktiflik_i - Aktiflik_j)² + (Etkileşim_i - Etkileşim_j)² + (Bağlantı_i - Bağlantı_j)²))
		/// Benzer özelliklere sahip düğümler arasındaki uzaklık küçük olacağından ağırlık değeri yüksek olur.
		/// </summary>
		private double CalculateWeight()
		{
			// İki düğüm arasındaki özellik farklarının karesini al
			double diffActivity = Math.Pow(Source.Activity - Target.Activity, 2);
			double diffInteraction = Math.Pow(Source.Interaction - Target.Interaction, 2);
			double diffConnection = Math.Pow(Source.ConnectionCount - Target.ConnectionCount, 2);

			// Öklid mesafesi
			double euclideanDistance = Math.Sqrt(diffActivity + diffInteraction + diffConnection);

			// Proje gereksinimlerine göre formül: 1 / (1 + mesafe)
			// Sonucu virgülden sonra 4 hane olacak şekilde yuvarlıyoruz (daha hassas).
			double result = 1.0 / (1.0 + euclideanDistance);
			return Math.Round(result, 4);
		}

		public override string ToString()
		{
			return $"{Source.Name} -> {Target.Name} (W: {Weight})";
		}
	}
}
