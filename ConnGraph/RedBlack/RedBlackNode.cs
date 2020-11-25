using System;
using System.Collections.Generic;

namespace Connectivity.RedBlack
{
	/// <summary>
	/// A node in a red-black tree ( https://en.wikipedia.org/wiki/Red%E2%80%93black_tree ). Compared to a class like Java's
	/// TreeMap, RedBlackNode is a low-level data structure. The internals of a node are exposed as public fields, allowing
	/// clients to directly observe and manipulate the structure of the tree. This gives clients flexibility, although it
	/// also enables them to violate the red-black or BST properties. The RedBlackNode class provides methods for performing
	/// various standard operations, such as insertion and removal.
	/// 
	/// Unlike most implementations of binary search trees, RedBlackNode supports arbitrary augmentation. By subclassing
	/// RedBlackNode, clients can add arbitrary data and augmentation information to each node. For example, if we were to
	/// use a RedBlackNode subclass to implement a sorted set, the subclass would have a field storing an element in the set.
	/// If we wanted to keep track of the number of non-leaf nodes in each subtree, we would store this as a "size" field and
	/// override augment() to update this field. All RedBlackNode methods (such as "insert" and remove()) call augment() as
	/// necessary to correctly maintain the augmentation information, unless otherwise indicated.
	/// 
	/// The values of the tree are stored in the non-leaf nodes. RedBlackNode does not support use cases where values must be
	/// stored in the leaf nodes. It is recommended that all of the leaf nodes in a given tree be the same (black)
	/// RedBlackNode instance, to save space. The root of an empty tree is a leaf node, as opposed to null.
	/// 
	/// For reference, a red-black tree is a binary search tree satisfying the following properties:
	/// 
	/// - Every node is colored red or black.
	/// - The leaf nodes, which are dummy nodes that do not store any values, are colored black.
	/// - The root is black.
	/// - Both children of each red node are black.
	/// - Every path from the root to a leaf contains the same number of black nodes.
	/// </summary>
	/// <author>Bill Jacobs</author>
	public abstract class RedBlackNode<TN> : IComparable<TN> where TN : RedBlackNode<TN>
	{
		/// <summary>
		/// A Comparator that compares Comparable elements using their natural order. </summary>
		private static readonly IComparer<IComparable<object>> _naturalOrder = new ComparatorAnonymousInnerClass();

		private class ComparatorAnonymousInnerClass : IComparer<IComparable<object>>
		{
			public int Compare(IComparable<object> value1, IComparable<object> value2)
			{
				return value1.CompareTo(value2);
			}
		}

		/// <summary>
		/// The parent of this node, if any.  "parent" is null if this is a leaf node. </summary>
		public TN parent;

		/// <summary>
		/// The left child of this node.  "left" is null if this is a leaf node. </summary>
		public TN left;

		/// <summary>
		/// The right child of this node.  "right" is null if this is a leaf node. </summary>
		public TN right;

		/// <summary>
		/// Whether the node is colored red, as opposed to black. </summary>
		public bool isRed;

		/// <summary>
		/// Sets any augmentation information about the subtree rooted at this node that is stored in this node.  For
		/// example, if we augment each node by subtree size (the number of non-leaf nodes in the subtree), this method would
		/// set the size field of this node to be equal to the size field of the left child plus the size field of the right
		/// child plus one.
		///
		/// "Augmentation information" is information that we can compute about a subtree rooted at some node, preferably
		/// based only on the augmentation information in the node's two children and the information in the node.  Examples
		/// of augmentation information are the sum of the values in a subtree and the number of non-leaf nodes in a subtree.
		/// Augmentation information may not depend on the colors of the nodes.
		///
		/// This method returns whether the augmentation information in any of the ancestors of this node might have been
		/// affected by changes in this subtree since the last call to augment().  In the usual case, where the augmentation
		/// information depends only on the information in this node and the augmentation information in its immediate
		/// children, this is equivalent to whether the augmentation information changed as a result of this call to
		/// augment().  For example, in the case of subtree size, this returns whether the value of the size field prior to
		/// calling augment() differed from the size field of the left child plus the size field of the right child plus one.
		/// False positives are permitted.  The return value is unspecified if we have not called augment() on this node
		/// before.
		///
		/// This method may assume that this is not a leaf node.  It may not assume that the augmentation information stored
		/// in any of the tree's nodes is correct.  However, if the augmentation information stored in all of the node's
		/// descendants is correct, then the augmentation information stored in this node must be correct after calling
		/// augment().
		/// </summary>
		public virtual bool Augment()
		{
			return false;
		}

		/// <summary>
		/// Throws a RuntimeException if we detect that this node locally violates any invariants specific to this subclass
		/// of RedBlackNode.  For example, if this stores the size of the subtree rooted at this node, this should throw a
		/// RuntimeException if the size field of this is not equal to the size field of the left child plus the size field
		/// of the right child plus one.  Note that we may call this on a leaf node.
		///
		/// assertSubtreeIsValid() calls assertNodeIsValid() on each node, or at least starts to do so until it detects a
		/// problem.  assertNodeIsValid() should assume the node is in a tree that satisfies all properties common to all
		/// red-black trees, as assertSubtreeIsValid() is responsible for such checks.  assertNodeIsValid() should be
		/// "downward-looking", i.e. it should ignore any information in "parent", and it should be "local", i.e. it should
		/// only check a constant number of descendants.  To include "global" checks, such as verifying the BST property
		/// concerning ordering, override assertSubtreeIsValid().  assertOrderIsValid is useful for checking the BST
		/// property.
		/// </summary>
		public virtual void AssertNodeIsValid()
		{

		}

		/// <summary>
		/// Returns whether this is a leaf node. </summary>
		public virtual bool IsLeaf => left == null;

		/// <summary>
		/// Returns the root of the tree that contains this node. </summary>
		public virtual TN Root()
		{
			TN node = (TN)this;
			while (node.parent != null)
			{
				node = node.parent;
			}
			return node;
		}

		/// <summary>
		/// Returns the first node in the subtree rooted at this node, if any. </summary>
		public virtual TN Min()
		{
			if (IsLeaf)
			{
				return default;
			}
			TN node = (TN)this;
			while (!node.left.IsLeaf)
			{
				node = node.left;
			}
			return node;
		}

