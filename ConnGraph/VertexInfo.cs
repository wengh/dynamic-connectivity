using System.Collections.Generic;

namespace Connectivity
{

	/// <summary>
	/// Describes a ConnVertex, with respect to a particular ConnGraph. There is exactly one VertexInfo object per vertex in
	/// a given graph, regardless of how many levels the vertex is in. See the comments for the implementation of ConnGraph.
	/// </summary>
	internal class VertexInfo
	{
		/// <summary>
		/// The representation of the vertex in the highest level. </summary>
		public EulerTourVertex vertex;

		/// <summary>
		/// A map from each ConnVertex adjacent to this vertex to the ConnEdge object for the edge connecting it to this
		/// vertex. Lookups take O(1) expected time and O(log N / log log N) time with high probability, because "edges" is a
		/// HashMap, and ConnVertex.hashCode() returns a random integer.
		/// </summary>
		public IDictionary<ConnVertex, ConnEdge> edges = new Dictionary<ConnVertex, ConnEdge>();

		/// <summary>
		/// The maximum number of entries in "edges" since the last time we "rebuilt" that field. When the number of edges
		/// drops sufficiently, we rebuild "edges" by copying its contents to a new HashMap. We do this to ensure that
		/// "edges" uses O(K) space, where K is the number of vertices adjacent to this. (The capacity of a HashMap is not
		/// automatically reduced as the number of entries decreases, so we have to limit space usage manually.)
		/// </summary>
		public int maxEdgeCountSinceRebuild;

		public VertexInfo(EulerTourVertex vertex)
		{
			this.vertex = vertex;
		}
	}

}