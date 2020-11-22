﻿namespace com.github.btrekkie.connectivity
{
	/// <summary>
	/// Represents an edge in a ConnGraph, at the level of the edge (i.e. at the lowest level i for which G_i contains the
	/// edge). Every graph edge has exactly one corresponding ConnEdge object, regardless of the number of levels it appears
	/// in. See the comments for the implementation of ConnGraph.
	/// 
	/// ConnEdges are stored in the linked lists suggested by EulerTourVertex.graphListHead and
	/// EulerTourVertex.forestListHead. Each ConnEdge is in two linked lists, so care must be taken when traversing the
	/// linked lists. prev1 and next1 are the links for the list starting at vertex1.graphListHead or vertex1.forestListHead,
	/// while prev2 and next2 are the links for vertex2. But the vertex1 and vertex2 fields of a given edge are different
	/// from the vertex1 and vertex2 fields of the linked edges. For example, the edge after next1 is not necessarily
	/// next1.next1. It depends on whether next1.vertex1 is the same as vertex1. If next1.vertex1 == vertex1, then the edge
	/// after next1 is next1.next1, but otherwise, it is next1.next2.
	/// </summary>
	internal class ConnEdge
	{
		/// <summary>
		/// The edge's first endpoint (at the same level as the edge). </summary>
		public EulerTourVertex vertex1;

		/// <summary>
		/// The edge's second endpoint (at the same level as the edge). </summary>
		public EulerTourVertex vertex2;

		/// <summary>
		/// The EulerTourEdge object describing the edge's presence in an Euler tour tree, at the same level as the edge, or
		/// null if the edge is not in the Euler tour forest F_i.
		/// </summary>
		public EulerTourEdge eulerTourEdge;

		/// <summary>
		/// The edge preceding this in a linked list of same-level edges adjacent to vertex1, if any. The edge is either part
		/// of a list of non-forest edges starting with vertex1.graphListHead, or part of a list of forest edges starting
		/// with vertex1.forestListHead. Note that this list excludes any edges that also appear in lower levels.
		/// </summary>
		public ConnEdge prev1;

		/// <summary>
		/// The edge succeeding this in a linked list of same-level edges adjacent to vertex1, if any. The edge is either
		/// part of a list of non-forest edges starting with vertex1.graphListHead, or part of a list of forest edges
		/// starting with vertex1.forestListHead. Note that this list excludes any edges that also appear in lower levels.
		/// </summary>
		public ConnEdge next1;

		/// <summary>
		/// The edge preceding this in a linked list of same-level edges adjacent to vertex2, if any. The edge is either part
		/// of a list of non-forest edges starting with vertex2.graphListHead, or part of a list of forest edges starting
		/// with vertex2.forestListHead. Note that this list excludes any edges that also appear in lower levels.
		/// </summary>
		public ConnEdge prev2;

		/// <summary>
		/// The edge succeeding this in a linked list of same-level edges adjacent to vertex2, if any. The edge is either
		/// part of a list of non-forest edges starting with vertex2.graphListHead, or part of a list of forest edges
		/// starting with vertex2.forestListHead. Note that this list excludes any edges that also appear in lower levels.
		/// </summary>
		public ConnEdge next2;

		public ConnEdge(EulerTourVertex vertex1, EulerTourVertex vertex2)
		{
			this.vertex1 = vertex1;
			this.vertex2 = vertex2;
		}
	}

}