		/// <summary>
		/// Returns the last node in the subtree rooted at this node, if any. </summary>
		public virtual TN Max()
		{
			if (IsLeaf)
			{
				return default;
			}
			TN node = (TN)this;
			while (!node.right.IsLeaf)
			{
				node = node.right;
			}
			return node;
		}

		/// <summary>
		/// Returns the node immediately before this in the tree that contains this node, if any. </summary>
		public virtual TN Predecessor()
		{
			if (!left.IsLeaf)
			{
				TN node;
				for (node = left; !node.right.IsLeaf; node = node.right)
				{
					;
				}
				return node;
			}
			else if (parent == null)
			{
				return default;
			}
			else
			{
				TN node = (TN)this;
				while (node.parent != null && node.parent.left == node)
				{
					node = node.parent;
				}
				return node.parent;
			}
		}

		/// <summary>
		/// Returns the node immediately after this in the tree that contains this node, if any. </summary>
		public virtual TN Successor()
		{
			if (!right.IsLeaf)
			{
				TN node;
				for (node = right; !node.left.IsLeaf; node = node.left)
				{
					;
				}
				return node;
			}
			else if (parent == null)
			{
				return default;
			}
			else
			{
				TN node = (TN)this;
				while (node.parent != null && node.parent.right == node)
				{
					node = node.parent;
				}
				return node.parent;
			}
		}

		/// <summary>
		/// Performs a left rotation about this node. This method assumes that !isLeaf() && !right.isLeaf(). It calls
		/// augment() on this node and on its resulting parent. However, it does not call augment() on any of the resulting
		/// parent's ancestors, because that is normally the responsibility of the caller. </summary>
		/// <returns> The return value from calling augment() on the resulting parent. </returns>
		public virtual bool RotateLeft()
		{
			if (IsLeaf || right.IsLeaf)
			{
				throw new ArgumentException("The node or its right child is a leaf");
			}
			TN newParent = right;
			right = newParent.left;
			TN nThis = (TN)this;
			if (!right.IsLeaf)
			{
				right.parent = nThis;
			}
			newParent.parent = parent;
			parent = newParent;
			newParent.left = nThis;
			if (newParent.parent != null)
			{
				if (newParent.parent.left == this)
				{
					newParent.parent.left = newParent;
				}
				else
				{
					newParent.parent.right = newParent;
				}
			}
			Augment();
			return newParent.Augment();
		}

		/// <summary>
		/// Performs a right rotation about this node. This method assumes that !isLeaf() && !left.isLeaf(). It calls
		/// augment() on this node and on its resulting parent. However, it does not call augment() on any of the resulting
		/// parent's ancestors, because that is normally the responsibility of the caller. </summary>
		/// <returns> The return value from calling augment() on the resulting parent. </returns>
		public virtual bool RotateRight()
		{
			if (IsLeaf || left.IsLeaf)
			{
				throw new ArgumentException("The node or its left child is a leaf");
			}
			TN newParent = left;
			left = newParent.right;
			TN nThis = (TN)this;
			if (!left.IsLeaf)
			{
				left.parent = nThis;
			}
			newParent.parent = parent;
			parent = newParent;
			newParent.right = nThis;
			if (newParent.parent != null)
			{
				if (newParent.parent.left == this)
				{
					newParent.parent.left = newParent;
				}
				else
				{
					newParent.parent.right = newParent;
				}
			}
			Augment();
			return newParent.Augment();
		}

		/// <summary>
		/// Performs red-black insertion fixup.  To be more precise, this fixes a tree that satisfies all of the requirements
		/// of red-black trees, except that this may be a red child of a red node, and if this is the root, the root may be
		/// red.  node.isRed must initially be true.  This method assumes that this is not a leaf node.  The method performs
		/// any rotations by calling rotateLeft() and rotateRight().  This method is more efficient than fixInsertion if
		/// "augment" is false or augment() might return false. </summary>
		/// <param name="augment"> Whether to set the augmentation information for "node" and its ancestors, by calling augment(). </param>
		public virtual void FixInsertionWithoutGettingRoot(bool augment)
		{
			if (!isRed)
			{
				throw new ArgumentException("The node must be red");
			}
			bool changed = augment;
			if (augment)
			{
				Augment();
			}

			RedBlackNode<TN> node = this;
			while (node.parent != null && node.parent.isRed)
			{
				TN parent = node.parent;
				TN grandparent = parent.parent;
				if (grandparent.left.isRed && grandparent.right.isRed)
				{
					grandparent.left.isRed = false;
					grandparent.right.isRed = false;
					grandparent.isRed = true;

					if (changed)
					{
						changed = parent.Augment();
						if (changed)
						{
							changed = grandparent.Augment();
						}
					}
					node = grandparent;
				}
				else
				{
					if (parent.left == node)
					{
						if (grandparent.right == parent)
						{
							parent.RotateRight();
							node = parent;
							parent = node.parent;
						}
					}
					else if (grandparent.left == parent)
					{
						parent.RotateLeft();
						node = parent;
						parent = node.parent;
					}

					if (parent.left == node)
					{
						bool grandparentChanged = grandparent.RotateRight();
						if (augment)
						{
							changed = grandparentChanged;
						}
					}
					else
					{
						bool grandparentChanged = grandparent.RotateLeft();
						if (augment)
						{
							changed = grandparentChanged;
						}
					}

					parent.isRed = false;
					grandparent.isRed = true;
					node = parent;
					break;
				}
			}

			if (node.parent == null)
			{
				node.isRed = false;
			}
			if (changed)
			{
				for (node = node.parent; node != null; node = node.parent)
				{
					if (!node.Augment())
					{
						break;
					}
				}
			}
		}

		/// <summary>
		/// Performs red-black insertion fixup.  To be more precise, this fixes a tree that satisfies all of the requirements
		/// of red-black trees, except that this may be a red child of a red node, and if this is the root, the root may be
		/// red.  node.isRed must initially be true.  This method assumes that this is not a leaf node.  The method performs
		/// any rotations by calling rotateLeft() and rotateRight().  This method is more efficient than fixInsertion() if
		/// augment() might return false.
		/// </summary>
		public virtual void FixInsertionWithoutGettingRoot()
		{
			FixInsertionWithoutGettingRoot(true);
		}

