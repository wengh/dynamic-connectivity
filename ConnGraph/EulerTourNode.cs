using Connectivity.RedBlack;

namespace Connectivity
{
	/// <summary>
	/// A node in an Euler tour tree for ConnGraph (at some particular level i). See the comments for the implementation of
	/// ConnGraph.
	/// </summary>
	internal class EulerTourNode : RedBlackNode<EulerTourNode>
	{
		/// <summary>
		/// The dummy leaf node. </summary>
		public static readonly EulerTourNode leaf = new EulerTourNode(null, null);

		/// <summary>
		/// The vertex this node visits. </summary>
		public readonly EulerTourVertex vertex;

		/// <summary>
		/// The number of nodes in the subtree rooted at this node. </summary>
		public int size;

		/// <summary>
		/// Whether the subtree rooted at this node contains a node "node" for which
		/// node.vertex.arbitraryNode == node && node.vertex.graphListHead != null.
		/// </summary>
		public bool hasGraphEdge;

		/// <summary>
		/// Whether the subtree rooted at this node contains a node "node" for which
		/// node.vertex.arbitraryNode == node && node.vertex.forestListHead != null.
		/// </summary>
		public bool hasForestEdge;

		/// <summary>
		/// The combining function for combining user-provided augmentations. augmentationFunc is null if this node is not in
		/// the highest level.
		/// </summary>
		public readonly IAugmentation augmentationFunc;

		/// <summary>
		/// The combined augmentation for the subtree rooted at this node. This is the result of combining the augmentation
		/// values node.vertex.augmentation for all nodes "node" in the subtree rooted at this node for which
		/// node.vertex.arbitraryVisit == node, using augmentationFunc. This is null if hasAugmentation is false.
		/// </summary>
		public object augmentation;

		/// <summary>
		/// Whether the subtree rooted at this node contains at least one augmentation value. This indicates whether there is
		/// some node "node" in the subtree rooted at this node for which node.vertex.hasAugmentation is true and
		/// node.vertex.arbitraryVisit == node.
		/// </summary>
		public bool hasAugmentation;

		public EulerTourNode(EulerTourVertex vertex, IAugmentation augmentationFunc)
		{
			this.vertex = vertex;
			this.augmentationFunc = augmentationFunc;
		}

		/// <summary>
		/// Like augment(), but only updates the augmentation fields hasGraphEdge and hasForestEdge. </summary>
		public virtual bool AugmentFlags()
		{
			bool newHasGraphEdge = left.hasGraphEdge || right.hasGraphEdge || (vertex.arbitraryVisit == this && vertex.graphListHead != null);
			bool newHasForestEdge = left.hasForestEdge || right.hasForestEdge || (vertex.arbitraryVisit == this && vertex.forestListHead != null);
			if (newHasGraphEdge == hasGraphEdge && newHasForestEdge == hasForestEdge)
			{
				return false;
			}
			else
			{
				hasGraphEdge = newHasGraphEdge;
				hasForestEdge = newHasForestEdge;
				return true;
			}
		}

		public override bool Augment()
		{
			int newSize = left.size + right.size + 1;
			bool augmentedFlags = AugmentFlags();

			object newAugmentation = null;
			bool newHasAugmentation = false;
			if (augmentationFunc != null)
			{
				if (left.hasAugmentation)
				{
					newAugmentation = left.augmentation;
					newHasAugmentation = true;
				}
				if (vertex.hasAugmentation && vertex.arbitraryVisit == this)
				{
					if (newHasAugmentation)
					{
						newAugmentation = augmentationFunc.Combine(newAugmentation, vertex.augmentation);
					}
					else
					{
						newAugmentation = vertex.augmentation;
						newHasAugmentation = true;
					}
				}
				if (right.hasAugmentation)
				{
					if (newHasAugmentation)
					{
						newAugmentation = augmentationFunc.Combine(newAugmentation, right.augmentation);
					}
					else
					{
						newAugmentation = right.augmentation;
						newHasAugmentation = true;
					}
				}
			}

			if (newSize == size && !augmentedFlags && hasAugmentation == newHasAugmentation && (newAugmentation != null ? newAugmentation.Equals(augmentation) : augmentation == null))
			{
				return false;
			}
			else
			{
				size = newSize;
				augmentation = newAugmentation;
				hasAugmentation = newHasAugmentation;
				return true;
			}
		}
	}

}