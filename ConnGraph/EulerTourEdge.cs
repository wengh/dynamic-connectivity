namespace com.github.btrekkie.connectivity
{
	/// <summary>
	/// The representation of a forest edge in some Euler tour forest F_i at some particular level i. Each forest edge has
	/// one EulerTourEdge object for each level it appears in. See the comments for the implementation of ConnGraph.
	/// </summary>
	internal class EulerTourEdge
	{
		/// <summary>
		/// One of the two visits preceding the edge in the Euler tour, in addition to visit2. (The node is at the same level
		/// as the EulerTourEdge.)
		/// </summary>
		public readonly EulerTourNode visit1;

		/// <summary>
		/// One of the two visits preceding the edge in the Euler tour, in addition to visit1. (The node is at the same level
		/// as the EulerTourEdge.)
		/// </summary>
		public readonly EulerTourNode visit2;

		/// <summary>
		/// The representation of this edge in the next-higher level. higherEdge is null if this edge is in the highest
		/// level.
		/// </summary>
		public EulerTourEdge higherEdge;

		public EulerTourEdge(EulerTourNode visit1, EulerTourNode visit2)
		{
			this.visit1 = visit1;
			this.visit2 = visit2;
		}
	}

}