		/// <summary>
		/// Performs red-black insertion fixup.  To be more precise, this fixes a tree that satisfies all of the requirements
		/// of red-black trees, except that this may be a red child of a red node, and if this is the root, the root may be
		/// red.  node.isRed must initially be true.  This method assumes that this is not a leaf node.  The method performs
		/// any rotations by calling rotateLeft() and rotateRight(). </summary>
		/// <param name="augment"> Whether to set the augmentation information for "node" and its ancestors, by calling augment(). </param>
		/// <returns> The root of the resulting tree. </returns>
		public virtual TN FixInsertion(bool augment)
		{
			FixInsertionWithoutGettingRoot(augment);
			return Root();
		}

		/// <summary>
		/// Performs red-black insertion fixup.  To be more precise, this fixes a tree that satisfies all of the requirements
		/// of red-black trees, except that this may be a red child of a red node, and if this is the root, the root may be
		/// red.  node.isRed must initially be true.  This method assumes that this is not a leaf node.  The method performs
		/// any rotations by calling rotateLeft() and rotateRight(). </summary>
		/// <returns> The root of the resulting tree. </returns>
		public virtual TN FixInsertion()
		{
			FixInsertionWithoutGettingRoot(true);
			return Root();
		}

		/// <summary>
		/// Returns a Comparator that compares instances of N using their natural order, as in N.compareTo. </summary>
		private IComparer<TN> NaturalOrder()
		{
			return (IComparer<TN>)_naturalOrder;
		}

		/// <summary>
		/// Inserts the specified node into the tree rooted at this node. Assumes this is the root. We treat newNode as a
		/// solitary node that does not belong to any tree, and we ignore its initial "parent", "left", "right", and isRed
		/// fields.
		///
		/// If it is not efficient or convenient to find the location for a node using a Comparator, then you should manually
		/// add the node to the appropriate location, color it red, and call fixInsertion().
		/// </summary>
		/// <param name="newNode"> The node to insert. </param>
		/// <param name="allowDuplicates"> Whether to insert newNode if there is an equal node in the tree. To check whether we
		///     inserted newNode, check whether newNode.parent is null and the return value differs from newNode. </param>
		/// <param name="comparator"> A comparator indicating where to put the node. If this is null, we use the nodes' natural
		///     order, as in N.compareTo. If you are passing null, then you must override the compareTo method, because the
		///     default implementation requires the nodes to already be in the same tree. </param>
		/// <returns> The root of the resulting tree. </returns>
//JAVA TO C# CONVERTER TODO TASK: There is no C# equivalent to the Java 'super' constraint:
//ORIGINAL LINE: public N insert(N newNode, boolean allowDuplicates, java.util.Comparator<? super N> comparator)
		public virtual TN Insert(TN newNode, bool allowDuplicates, IComparer<TN> comparator)
		{
			if (parent != null)
			{
				throw new ArgumentException("This is not the root of a tree");
			}
			TN nThis = (TN)this;
			if (IsLeaf)
			{
				newNode.isRed = false;
				newNode.left = nThis;
				newNode.right = nThis;
				newNode.parent = null;
				newNode.Augment();
				return newNode;
			}
			if (comparator == null)
			{
				comparator = NaturalOrder();
			}

			TN node = nThis;
			int comparison;
			while (true)
			{
				comparison = comparator.Compare(newNode, node);
				if (comparison < 0)
				{
					if (!node.left.IsLeaf)
					{
						node = node.left;
					}
					else
					{
						newNode.left = node.left;
						newNode.right = node.left;
						node.left = newNode;
						newNode.parent = node;
						break;
					}
				}
				else if (comparison > 0 || allowDuplicates)
				{
					if (!node.right.IsLeaf)
					{
						node = node.right;
					}
					else
					{
						newNode.left = node.right;
						newNode.right = node.right;
						node.right = newNode;
						newNode.parent = node;
						break;
					}
				}
				else
				{
					newNode.parent = null;
					return nThis;
				}
			}
			newNode.isRed = true;
			return newNode.FixInsertion();
		}

		/// <summary>
		/// Moves this node to its successor's former position in the tree and vice versa, i.e. sets the "left", "right",
		/// "parent", and isRed fields of each.  This method assumes that this is not a leaf node. </summary>
		/// <returns> The node with which we swapped. </returns>
		private TN SwapWithSuccessor()
		{
			TN replacement = Successor();
			bool oldReplacementIsRed = replacement.isRed;
			TN oldReplacementLeft = replacement.left;
			TN oldReplacementRight = replacement.right;
			TN oldReplacementParent = replacement.parent;

			replacement.isRed = isRed;
			replacement.left = left;
			replacement.right = right;
			replacement.parent = parent;
			if (parent != null)
			{
				if (parent.left == this)
				{
					parent.left = replacement;
				}
				else
				{
					parent.right = replacement;
				}
			}

			TN nThis = (TN)this;
			isRed = oldReplacementIsRed;
			left = oldReplacementLeft;
			right = oldReplacementRight;
			if (oldReplacementParent == this)
			{
				parent = replacement;
				parent.right = nThis;
			}
			else
			{
				parent = oldReplacementParent;
				parent.left = nThis;
			}

			replacement.right.parent = replacement;
			if (!replacement.left.IsLeaf)
			{
				replacement.left.parent = replacement;
			}
			if (!right.IsLeaf)
			{
				right.parent = nThis;
			}
			return replacement;
		}

