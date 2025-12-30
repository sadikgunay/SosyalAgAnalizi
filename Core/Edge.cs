namespace SocialNetworkGraph.App.Core
{
    public class Edge
    {
        public Node Source { get; set; }
        public Node Target { get; set; }
        public double Weight { get; set; } = 1.0;

        public Edge() { }
        public Edge(Node source, Node target, double weight = 1.0)
        {
            Source = source;
            Target = target;
            Weight = weight;
        }
    }
}