using System.Collections.Generic;
using SocialNetworkGraph.App.Core;

namespace SocialNetworkGraph.App.Algorithms.Interfaces
{
    public interface IGraphAlgorithm
    {
        // Polimorfizm için ortak arayüz
        List<Node> Execute(Graph graph, Node startNode, Node endNode = null);
    }
}