		/// <summary>
		/// Performs red-black deletion fixup.  To be more precise, this fixes a tree that satisfies all of the requirements
		/// of red-black trees, except that all paths from the root to a leaf that pass through the sibling of this node have
		/// one fewer black node than all other root-to-leaf paths.  This method assumes that this is not a leaf node.
		/// </summary>
		private void FixSiblingDeletion()
		{
			RedBlackNode<TN> sibling = this;
			bool changed = true;
			bool haveAugmentedParent = false;
			bool haveAugmentedGrandparent = false;
			while (true)
			{
				TN parent1 = sibling.parent;
				if (sibling.isRed)
				{
					parent1.isRed = true;
					sibling.isRed = false;
					if (parent1.left == sibling)
					{
						changed = parent1.RotateRight();
						sibling = parent1.left;
					}
					else
					{
						changed = parent1.RotateLeft();
						sibling = parent1.right;
					}
					haveAugmentedParent = true;
					haveAugmentedGrandparent = true;
				}
				else if (!sibling.left.isRed && !sibling.right.isRed)
				{
					sibling.isRed = true;
					if (parent1.isRed)
					{
						parent1.isRed = false;
						break;
					}
					else
					{
						if (changed && !haveAugmentedParent)
						{
							changed = parent1.Augment();
						}
						TN grandparent = parent1.parent;
						if (grandparent == null)
						{
							break;
						}
						else if (grandparent.left == parent1)
						{
							sibling = grandparent.right;
						}
						else
						{
							sibling = grandparent.left;
						}
						haveAugmentedParent = haveAugmentedGrandparent;
						haveAugmentedGrandparent = false;
					}
				}
				else
				{
					if (sibling == parent1.left)
					{
						if (!sibling.left.isRed)
						{
							sibling.RotateLeft();
							sibling = sibling.parent;
						}
					}
					else if (!sibling.right.isRed)
					{
						sibling.RotateRight();
						sibling = sibling.parent;
					}
					sibling.isRed = parent1.isRed;
					parent1.isRed = false;
					if (sibling == parent1.left)
					{
						sibling.left.isRed = false;
						changed = parent1.RotateRight();
					}
					else
					{
						sibling.right.isRed = false;
						changed = parent1.RotateLeft();
					}
					haveAugmentedParent = haveAugmentedGrandparent;
					haveAugmentedGrandparent = false;
					break;
				}
			}

			// Update augmentation info
			TN parent2 = sibling.parent;
			if (changed && parent2 != null)
			{
				if (!haveAugmentedParent)
				{
					changed = parent2.Augment();
				}
				if (changed && parent2.parent != null)
				{
					parent2 = parent2.parent;
					if (!haveAugmentedGrandparent)
					{
						changed = parent2.Augment();
					}
					if (changed)
					{
						for (parent2 = parent2.parent; parent2 != null; parent2 = parent2.parent)
						{
							if (!parent2.Augment())
							{
								break;
							}
						}
					}
				}
			}
		}

		/// <summary>
		/// Removes this node from the tree that contains it.  The effect of this method on the fields of this node is
		/// unspecified.  This method assumes that this is not a leaf node.  This method is more efficient than remove() if
		/// augment() might return false.
		///
		/// If the node has two children, we begin by moving the node's successor to its former position, by changing the
		/// successor's "left", "right", "parent", and isRed fields.
		/// </summary>
		public virtual void RemoveWithoutGettingRoot()
		{
			if (IsLeaf)
			{
				throw new ArgumentException("Attempted to remove a leaf node");
			}
			TN replacement;
			if (left.IsLeaf || right.IsLeaf)
			{
				replacement = default;
			}
			else
			{
				replacement = SwapWithSuccessor();
			}

			TN child;
			if (!left.IsLeaf)
			{
				child = left;
			}
			else if (!right.IsLeaf)
			{
				child = right;
			}
			else
			{
				child = default;
			}

			if (child != null)
			{
				// Replace this node with its child
				child.parent = parent;
				if (parent != null)
				{
					if (parent.left == this)
					{
						parent.left = child;
					}
					else
					{
						parent.right = child;
					}
				}
				child.isRed = false;

				if (child.parent != null)
				{
					TN parent;
					for (parent = child.parent; parent != null; parent = parent.parent)
					{
						if (!parent.Augment())
						{
							break;
						}
					}
				}
			}
			else if (parent != null)
			{
				// Replace this node with a leaf node
				TN leaf = left;
				TN parent = this.parent;
				TN sibling;
				if (parent.left == this)
				{
					parent.left = leaf;
					sibling = parent.right;
				}
				else
				{
					parent.right = leaf;
					sibling = parent.left;
				}

				if (!isRed)
				{
					RedBlackNode<TN> siblingNode = sibling;
					siblingNode.FixSiblingDeletion();
				}
				else
				{
					while (parent != null)
					{
						if (!parent.Augment())
						{
							break;
						}
						parent = parent.parent;
					}
				}
			}

			if (replacement != null)
			{
				replacement.Augment();
				for (TN parent = replacement.parent; parent != null; parent = parent.parent)
				{
					if (!parent.Augment())
					{
						break;
					}
				}
			}

			// Clear any previously existing links, so that we're more likely to encounter an exception if we attempt to
			// access the removed node
			parent = default;
			left = default;
			right = default;
			isRed = true;
		}

		/// <summary>
		/// Removes this node from the tree that contains it.  The effect of this method on the fields of this node is
		/// unspecified.  This method assumes that this is not a leaf node.
		///
		/// If the node has two children, we begin by moving the node's successor to its former position, by changing the
		/// successor's "left", "right", "parent", and isRed fields.
		/// </summary>
		/// <returns> The root of the resulting tree. </returns>
		public virtual TN Remove()
		{
			if (IsLeaf)
			{
				throw new ArgumentException("Attempted to remove a leaf node");
			}

			// Find an arbitrary non-leaf node in the tree other than this node
			TN node;
			if (parent != null)
			{
				node = parent;
			}
			else if (!left.IsLeaf)
			{
				node = left;
			}
			else if (!right.IsLeaf)
			{
				node = right;
			}
			else
			{
				return left;
			}

			RemoveWithoutGettingRoot();
			return node.Root();
		}

