using System.Collections.Generic;

namespace Connectivity
{
    public interface IConnGraph
    {
        /// <summary>
        /// Adds an edge between the specified vertices, if such an edge is not already present. Taken together with
        /// removeEdge, this method takes O(log^2 N) amortized time with high probability. </summary>
        /// <returns> Whether there was no edge between the vertices. </returns>
        bool AddEdge(ConnVertex connVertex1, ConnVertex connVertex2);

        /// <summary>
        /// Removes the edge between the specified vertices, if there is such an edge. Taken together with addEdge, this
        /// method takes O(log^2 N) amortized time with high probability. </summary>
        /// <returns> Whether there was an edge between the vertices. </returns>
        bool RemoveEdge(ConnVertex vertex1, ConnVertex vertex2);

        /// <summary>
        /// Returns whether the specified vertices are connected - whether there is a path between them. Returns true if
        /// vertex1 == vertex2. This method takes O(log N) time with high probability.
        /// </summary>
        bool IsConnected(ConnVertex vertex1, ConnVertex vertex2);

        /// <summary>
        /// Returns the vertices that are directly adjacent to the specified vertex. </summary>
        ICollection<ConnVertex> AdjacentVertices(ConnVertex vertex);

        /// <summary>
        /// Sets the augmentation associated with the specified vertex. This method takes O(log N) time with high
        /// probability.
        ///
        /// Note that passing a null value for the second argument is not the same as removing the augmentation. For that,
        /// you need to call removeVertexAugmentation.
        /// </summary>
        /// <returns> The augmentation that was previously associated with the vertex. Returns null if it did not have any
        ///     associated augmentation. </returns>
        object SetVertexAugmentation(ConnVertex connVertex, object vertexAugmentation);

        /// <summary>
        /// Removes any augmentation associated with the specified vertex. This method takes O(log N) time with high
        /// probability. </summary>
        /// <returns> The augmentation that was previously associated with the vertex. Returns null if it did not have any
        ///     associated augmentation. </returns>
        object RemoveVertexAugmentation(ConnVertex connVertex);

        /// <summary>
        /// Returns the augmentation associated with the specified vertex. Returns null if it does not have any associated
        /// augmentation. At present, this method takes constant expected time. Contrast with getComponentAugmentation.
        /// </summary>
        object GetVertexAugmentation(ConnVertex vertex);

        /// <summary>
        /// Returns the information about the connected component that includes the vertex.
        /// Returns an empty component if the vertex is not in the graph.
        /// This method takes O(log N) time with high probability.
        /// </summary>
        ComponentInfo GetComponentInfo(ConnVertex vertex);

        /// <summary>
        /// O(1)
        /// </summary>
        /// <returns>The number of connected components in the graph</returns>
        int GetNumberOfComponents();

        /// <summary>
        /// O(C) where C is the number of components
        /// </summary>
        /// <returns>All components in the graph</returns>
        ICollection<ComponentInfo> GetAllComponents();

        /// <summary>
        /// Returns whether the specified vertex has any associated augmentation. At present, this method takes constant
        /// expected time. Contrast with componentHasAugmentation.
        /// </summary>
        bool VertexHasAugmentation(ConnVertex vertex);

        /// <summary>
        /// Returns whether any of the vertices in the connected component containing the specified vertex has any associated
        /// augmentation. This method takes O(log N) time with high probability.
        /// </summary>
        bool ComponentHasAugmentation(ConnVertex vertex);

        /// <summary>
        /// Clears this graph, by removing all edges and vertices, and removing all augmentation information from the
        /// vertices.
        /// </summary>
        void Clear();

        /// <summary>
        /// Attempts to optimize the internal representation of the graph so that future updates will take less time. This
        /// method does not affect how long queries such as "connected" will take. You may find it beneficial to call
        /// optimize() when there is some downtime. Note that this method generally increases the amount of space the
        /// ConnGraph uses, but not beyond the bound of O(V log V + E).
        /// </summary>
        void Optimize();
    }

    public static class ConnGraphExtensions
    {
        public static object GetComponentAugmentation(this IConnGraph graph, ConnVertex vertex)
        {
            return graph.GetComponentInfo(vertex).augmentation;
        }

        public static object GetComponentSize(this IConnGraph graph, ConnVertex vertex)
        {
            return graph.GetComponentInfo(vertex).size;
        }
    }
}