		/// <summary>
		/// Returns the root of a perfectly height-balanced subtree containing the next "size" (non-leaf) nodes from
		/// "iterator", in iteration order.  This method is responsible for setting the "left", "right", "parent", and isRed
		/// fields of the nodes, and calling augment() as appropriate.  It ignores the initial values of the "left", "right",
		/// "parent", and isRed fields. </summary>
		/// <param name="iterator"> The nodes. </param>
		/// <param name="size"> The number of nodes. </param>
		/// <param name="height"> The "height" of the subtree's root node above the deepest leaf in the tree that contains it.  Since
		///     insertion fixup is slow if there are too many red nodes and deleteion fixup is slow if there are too few red
		///     nodes, we compromise and have red nodes at every fourth level.  We color a node red iff its "height" is equal
		///     to 1 mod 4. </param>
		/// <param name="leaf"> The leaf node. </param>
		/// <returns> The root of the subtree. </returns>
		private static T CreateTree<T, T1>(IEnumerator<T1> iterator, int size, int height, T leaf) where T : RedBlackNode<T> where T1 : T
		{
			if (size == 0)
			{
				return leaf;
			}
			else
			{
				T left = CreateTree(iterator, (size - 1) / 2, height - 1, leaf);
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
				iterator.MoveNext();
				T node = iterator.Current;
				T right = CreateTree(iterator, size / 2, height - 1, leaf);

				node.isRed = height % 4 == 1;
				node.left = left;
				node.right = right;
				if (!left.IsLeaf)
				{
					left.parent = node;
				}
				if (!right.IsLeaf)
				{
					right.parent = node;
				}

				node.Augment();
				return node;
			}
		}

		/// <summary>
		/// Returns the root of a perfectly height-balanced tree containing the specified nodes, in iteration order. This
		/// method is responsible for setting the "left", "right", "parent", and isRed fields of the nodes (excluding
		/// "leaf"), and calling augment() as appropriate. It ignores the initial values of the "left", "right", "parent",
		/// and isRed fields. </summary>
		/// <param name="nodes"> The nodes. </param>
		/// <param name="leaf"> The leaf node. </param>
		/// <returns> The root of the tree. </returns>
		public static T CreateTree<T, TNode>(ICollection<TNode> nodes, T leaf) where T : RedBlackNode<T> where TNode : T
		{
			int size = nodes.Count;
			if (size == 0)
			{
				return leaf;
			}

			int height = 0;
			for (int subtreeSize = size; subtreeSize > 0; subtreeSize /= 2)
			{
				height++;
			}

			T node = CreateTree(nodes.GetEnumerator(), size, height, leaf);
			node.parent = null;
			node.isRed = false;
			return node;
		}

		/// <summary>
		/// Concatenates to the end of the tree rooted at this node.  To be precise, given that all of the nodes in this
		/// precede the node "pivot", which precedes all of the nodes in "last", this returns the root of a tree containing
		/// all of these nodes.  This method destroys the trees rooted at "this" and "last".  We treat "pivot" as a solitary
		/// node that does not belong to any tree, and we ignore its initial "parent", "left", "right", and isRed fields.
		/// This method assumes that this node and "last" are the roots of their respective trees.
		///
		/// This method takes O(log N) time.  It is more efficient than inserting "pivot" and then calling concatenate(last).
		/// It is considerably more efficient than inserting "pivot" and all of the nodes in "last".
		/// </summary>
		public virtual TN Concatenate(TN last, TN pivot)
		{
			// If the black height of "first", where first = this, is less than or equal to that of "last", starting at the
			// root of "last", we keep going left until we reach a black node whose black height is equal to that of
			// "first".  Then, we make "pivot" the parent of that node and of "first", coloring it red, and perform
			// insertion fixup on the pivot.  If the black height of "first" is greater than that of "last", we do the
			// mirror image of the above.

			if (this.parent != null)
			{
				throw new ArgumentException("This is not the root of a tree");
			}
			if (last.parent != null)
			{
				throw new ArgumentException("\"last\" is not the root of a tree");
			}

			// Compute the black height of the trees
			int firstBlackHeight = 0;
			TN first = (TN)this;
			for (TN node = first; node != null; node = node.right)
			{
				if (!node.isRed)
				{
					firstBlackHeight++;
				}
			}
			int lastBlackHeight = 0;
			for (TN node = last; node != null; node = node.right)
			{
				if (!node.isRed)
				{
					lastBlackHeight++;
				}
			}

			// Identify the children and parent of pivot
			TN firstChild = first;
			TN lastChild = last;
			TN parent;
			if (firstBlackHeight <= lastBlackHeight)
			{
				parent = default;
				int blackHeight = lastBlackHeight;
				while (blackHeight > firstBlackHeight)
				{
					if (!lastChild.isRed)
					{
						blackHeight--;
					}
					parent = lastChild;
					lastChild = lastChild.left;
				}
				if (lastChild.isRed)
				{
					parent = lastChild;
					lastChild = lastChild.left;
				}
			}
			else
			{
				parent = default;
				int blackHeight = firstBlackHeight;
				while (blackHeight > lastBlackHeight)
				{
					if (!firstChild.isRed)
					{
						blackHeight--;
					}
					parent = firstChild;
					firstChild = firstChild.right;
				}
				if (firstChild.isRed)
				{
					parent = firstChild;
					firstChild = firstChild.right;
				}
			}

			// Add "pivot" to the tree
			pivot.isRed = true;
			pivot.parent = parent;
			if (parent != null)
			{
				if (firstBlackHeight < lastBlackHeight)
				{
					parent.left = pivot;
				}
				else
				{
					parent.right = pivot;
				}
			}
			pivot.left = firstChild;
			if (!firstChild.IsLeaf)
			{
				firstChild.parent = pivot;
			}
			pivot.right = lastChild;
			if (!lastChild.IsLeaf)
			{
				lastChild.parent = pivot;
			}

			// Perform insertion fixup
			return pivot.FixInsertion();
		}

		/// <summary>
		/// Concatenates the tree rooted at "last" to the end of the tree rooted at this node.  To be precise, given that all
		/// of the nodes in this precede all of the nodes in "last", this returns the root of a tree containing all of these
		/// nodes.  This method destroys the trees rooted at "this" and "last".  It assumes that this node and "last" are the
		/// roots of their respective trees.  This method takes O(log N) time.  It is considerably more efficient than
		/// inserting all of the nodes in "last".
		/// </summary>
		public virtual TN Concatenate(TN last)
		{
			if (parent != null || last.parent != null)
			{
				throw new ArgumentException("The node is not the root of a tree");
			}
			if (IsLeaf)
			{
				return last;
			}
			else if (last.IsLeaf)
			{
				TN nThis = (TN)this;
				return nThis;
			}
			else
			{
				TN node = last.Min();
				last = node.Remove();
				return Concatenate(last, node);
			}
		}

		/// <summary>
		/// Splits the tree rooted at this node into two trees, so that the first element of the return value is the root of
		/// a tree consisting of the nodes that were before the specified node, and the second element of the return value is
		/// the root of a tree consisting of the nodes that were equal to or after the specified node. This method is
		/// destructive, meaning it does not preserve the original tree. It assumes that this node is the root and is in the
		/// same tree as splitNode. It takes O(log N) time. It is considerably more efficient than removing all of the
		/// nodes at or after splitNode and then creating a new tree from those nodes. </summary>
		/// <param name="The"> node at which to split the tree. </param>
		/// <returns> An array consisting of the resulting trees. </returns>
		public virtual TN[] Split(TN splitNode)
		{
			// To split the tree, we accumulate a pre-split tree and a post-split tree.  We walk down the tree toward the
			// position where we are splitting.  Whenever we go left, we concatenate the right subtree with the post-split
			// tree, and whenever we go right, we concatenate the pre-split tree with the left subtree.  We use the
			// concatenation algorithm described in concatenate(Object, Object).  For the pivot, we use the last node where
			// we went left in the case of a left move, and the last node where we went right in the case of a right move.
			//
			// The method uses the following variables:
			//
			// node: The current node in our walk down the tree.
			// first: A node on the right spine of the pre-split tree.  At the beginning of each iteration, it is the black
			//     node with the same black height as "node".  If the pre-split tree is empty, this is null instead.
			// firstParent: The parent of "first".  If the pre-split tree is empty, this is null.  Otherwise, this is the
			//     same as first.parent, unless first.isLeaf().
			// firstPivot: The node where we last went right, i.e. the next node to use as a pivot when concatenating with
			//     the pre-split tree.
			// advanceFirst: Whether to set "first" to be its next black descendant at the end of the loop.
			// last, lastParent, lastPivot, advanceLast: Analogous to "first", firstParent, firstPivot, and advanceFirst,
			//     but for the post-split tree.
			if (parent != null)
			{
				throw new ArgumentException("This is not the root of a tree");
			}
			if (IsLeaf || splitNode.IsLeaf)
			{
				throw new ArgumentException("The root or the split node is a leaf");
			}

			// Create an array containing the path from the root to splitNode
			int depth = 1;
			TN parent3;
			for (parent3 = splitNode; parent3.parent != null; parent3 = parent3.parent)
			{
				depth++;
			}
			if (parent3 != this)
			{
				throw new ArgumentException("The split node does not belong to this tree");
			}
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in C#:
//ORIGINAL LINE: RedBlackNode<?>[] path = new RedBlackNode<?>[depth];
			TN[] path = new TN[depth];
			for (parent3 = splitNode; parent3 != null; parent3 = parent3.parent)
			{
				depth--;
				path[depth] = parent3;
			}
			TN node = (TN)this;
			TN first = default(TN);
			TN firstParent = default(TN);
			TN last = default(TN);
			TN lastParent = default(TN);
			TN firstPivot = default(TN);
			TN lastPivot = default(TN);
			while (!node.IsLeaf)
			{
				bool advanceFirst = !node.isRed && firstPivot != null;
				bool advanceLast = !node.isRed && lastPivot != null;
				if ((depth + 1 < path.Length && path[depth + 1] == node.left) || depth + 1 == path.Length)
				{
					// Left move
					if (lastPivot == null)
					{
						// The post-split tree is empty
						last = node.right;
						last.parent = null;
						if (last.isRed)
						{
							last.isRed = false;
							lastParent = last;
							last = last.left;
						}
					}
					else
					{
						// Concatenate node.right and the post-split tree
						if (node.right.isRed)
						{
							node.right.isRed = false;
						}
						else if (!node.isRed)
						{
							lastParent = last;
							last = last.left;
							if (last.isRed)
							{
								lastParent = last;
								last = last.left;
							}
							advanceLast = false;
						}
						lastPivot.isRed = true;
						lastPivot.parent = lastParent;
						if (lastParent != null)
						{
							lastParent.left = lastPivot;
						}
						lastPivot.left = node.right;
						if (!lastPivot.left.IsLeaf)
						{
							lastPivot.left.parent = lastPivot;
						}
						lastPivot.right = last;
						if (!last.IsLeaf)
						{
							last.parent = lastPivot;
						}
						last = lastPivot.left;
						lastParent = lastPivot;
						lastPivot.FixInsertionWithoutGettingRoot(false);
					}
					lastPivot = node;
					node = node.left;
				}
				else
				{
					// Right move
					if (firstPivot == null)
					{
						// The pre-split tree is empty
						first = node.left;
						first.parent = null;
						if (first.isRed)
						{
							first.isRed = false;
							firstParent = first;
							first = first.right;
						}
					}
					else
					{
						// Concatenate the post-split tree and node.left
						if (node.left.isRed)
						{
							node.left.isRed = false;
						}
						else if (!node.isRed)
						{
							firstParent = first;
							first = first.right;
							if (first.isRed)
							{
								firstParent = first;
								first = first.right;
							}
							advanceFirst = false;
						}
						firstPivot.isRed = true;
						firstPivot.parent = firstParent;
						if (firstParent != null)
						{
							firstParent.right = firstPivot;
						}
						firstPivot.right = node.left;
						if (!firstPivot.right.IsLeaf)
						{
							firstPivot.right.parent = firstPivot;
						}
						firstPivot.left = first;
						if (!first.IsLeaf)
						{
							first.parent = firstPivot;
						}
						first = firstPivot.right;
						firstParent = firstPivot;
						firstPivot.FixInsertionWithoutGettingRoot(false);
					}
					firstPivot = node;
					node = node.right;
				}

				depth++;

				// Update "first" and "last" to be the nodes at the proper black height
				if (advanceFirst)
				{
					firstParent = first;
					first = first.right;
					if (first.isRed)
					{
						firstParent = first;
						first = first.right;
					}
				}
				if (advanceLast)
				{
					lastParent = last;
					last = last.left;
					if (last.isRed)
					{
						lastParent = last;
						last = last.left;
					}
				}
			}

			// Add firstPivot to the pre-split tree
			TN leaf = node;
			if (first == null)
			{
				first = leaf;
			}
			else
			{
				firstPivot.isRed = true;
				firstPivot.parent = firstParent;
				if (firstParent != null)
				{
					firstParent.right = firstPivot;
				}
				firstPivot.left = leaf;
				firstPivot.right = leaf;
				firstPivot.FixInsertionWithoutGettingRoot(false);
				for (first = firstPivot; first.parent != null; first = first.parent)
				{
					first.Augment();
				}
				first.Augment();
			}

			// Add lastPivot to the post-split tree
			lastPivot.isRed = true;
			lastPivot.parent = lastParent;
			if (lastParent != null)
			{
				lastParent.left = lastPivot;
			}
			lastPivot.left = leaf;
			lastPivot.right = leaf;
			lastPivot.FixInsertionWithoutGettingRoot(false);
			for (last = lastPivot; last.parent != null; last = last.parent)
			{
				last.Augment();
			}
			last.Augment();

			TN[] result = (TN[])Array.CreateInstance(GetType(), 2);
			result[0] = first;
			result[1] = last;
			return result;
		}

		/// <summary>
		/// Returns the lowest common ancestor of this node and "other" - the node that is an ancestor of both and is not the
		/// parent of a node that is an ancestor of both. Assumes that this is in the same tree as "other". Assumes that
		/// neither "this" nor "other" is a leaf node. This method may return "this" or "other".
		///
		/// Note that while it is possible to compute the lowest common ancestor in O(P) time, where P is the length of the
		/// path from this node to "other", the "lca" method is not guaranteed to take O(P) time. If your application
		/// requires this, then you should write your own lowest common ancestor method.
		/// </summary>
		public virtual TN Lca(TN other)
		{
			if (IsLeaf || other.IsLeaf)
			{
				throw new ArgumentException("One of the nodes is a leaf node");
			}

			// Compute the depth of each node
			int depth = 0;
			for (TN parent1 = parent; parent1 != null; parent1 = parent1.parent)
			{
				depth++;
			}
			int otherDepth = 0;
			for (TN parent2 = other.parent; parent2 != null; parent2 = parent2.parent)
			{
				otherDepth++;
			}

			// Go up to nodes of the same depth
			TN parent3 = (TN)this;
			TN otherParent = other;
			if (depth <= otherDepth)
			{
				for (int i = otherDepth; i > depth; i--)
				{
					otherParent = otherParent.parent;
				}
			}
			else
			{
				for (int i = depth; i > otherDepth; i--)
				{
					parent3 = parent3.parent;
				}
			}

			// Find the LCA
			while (parent3 != otherParent)
			{
				parent3 = parent3.parent;
				otherParent = otherParent.parent;
			}
			if (parent3 != null)
			{
				return parent3;
			}
			else
			{
				throw new ArgumentException("The nodes do not belong to the same tree");
			}
		}

		/// <summary>
		/// Returns an integer comparing the position of this node in the tree that contains it with that of "other". Returns
		/// a negative number if this is earlier, a positive number if this is later, and 0 if this is at the same position.
		/// Assumes that this is in the same tree as "other". Assumes that neither "this" nor "other" is a leaf node.
		///
		/// The base class's implementation takes O(log N) time. If a RedBlackNode subclass stores a value used to order the
		/// nodes, then it could override compareTo to compare the nodes' values, which would take O(1) time.
		///
		/// Note that while it is possible to compare the positions of two nodes in O(P) time, where P is the length of the
		/// path from this node to "other", the default implementation of compareTo is not guaranteed to take O(P) time. If
		/// your application requires this, then you should write your own comparison method.
		/// </summary>
		public virtual int CompareTo(TN other)
		{
			if (IsLeaf || other.IsLeaf)
			{
				throw new ArgumentException("One of the nodes is a leaf node");
			}

			// The algorithm operates as follows: compare the depth of this node to that of "other".  If the depth of
			// "other" is greater, keep moving up from "other" until we find the ancestor at the same depth.  Then, keep
			// moving up from "this" and from that node until we reach the lowest common ancestor.  The node that arrived
			// from the left child of the common ancestor is earlier.  The algorithm is analogous if the depth of "other" is
			// not greater.
			if (this == other)
			{
				return 0;
			}

			// Compute the depth of each node
			int depth = 0;
			RedBlackNode<TN> parent;
			for (parent = this; parent.parent != null; parent = parent.parent)
			{
				depth++;
			}
			int otherDepth = 0;
			TN otherParent;
			for (otherParent = other; otherParent.parent != null; otherParent = otherParent.parent)
			{
				otherDepth++;
			}

			// Go up to nodes of the same depth
			if (depth < otherDepth)
			{
				otherParent = other;
				for (int i = otherDepth - 1; i > depth; i--)
				{
					otherParent = otherParent.parent;
				}
				if (otherParent.parent != this)
				{
					otherParent = otherParent.parent;
				}
				else if (left == otherParent)
				{
					return 1;
				}
				else
				{
					return -1;
				}
				parent = this;
			}
			else if (depth > otherDepth)
			{
				parent = this;
				for (int i = depth - 1; i > otherDepth; i--)
				{
					parent = parent.parent;
				}
				if (parent.parent != other)
				{
					parent = parent.parent;
				}
				else if (other.left == parent)
				{
					return -1;
				}
				else
				{
					return 1;
				}
				otherParent = other;
			}
			else
			{
				parent = this;
				otherParent = other;
			}

			// Keep going up until we reach the lowest common ancestor
			while (parent.parent != otherParent.parent)
			{
				parent = parent.parent;
				otherParent = otherParent.parent;
			}
			if (parent.parent == null)
			{
				throw new ArgumentException("The nodes do not belong to the same tree");
			}
			if (parent.parent.left == parent)
			{
				return -1;
			}
			else
			{
				return 1;
			}
		}

		/// <summary>
		/// Throws a RuntimeException if the RedBlackNode fields of this are not correct for a leaf node. </summary>
		private void AssertIsValidLeaf()
		{
			if (left != null || right != null || parent != null || isRed)
			{
				throw new Exception("A leaf node's \"left\", \"right\", \"parent\", or isRed field is incorrect");
			}
		}

		/// <summary>
		/// Throws a RuntimeException if the subtree rooted at this node does not satisfy the red-black properties, excluding
		/// the requirement that the root be black, or it contains a repeated node other than a leaf node. </summary>
		/// <param name="blackHeight"> The required number of black nodes in each path from this to a leaf node, including this and
		///     the leaf node. </param>
		/// <param name="visited"> The nodes we have reached thus far, other than leaf nodes. This method adds the non-leaf nodes in
		///     the subtree rooted at this node to "visited". </param>
		private void AssertSubtreeIsValidRedBlack(int blackHeight, ISet<Reference<TN>> visited)
		{
			TN nThis = (TN)this;
			if (left == null || right == null)
			{
				AssertIsValidLeaf();
				if (blackHeight != 1)
				{
					throw new Exception("Not all root-to-leaf paths have the same number of black nodes");
				}
				return;
			}
			else if (!visited.Add(new Reference<TN>(nThis)))
			{
				throw new Exception("The tree contains a repeated non-leaf node");
			}
			else
			{
				int childBlackHeight;
				if (isRed)
				{
					if ((!left.IsLeaf && left.isRed) || (!right.IsLeaf && right.isRed))
					{
						throw new Exception("A red node has a red child");
					}
					childBlackHeight = blackHeight;
				}
				else if (blackHeight == 0)
				{
					throw new Exception("Not all root-to-leaf paths have the same number of black nodes");
				}
				else
				{
					childBlackHeight = blackHeight - 1;
				}

				if (!left.IsLeaf && left.parent != this)
				{
					throw new Exception("left.parent != this");
				}
				if (!right.IsLeaf && right.parent != this)
				{
					throw new Exception("right.parent != this");
				}
				RedBlackNode<TN> leftNode = left;
				RedBlackNode<TN> rightNode = right;
				leftNode.AssertSubtreeIsValidRedBlack(childBlackHeight, visited);
				rightNode.AssertSubtreeIsValidRedBlack(childBlackHeight, visited);
			}
		}

		/// <summary>
		/// Calls assertNodeIsValid() on every node in the subtree rooted at this node. </summary>
		private void AssertNodesAreValid()
		{
			AssertNodeIsValid();
			if (left != null)
			{
				RedBlackNode<TN> leftNode = left;
				RedBlackNode<TN> rightNode = right;
				leftNode.AssertNodesAreValid();
				rightNode.AssertNodesAreValid();
			}
		}

		/// <summary>
		/// Throws a RuntimeException if the subtree rooted at this node is not a valid red-black tree, e.g. if a red node
		/// has a red child or it contains a non-leaf node "node" for which node.left.parent != node. (If parent != null,
		/// it's okay if isRed is true.) This method is useful for debugging. See also assertSubtreeIsValid().
		/// </summary>
		public virtual void AssertSubtreeIsValidRedBlack()
		{
			if (IsLeaf)
			{
				AssertIsValidLeaf();
			}
			else
			{
				if (parent == null && isRed)
				{
					throw new Exception("The root is red");
				}

				// Compute the black height of the tree
				ISet<Reference<TN>> nodes = new HashSet<Reference<TN>>();
				int blackHeight = 0;
				TN node = (TN)this;
				while (node != null)
				{
					if (!nodes.Add(new Reference<TN>(node)))
					{
						throw new Exception("The tree contains a repeated non-leaf node");
					}
					if (!node.isRed)
					{
						blackHeight++;
					}
					node = node.left;
				}

				AssertSubtreeIsValidRedBlack(blackHeight, new HashSet<Reference<TN>>());
			}
		}

		/// <summary>
		/// Throws a RuntimeException if we detect a problem with the subtree rooted at this node, such as a red child of a
		/// red node or a non-leaf descendant "node" for which node.left.parent != node.  This method is useful for
		/// debugging.  RedBlackNode subclasses may want to override assertSubtreeIsValid() to call assertOrderIsValid.
		/// </summary>
		public virtual void AssertSubtreeIsValid()
		{
			AssertSubtreeIsValidRedBlack();
			AssertNodesAreValid();
		}

		/// <summary>
		/// Throws a RuntimeException if the nodes in the subtree rooted at this node are not in the specified order or they
		/// do not lie in the specified range.  Assumes that the subtree rooted at this node is a valid binary tree, i.e. it
		/// has no repeated nodes other than leaf nodes. </summary>
		/// <param name="comparator"> A comparator indicating how the nodes should be ordered. </param>
		/// <param name="start"> The lower limit for nodes in the subtree, if any. </param>
		/// <param name="end"> The upper limit for nodes in the subtree, if any. </param>
//JAVA TO C# CONVERTER TODO TASK: There is no C# equivalent to the Java 'super' constraint:
//ORIGINAL LINE: private void assertOrderIsValid(java.util.Comparator<? super N> comparator, N start, N end)
		private void AssertOrderIsValid(IComparer<TN> comparator, TN start, TN end)
		{
			if (!IsLeaf)
			{
				TN nThis = (TN)this;
				if (start != null && comparator.Compare(nThis, start) < 0)
				{
					throw new Exception("The nodes are not ordered correctly");
				}
				if (end != null && comparator.Compare(nThis, end) > 0)
				{
					throw new Exception("The nodes are not ordered correctly");
				}
				RedBlackNode<TN> leftNode = left;
				RedBlackNode<TN> rightNode = right;
				leftNode.AssertOrderIsValid(comparator, start, nThis);
				rightNode.AssertOrderIsValid(comparator, nThis, end);
			}
		}

		/// <summary>
		/// Throws a RuntimeException if the nodes in the subtree rooted at this node are not in the specified order.
		/// Assumes that this is a valid binary tree, i.e. there are no repeated nodes other than leaf nodes.  This method is
		/// useful for debugging.  RedBlackNode subclasses may want to override assertSubtreeIsValid() to call
		/// assertOrderIsValid. </summary>
		/// <param name="comparator"> A comparator indicating how the nodes should be ordered.  If this is null, we use the nodes'
		///     natural order, as in N.compareTo. </param>
//JAVA TO C# CONVERTER TODO TASK: There is no C# equivalent to the Java 'super' constraint:
//ORIGINAL LINE: public void assertOrderIsValid(java.util.Comparator<? super N> comparator)
		public virtual void AssertOrderIsValid(IComparer<TN> comparator)
		{
			if (comparator == null)
			{
				comparator = NaturalOrder();
			}
			AssertOrderIsValid(comparator, null, null);
		}
	}

}