using System;
using System.Collections.Generic;

namespace Connectivity
{

	/// <summary>
	/// Implements an undirected graph with dynamic connectivity. It supports adding and removing edges and determining
	/// whether two vertices are connected - whether there is a path between them. Adding and removing edges take O(log^2 N)
	/// amortized time with high probability, while checking whether two vertices are connected takes O(log N) time with high
	/// probability. It uses O(V log V + E) space, where V is the number of vertices and E is the number of edges. Note that
	/// a ConnVertex may appear in multiple ConnGraphs, with a different set of adjacent vertices in each graph.
	/// 
	/// ConnGraph optionally supports arbitrary augmentation. Each vertex may have an associated augmentation, or value.
	/// Given a vertex V, ConnGraph can quickly report the result of combining the augmentations of all of the vertices in
	/// the connected component containing V, using a combining function provided to the constructor. For example, if a
	/// ConnGraph represents a game map, then given the location of the player, we can quickly determine the amount of gold
	/// the player can access, or the strongest monster that can reach him. Augmentation does not affect the running time or
	/// space of ConnGraph in terms of big O notation, assuming the augmentation function takes a constant amount of time and
	/// the augmentation takes a constant amount of space. Retrieving the combined augmentation for a connected component
	/// takes O(log N) time with high probability. (Although ConnGraph does not directly support augmenting edges, this can
	/// also be accomplished, by imputing each edge's augmentation to an adjacent vertex.)
	/// 
	/// When a vertex no longer has any adjacent edges, and it has no augmentation information, ConnGraph stops keeping track
	/// of the vertex. This reduces the time and space bounds of the ConnGraph, and it enables the ConnVertex to be garbage
	/// collected. If you know you are finished with a vertex, and that vertex has an augmentation, then you should call
	/// removeVertexAugmentation on the vertex, so that the graph can release it.
	/// 
	/// As a side note, it would be more proper if ConnGraph had a generic type parameter indicating the type of the
	/// augmentation values. However, it is expected that it is more common not to use augmentation, so by not using a type
	/// parameter, we make usage of the ConnGraph class more convenient and less confusing in the common case.
	/// </summary>
	/* ConnGraph is implemented using a data structure described in
	 * http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.89.919&rep=rep1&type=pdf (Holm, de Lichtenberg, and Thorup
	 * (1998): Poly-Logarithmic Deterministic Fully-Dynamic Algorithms for Connectivity, Minimum Spanning Tree, 2-Edge, and
	 * Biconnectivity). However, ConnGraph does not include the optimization of using a B-tree in the top level, so queries
	 * take O(log N) time rather than O(log N / log log N) time.
	 *
	 * This implementation is actually based on a slightly modified description of the data structure given in
	 * http://ocw.mit.edu/courses/electrical-engineering-and-computer-science/6-851-advanced-data-structures-spring-2012/lecture-videos/session-20-dynamic-graphs-ii/ .
	 * The description in the video differs from the data structure in the paper in that the levels are numbered in reverse
	 * order, the constraint on tree sizes is different, and the augmentation uses booleans in place of edges. In addition,
	 * the video defines subgraphs G_i. The change in the constraint on tree sizes is beneficial because it makes it easier
	 * to delete vertices.
	 *
	 * Note that the data structure described in the video is faulty. In the procedure for deleting an edge, it directs us
	 * to push down some edges. When we push an edge from level i to level i - 1, we would need to add the edge to
	 * F_{i - 1}, if the endpoints were not already connected in G_{i - 1}. However, this would violate the invariant that
	 * F_i be a superset of F_{i - 1}. To fix this problem, before searching for a replacement edge in level i, ConnGraph
	 * first pushes all level-i edges in the relevant tree down to level i - 1 and adds them to F_{i - 1}, as in the
	 * original paper. That way, when we subsequently push down edges, we can safely add them to G_{i - 1} without also
	 * adding them to F_{i - 1}. In order to do this efficiently, each vertex stores a second adjacency list, consisting of
	 * the level-i edges that are in F_i. In addition, we augment each Euler tour tree node with an a second boolean,
	 * indicating whether the subtree rooted at the node contains a canonical visit to a vertex with at least one level-i
	 * edge that is in F_i.
	 *
	 * The implementation of rerooting an Euler tour tree described in the video lecture appears to be incorrect as well. It
	 * breaks the references to the vertices' first and last visits. To fix this, we do not store references to the
	 * vertices' first and last visits. Instead, we have each vertex store a reference to an arbitrary visit to that vertex.
	 * We also maintain edge objects for each of the edges in the Euler tours. Each such edge stores a pointer to the two
	 * visits that precede the traversal of the edge in the Euler tour. These do not change when we perform a reroot. The
	 * remove edge operation then requires a pointer to the edge object, rather than pointers to the vertices. Given the
	 * edge object, we can splice out the range of nodes between the two visits that precede the edge.
	 *
	 * Rather than explicitly giving each edge a level number, the level numbers are implicit through links from each level
	 * to the level below it. For purposes of analysis, the level number of the top level is equal to
	 * maxLogVertexCountSinceRebuild, the ceiling of log base 2 of the maximum number of vertices in the graph since the
	 * last rebuild operation. Once the ratio between the maximum number of vertices since the last rebuild and the current
	 * number of vertices becomes large enough, we rebuild the data structure. This ensures that the level number of the top
	 * level is O(log V).
	 *
	 * Most methods' time bounds are probabilistic. For example, "connected" takes O(log N) time with high probability. The
	 * reason they are probabilistic is that they involve hash lookups, using the vertexInfo and VertexInfo.edges hash maps.
	 * Given that each ConnVertex has a random hash code, it is easy to demonstrate that lookups take O(1) expected time.
	 * Furthermore, I claim that they take O(log N / log log N) time with high probability. This claim is sufficient to
	 * establish that all time bounds that are at least O(log N / log log N) if we exclude hash lookups can be sustained if
	 * we add the qualifier "with high probability."
	 *
	 * This claim is based on information presented in
	 * https://ocw.mit.edu/courses/electrical-engineering-and-computer-science/6-851-advanced-data-structures-spring-2012/lecture-videos/session-10-dictionaries/ .
	 * According to that video, in a hash map with chaining, if the hash function is totally random, then the longest chain
	 * length is O(log N / log log N) with high probability. A totally random hash function is a slightly different concept
	 * than having ConnVertex.hashCode() return a random value, due to the particular definition of "hash function" used in
	 * the video. Nevertheless, the analysis is the same. A random hashCode() implementation ultimately results in
	 * independently hashing each entry to a random bucket, which is equivalent to a totally random hash function.
	 *
	 * However, the claim depends on certain features of the implementation of HashMap, gleaned from reading the source
	 * code. In particular, it assumes that HashMap resolves collisions using chaining. (Newer versions of Java sometimes
	 * store the entries that hash to the same bucket in binary search trees rather than linked lists, but this can't hurt
	 * the asymptotic performance.) Note that the implementation of HashMap transforms the return value of hashCode(), by
	 * "spreading" the higher-order bits to lower-order positions. However, this transform is a permutation of the integers.
	 * If the input to a transform is selected uniformly at random, and the transform is a permutation, than the output also
	 * has a uniform random distribution.
	 */
	public class ConnGraph : IConnGraph
	{
		/// <summary>
		/// The difference between ceiling of log base 2 of the maximum number of vertices in the graph since the last call
		/// to rebuild() or clear() and ceiling of log base 2 of the current number of vertices, at or above which we call
		/// rebuild(). (There is special handling for 0 vertices.)
		/// </summary>
		private const int RebuildChange = 2;

		/// <summary>
		/// The maximum number of vertices we can store in a ConnGraph. This is limited by the fact that EulerTourNode.size
		/// is an int. Since the size of an Euler tour tree is one less than twice the number of vertices in the tree, the
		/// number of vertices may be at most (int)((((long)Integer.MAX_VALUE) + 1) / 2).
		/// 
		/// Of course, we could simply change the "size" field to be a long. But more fundamentally, the number of vertices
		/// is limited by the fact that vertexInfo and VertexInfo.edges use HashMaps. Using a HashMap becomes problematic at
		/// around Integer.MAX_VALUE entries. HashMap buckets entries based on 32-bit hash codes, so in principle, it can
		/// only hash the entries to at most 2^32 buckets. In order to support a significantly greater limit on the number of
		/// vertices, we would need to use a more elaborate mapping approach.
		/// </summary>
		private static readonly int _maxVertexCount = 1 << 30;

		/// <summary>
		/// The augmentation function for the graph, if any. </summary>
		private readonly IAugmentation _augmentation;

		/// <summary>
		/// A map from each vertex in this graph to information about the vertex in this graph. If a vertex has no adjacent
		/// edges and no associated augmentation, we remove it from vertexInfo, to save time and space. Lookups take O(1)
		/// expected time and O(log N / log log N) time with high probability, because vertexInfo is a HashMap, and
		/// ConnVertex.hashCode() returns a random integer.
		/// </summary>
		private IDictionary<ConnVertex, VertexInfo> _vertexInfo = new Dictionary<ConnVertex, VertexInfo>();

		/// <summary>
		/// Ceiling of log base 2 of the maximum number of vertices in this graph since the last rebuild. This is 0 if that
		/// number is 0.
		/// </summary>
		private int _maxLogVertexCountSinceRebuild;

		/// <summary>
		/// The maximum number of entries in vertexInfo since the last time we copied that field to a new HashMap. We do this
		/// when the number of vertices drops sufficiently, in order to limit space usage. (The capacity of a HashMap is not
		/// automatically reduced as the number of entries decreases, so we have to limit space usage manually.)
		/// </summary>
		private int _maxVertexInfoSize;

		/// <summary>
		/// Constructs a new ConnGraph with no augmentation. </summary>
		public ConnGraph()
		{
			_augmentation = null;
		}

		/// <summary>
		/// Constructs an augmented ConnGraph, using the specified function to combine augmentation values. </summary>
		public ConnGraph(IAugmentation augmentation)
		{
			_augmentation = augmentation;
		}

		/// <summary>
		/// Equivalent implementation is contractual. </summary>
		private void AssertIsAugmented()
		{
			if (_augmentation == null)
			{
				throw new Exception("You may only call augmentation-related methods on ConnGraph if the graph is augmented, i.e. if an " + "Augmentation was passed to the constructor");
			}
		}

		/// <summary>
		/// Returns the VertexInfo containing information about the specified vertex in this graph. If the vertex is not in
		/// this graph (i.e. it does not have an entry in vertexInfo), this method adds it to the graph, and creates a
		/// VertexInfo object for it.
		/// </summary>
		private VertexInfo EnsureInfo(ConnVertex vertex)
		{
			if (_vertexInfo.TryGetValue(vertex, out var info))
			{
				return info;
			}

			if (_vertexInfo.Count == _maxVertexCount)
			{
				throw new Exception("Sorry, ConnGraph has too many vertices to perform this operation. ConnGraph does not support " + "storing more than ~2^30 vertices at a time.");
			}

			EulerTourVertex eulerTourVertex = new EulerTourVertex();
			EulerTourNode node = new EulerTourNode(eulerTourVertex, _augmentation);
			eulerTourVertex.arbitraryVisit = node;
			node.left = EulerTourNode.leaf;
			node.right = EulerTourNode.leaf;
			node.Augment();

			info = new VertexInfo(eulerTourVertex);
			_vertexInfo[vertex] = info;
			if (_vertexInfo.Count > 1 << _maxLogVertexCountSinceRebuild)
			{
				_maxLogVertexCountSinceRebuild++;
			}
			_maxVertexInfoSize = Math.Max(_maxVertexInfoSize, _vertexInfo.Count);
			return info;
		}

		/// <summary>
		/// Takes the specified vertex out of this graph. We should call this method as soon as a vertex does not have any
		/// adjacent edges and does not have any augmentation information. This method assumes that the vertex is currently
		/// in the graph.
		/// </summary>
		private void Remove(ConnVertex vertex)
		{
			_vertexInfo.Remove(vertex);
			if (4 * _vertexInfo.Count <= _maxVertexInfoSize && _maxVertexInfoSize > 12)
			{
				// The capacity of a HashMap is not automatically reduced as the number of entries decreases. To avoid
				// violating our O(V log V + E) space guarantee, we copy vertexInfo to a new HashMap, which will have a
				// suitable capacity.
				_vertexInfo = new Dictionary<ConnVertex, VertexInfo>(_vertexInfo);
				_maxVertexInfoSize = _vertexInfo.Count;
			}
			if (_vertexInfo.Count << RebuildChange <= 1 << _maxLogVertexCountSinceRebuild)
			{
				Rebuild();
			}
		}

		/// <summary>
		/// Collapses an adjacency list (either graphListHead or forestListHead) for an EulerTourVertex into the adjacency
		/// list for an EulerTourVertex that represents the same underlying ConnVertex, but at a higher level. This has the
		/// effect of prepending the list for the lower level to the beginning of the list for the higher level, and
		/// replacing all links to the lower-level vertex in the ConnEdges with links to the higher-level vertex. </summary>
		/// <param name="head"> The first node in the list for the higher-level vertex. </param>
		/// <param name="lowerHead"> The first node in the list for the lower-level vertex. </param>
		/// <param name="vertex"> The higher-level vertex. </param>
		/// <param name="lowerVertex"> The lower-level vertex. </param>
		/// <returns> The head of the combined linked list. </returns>
		private ConnEdge CollapseEdgeList(ConnEdge head, ConnEdge lowerHead, EulerTourVertex vertex, EulerTourVertex lowerVertex)
		{
			if (lowerHead == null)
			{
				return head;
			}

			ConnEdge prevLowerEdge = null;
			ConnEdge lowerEdge = lowerHead;
			while (lowerEdge != null)
			{
				prevLowerEdge = lowerEdge;
				if (lowerEdge.vertex1 == lowerVertex)
				{
					lowerEdge.vertex1 = vertex;
					lowerEdge = lowerEdge.next1;
				}
				else
				{
					lowerEdge.vertex2 = vertex;
					lowerEdge = lowerEdge.next2;
				}
			}

			if (prevLowerEdge.vertex1 == vertex)
			{
				prevLowerEdge.next1 = head;
			}
			else
			{
				prevLowerEdge.next2 = head;
			}
			if (head != null)
			{
				if (head.vertex1 == vertex)
				{
					head.prev1 = prevLowerEdge;
				}
				else
				{
					head.prev2 = prevLowerEdge;
				}
			}
			return lowerHead;
		}

		/// <summary>
		/// Equivalent implementation is contractual.
		/// 
		/// This method is useful for when an EulerTourVertex's lists (graphListHead or forestListHead) or arbitrary visit
		/// change, as these affect the hasGraphEdge and hasForestEdge augmentations.
		/// </summary>
		private void AugmentAncestorFlags(EulerTourNode node)
		{
			for (EulerTourNode parent = node; parent != null; parent = parent.parent)
			{
				if (!parent.AugmentFlags())
				{
					break;
				}
			}
		}

		/// <summary>
		/// Rebuilds the data structure so that the number of levels is at most the ceiling of log base 2 of the number of
		/// vertices in the graph (or zero in the case of zero vertices). The current implementation of rebuild() takes
		/// O(V + E) time, assuming a constant difference between maxLogVertexCountSinceRebuild and the result of the
		/// logarithm.
		/// </summary>
		private void Rebuild()
		{
			// Rebuild the graph by collapsing the top deleteCount + 1 levels into the top level

			if (_vertexInfo.Count == 0)
			{
				_maxLogVertexCountSinceRebuild = 0;
				return;
			}
			int deleteCount = 0;
			while (2 * _vertexInfo.Count <= 1 << _maxLogVertexCountSinceRebuild)
			{
				_maxLogVertexCountSinceRebuild--;
				deleteCount++;
			}
			if (deleteCount == 0)
			{
				return;
			}

			foreach (VertexInfo info in _vertexInfo.Values)
			{
				EulerTourVertex vertex = info.vertex;
				EulerTourVertex lowerVertex = vertex;
				for (int i = 0; i < deleteCount; i++)
				{
					lowerVertex = lowerVertex.lowerVertex;
					if (lowerVertex == null)
					{
						break;
					}

					vertex.graphListHead = CollapseEdgeList(vertex.graphListHead, lowerVertex.graphListHead, vertex, lowerVertex);
					if (lowerVertex.forestListHead != null)
					{
						// Change the eulerTourEdge links
						ConnEdge lowerEdge = lowerVertex.forestListHead;
						while (lowerEdge != null)
						{
							if (lowerEdge.vertex1 == lowerVertex)
							{
								// We'll address this edge when we visit lowerEdge.vertex2
								lowerEdge = lowerEdge.next1;
							}
							else
							{
								EulerTourEdge edge = lowerEdge.eulerTourEdge.higherEdge;
								for (int j = 0; j < i; j++)
								{
									edge = edge.higherEdge;
								}
								lowerEdge.eulerTourEdge = edge;
								lowerEdge = lowerEdge.next2;
							}
						}

						vertex.forestListHead = CollapseEdgeList(vertex.forestListHead, lowerVertex.forestListHead, vertex, lowerVertex);
					}
				}

				if (lowerVertex != null)
				{
					lowerVertex = lowerVertex.lowerVertex;
				}
				vertex.lowerVertex = lowerVertex;
				if (lowerVertex != null)
				{
					lowerVertex.higherVertex = vertex;
				}
				AugmentAncestorFlags(vertex.arbitraryVisit);
			}
		}

		/// <summary>
		/// Adds the specified edge to the graph adjacency list of edge.vertex1, as in EulerTourVertex.graphListHead.
		/// Assumes it is not currently in any lists, except possibly the graph adjacency list of edge.vertex2.
		/// </summary>
		private void AddToGraphLinkedList1(ConnEdge edge)
		{
			edge.prev1 = null;
			edge.next1 = edge.vertex1.graphListHead;
			if (edge.next1 != null)
			{
				if (edge.next1.vertex1 == edge.vertex1)
				{
					edge.next1.prev1 = edge;
				}
				else
				{
					edge.next1.prev2 = edge;
				}
			}
			edge.vertex1.graphListHead = edge;
		}

		/// <summary>
		/// Adds the specified edge to the graph adjacency list of edge.vertex2, as in EulerTourVertex.graphListHead.
		/// Assumes it is not currently in any lists, except possibly the graph adjacency list of edge.vertex1.
		/// </summary>
		private void AddToGraphLinkedList2(ConnEdge edge)
		{
			edge.prev2 = null;
			edge.next2 = edge.vertex2.graphListHead;
			if (edge.next2 != null)
			{
				if (edge.next2.vertex1 == edge.vertex2)
				{
					edge.next2.prev1 = edge;
				}
				else
				{
					edge.next2.prev2 = edge;
				}
			}
			edge.vertex2.graphListHead = edge;
		}

		/// <summary>
		/// Adds the specified edge to the graph adjacency lists of edge.vertex1 and edge.vertex2, as in
		/// EulerTourVertex.graphListHead. Assumes it is not currently in any lists.
		/// </summary>
		private void AddToGraphLinkedLists(ConnEdge edge)
		{
			AddToGraphLinkedList1(edge);
			AddToGraphLinkedList2(edge);
		}

		/// <summary>
		/// Adds the specified edge to the forest adjacency lists of edge.vertex1 and edge.vertex2, as in
		/// EulerTourVertex.forestListHead. Assumes it is not currently in any lists.
		/// </summary>
		private void AddToForestLinkedLists(ConnEdge edge)
		{
			edge.prev1 = null;
			edge.next1 = edge.vertex1.forestListHead;
			if (edge.next1 != null)
			{
				if (edge.next1.vertex1 == edge.vertex1)
				{
					edge.next1.prev1 = edge;
				}
				else
				{
					edge.next1.prev2 = edge;
				}
			}
			edge.vertex1.forestListHead = edge;

			edge.prev2 = null;
			edge.next2 = edge.vertex2.forestListHead;
			if (edge.next2 != null)
			{
				if (edge.next2.vertex1 == edge.vertex2)
				{
					edge.next2.prev1 = edge;
				}
				else
				{
					edge.next2.prev2 = edge;
				}
			}
			edge.vertex2.forestListHead = edge;
		}

		/// <summary>
		/// Removes the specified edge from an adjacency list of edge.vertex1, as in graphListHead and forestListHead.
		/// Assumes it is initially in exactly one of the lists for edge.vertex1.
		/// </summary>
		private void RemoveFromLinkedList1(ConnEdge edge)
		{
			if (edge.prev1 != null)
			{
				if (edge.prev1.vertex1 == edge.vertex1)
				{
					edge.prev1.next1 = edge.next1;
				}
				else
				{
					edge.prev1.next2 = edge.next1;
				}
			}
			else if (edge == edge.vertex1.graphListHead)
			{
				edge.vertex1.graphListHead = edge.next1;
			}
			else
			{
				edge.vertex1.forestListHead = edge.next1;
			}
			if (edge.next1 != null)
			{
				if (edge.next1.vertex1 == edge.vertex1)
				{
					edge.next1.prev1 = edge.prev1;
				}
				else
				{
					edge.next1.prev2 = edge.prev1;
				}
			}
		}

		/// <summary>
		/// Removes the specified edge from an adjacency list of edge.vertex2, as in graphListHead and forestListHead.
		/// Assumes it is initially in exactly one of the lists for edge.vertex2.
		/// </summary>
		private void RemoveFromLinkedList2(ConnEdge edge)
		{
			if (edge.prev2 != null)
			{
				if (edge.prev2.vertex1 == edge.vertex2)
				{
					edge.prev2.next1 = edge.next2;
				}
				else
				{
					edge.prev2.next2 = edge.next2;
				}
			}
			else if (edge == edge.vertex2.graphListHead)
			{
				edge.vertex2.graphListHead = edge.next2;
			}
			else
			{
				edge.vertex2.forestListHead = edge.next2;
			}
			if (edge.next2 != null)
			{
				if (edge.next2.vertex1 == edge.vertex2)
				{
					edge.next2.prev1 = edge.prev2;
				}
				else
				{
					edge.next2.prev2 = edge.prev2;
				}
			}
		}

		/// <summary>
		/// Removes the specified edge from the adjacency lists of edge.vertex1 and edge.vertex2, as in graphListHead and
		/// forestListHead. Assumes it is initially in exactly one of the lists for edge.vertex1 and exactly one of the lists
		/// for edge.vertex2.
		/// </summary>
		private void RemoveFromLinkedLists(ConnEdge edge)
		{
			RemoveFromLinkedList1(edge);
			RemoveFromLinkedList2(edge);
		}

		/// <summary>
		/// Add an edge between the specified vertices to the Euler tour forest F_i. Assumes that the edge's endpoints are
		/// initially in separate trees. Returns the created edge.
		/// </summary>
		private EulerTourEdge AddForestEdge(EulerTourVertex vertex1, EulerTourVertex vertex2)
		{
			// We need to be careful about where we split and where we add and remove nodes, so as to avoid breaking any
			// EulerTourEdge.visit* fields
			EulerTourNode root = vertex2.arbitraryVisit.Root();
			EulerTourNode max = root.Max();
			if (max.vertex != vertex2)
			{
				// Reroot
				EulerTourNode min = root.Min();
				if (max.vertex.arbitraryVisit == max)
				{
					max.vertex.arbitraryVisit = min;
					AugmentAncestorFlags(min);
					AugmentAncestorFlags(max);
				}
				root = max.Remove();
				EulerTourNode[] splitRoots = root.Split(vertex2.arbitraryVisit);
				root = splitRoots[1].Concatenate(splitRoots[0]);
				EulerTourNode newNode = new EulerTourNode(vertex2, root.augmentationFunc);
				newNode.left = EulerTourNode.leaf;
				newNode.right = EulerTourNode.leaf;
				newNode.isRed = true;
				EulerTourNode parent = root.Max();
				parent.right = newNode;
				newNode.parent = parent;
				root = newNode.FixInsertion();
				max = newNode;
			}

			EulerTourNode[] splitRoots1 = vertex1.arbitraryVisit.Root().Split(vertex1.arbitraryVisit);
			EulerTourNode before = splitRoots1[0];
			EulerTourNode after = splitRoots1[1];
			EulerTourNode newNode1 = new EulerTourNode(vertex1, root.augmentationFunc);
			before.Concatenate(root, newNode1).Concatenate(after);
			return new EulerTourEdge(newNode1, max);
		}

		/// <summary>
		/// Removes the specified edge from the Euler tour forest F_i. </summary>
		private void RemoveForestEdge(EulerTourEdge edge)
		{
			EulerTourNode firstNode;
			EulerTourNode secondNode;
			if (edge.visit1.CompareTo(edge.visit2) < 0)
			{
				firstNode = edge.visit1;
				secondNode = edge.visit2;
			}
			else
			{
				firstNode = edge.visit2;
				secondNode = edge.visit1;
			}

			if (firstNode.vertex.arbitraryVisit == firstNode)
			{
				EulerTourNode successor = secondNode.Successor();
				firstNode.vertex.arbitraryVisit = successor;
				AugmentAncestorFlags(firstNode);
				AugmentAncestorFlags(successor);
			}

			EulerTourNode root = firstNode.Root();
			EulerTourNode[] firstSplitRoots = root.Split(firstNode);
			EulerTourNode before = firstSplitRoots[0];
			EulerTourNode[] secondSplitRoots = firstSplitRoots[1].Split(secondNode.Successor());
			before.Concatenate(secondSplitRoots[1]);
			firstNode.RemoveWithoutGettingRoot();
		}

		/// <summary>
		/// Adds the specified edge to the edge map for srcInfo (srcInfo.edges). Assumes that the edge is not currently in
		/// the map. </summary>
		/// <param name="edge"> The edge. </param>
		/// <param name="srcInfo"> The source vertex's info. </param>
		/// <param name="destVertex"> The destination vertex, i.e. the edge's key in srcInfo.edges. </param>
		private void AddToEdgeMap(ConnEdge edge, VertexInfo srcInfo, ConnVertex destVertex)
		{
			srcInfo.edges[destVertex] = edge;
			if (srcInfo.edges.Count > srcInfo.maxEdgeCountSinceRebuild)
			{
				srcInfo.maxEdgeCountSinceRebuild = srcInfo.edges.Count;
			}
		}

		/// <summary>
		/// Adds an edge between the specified vertices, if such an edge is not already present. Taken together with
		/// removeEdge, this method takes O(log^2 N) amortized time with high probability. </summary>
		/// <returns> Whether there was no edge between the vertices. </returns>
		public virtual bool AddEdge(ConnVertex connVertex1, ConnVertex connVertex2)
		{
			if (connVertex1 == connVertex2)
			{
				throw new ArgumentException("Self-loops are not allowed");
			}
			if (_vertexInfo.Count >= _maxVertexCount - 1)
			{
				throw new Exception("Sorry, ConnGraph has too many vertices to perform this operation. ConnGraph does not support " + "storing more than ~2^30 vertices at a time.");
			}
			VertexInfo info1 = EnsureInfo(connVertex1);
			if (info1.edges.ContainsKey(connVertex2))
			{
				return false;
			}
			VertexInfo info2 = EnsureInfo(connVertex2);

			EulerTourVertex vertex1 = info1.vertex;
			EulerTourVertex vertex2 = info2.vertex;
			ConnEdge edge = new ConnEdge(vertex1, vertex2);

			if (vertex1.arbitraryVisit.Root() == vertex2.arbitraryVisit.Root())
			{
				AddToGraphLinkedLists(edge);
			}
			else
			{
				AddToForestLinkedLists(edge);
				edge.eulerTourEdge = AddForestEdge(vertex1, vertex2);
			}
			AugmentAncestorFlags(vertex1.arbitraryVisit);
			AugmentAncestorFlags(vertex2.arbitraryVisit);

			AddToEdgeMap(edge, info1, connVertex2);
			AddToEdgeMap(edge, info2, connVertex1);
			return true;
		}

		/// <summary>
		/// Returns vertex.lowerVertex. If this is null, ensureLowerVertex sets vertex.lowerVertex to a new vertex and
		/// returns it.
		/// </summary>
		private EulerTourVertex EnsureLowerVertex(EulerTourVertex vertex)
		{
			EulerTourVertex lowerVertex = vertex.lowerVertex;
			if (lowerVertex == null)
			{
				lowerVertex = new EulerTourVertex();
				EulerTourNode lowerNode = new EulerTourNode(lowerVertex, null);
				lowerVertex.arbitraryVisit = lowerNode;
				vertex.lowerVertex = lowerVertex;
				lowerVertex.higherVertex = vertex;

				lowerNode.left = EulerTourNode.leaf;
				lowerNode.right = EulerTourNode.leaf;
				lowerNode.Augment();
			}
			return lowerVertex;
		}

		/// <summary>
		/// Pushes all level-i forest edges in the tree rooted at the specified node down to level i - 1, and adds them to
		/// F_{i - 1}, where i is the level of the tree.
		/// </summary>
		private void PushForestEdges(EulerTourNode root)
		{
			// Iterate over all of the nodes that have hasForestEdge == true
			if (!root.hasForestEdge || root.size == 1)
			{
				return;
			}
			EulerTourNode node;
			for (node = root; node.left.hasForestEdge; node = node.left)
			{
				;
			}
			while (node != null)
			{
				EulerTourVertex vertex = node.vertex;
				ConnEdge edge = vertex.forestListHead;
				if (edge != null)
				{
					EulerTourVertex lowerVertex = EnsureLowerVertex(vertex);
					ConnEdge prevEdge = null;
					while (edge != null)
					{
						if (edge.vertex2 == vertex || edge.vertex2 == lowerVertex)
						{
							// We address this edge when we visit edge.vertex1
							prevEdge = edge;
							edge = edge.next2;
						}
						else
						{
							edge.vertex1 = lowerVertex;
							edge.vertex2 = EnsureLowerVertex(edge.vertex2);
							EulerTourEdge lowerEdge = AddForestEdge(edge.vertex1, edge.vertex2);
							lowerEdge.higherEdge = edge.eulerTourEdge;
							edge.eulerTourEdge = lowerEdge;
							prevEdge = edge;
							edge = edge.next1;
						}
					}

					// Prepend vertex.forestListHead to the beginning of lowerVertex.forestListHead
					if (prevEdge.vertex1 == lowerVertex)
					{
						prevEdge.next1 = lowerVertex.forestListHead;
					}
					else
					{
						prevEdge.next2 = lowerVertex.forestListHead;
					}
					if (lowerVertex.forestListHead != null)
					{
						if (lowerVertex.forestListHead.vertex1 == lowerVertex)
						{
							lowerVertex.forestListHead.prev1 = prevEdge;
						}
						else
						{
							lowerVertex.forestListHead.prev2 = prevEdge;
						}
					}
					lowerVertex.forestListHead = vertex.forestListHead;
					vertex.forestListHead = null;
					AugmentAncestorFlags(lowerVertex.arbitraryVisit);
				}

				// Iterate to the next node with hasForestEdge == true, clearing hasForestEdge as we go
				if (node.right.hasForestEdge)
				{
					for (node = node.right; node.left.hasForestEdge; node = node.left)
					{
						;
					}
				}
				else
				{
					node.hasForestEdge = false;
					while (node.parent != null && node.parent.right == node)
					{
						node = node.parent;
						node.hasForestEdge = false;
					}
					node = node.parent;
				}
			}
		}

		/// <summary>
		/// Searches for a level-i edge connecting a vertex in the tree rooted at the specified node to a vertex in another
		/// tree, where i is the level of the tree. This is a "replacement" edge because it replaces the edge that was
		/// previously connecting the two trees. We push any level-i edges we encounter that do not connect to another tree
		/// down to level i - 1, adding them to G_{i - 1}. This method assumes that root.hasForestEdge is false. </summary>
		/// <param name="root"> The root of the tree. </param>
		/// <returns> The replacement edge, or null if there is no replacement edge. </returns>
		private ConnEdge FindReplacementEdge(EulerTourNode root)
		{
			// Iterate over all of the nodes that have hasGraphEdge == true
			if (!root.hasGraphEdge)
			{
				return null;
			}
			EulerTourNode node;
			for (node = root; node.left.hasGraphEdge; node = node.left)
			{
				;
			}
			while (node != null)
			{
				EulerTourVertex vertex = node.vertex;
				ConnEdge edge = vertex.graphListHead;
				if (edge != null)
				{
					ConnEdge replacementEdge = null;
					ConnEdge prevEdge = null;
					while (edge != null)
					{
						EulerTourVertex adjVertex;
						ConnEdge nextEdge;
						if (edge.vertex1 == vertex)
						{
							adjVertex = edge.vertex2;
							nextEdge = edge.next1;
						}
						else
						{
							adjVertex = edge.vertex1;
							nextEdge = edge.next2;
						}

						if (adjVertex.arbitraryVisit.Root() != root)
						{
							replacementEdge = edge;
							break;
						}

						// Remove the edge from the adjacency list of adjVertex. We will remove it from the adjacency list
						// of "vertex" later.
						if (edge.vertex1 == adjVertex)
						{
							RemoveFromLinkedList1(edge);
						}
						else
						{
							RemoveFromLinkedList2(edge);
						}
						AugmentAncestorFlags(adjVertex.arbitraryVisit);

						// Push the edge down to level i - 1
						edge.vertex1 = EnsureLowerVertex(edge.vertex1);
						edge.vertex2 = EnsureLowerVertex(edge.vertex2);

						// Add the edge to the adjacency list of adjVertex.lowerVertex. We will add it to the adjacency list
						// of lowerVertex later.
						if (edge.vertex1 != vertex.lowerVertex)
						{
							AddToGraphLinkedList1(edge);
						}
						else
						{
							AddToGraphLinkedList2(edge);
						}
						AugmentAncestorFlags(adjVertex.lowerVertex.arbitraryVisit);

						prevEdge = edge;
						edge = nextEdge;
					}

					// Prepend the linked list up to prevEdge to the beginning of vertex.lowerVertex.graphListHead
					if (prevEdge != null)
					{
						EulerTourVertex lowerVertex = vertex.lowerVertex;
						if (prevEdge.vertex1 == lowerVertex)
						{
							prevEdge.next1 = lowerVertex.graphListHead;
						}
						else
						{
							prevEdge.next2 = lowerVertex.graphListHead;
						}
						if (lowerVertex.graphListHead != null)
						{
							if (lowerVertex.graphListHead.vertex1 == lowerVertex)
							{
								lowerVertex.graphListHead.prev1 = prevEdge;
							}
							else
							{
								lowerVertex.graphListHead.prev2 = prevEdge;
							}
						}
						lowerVertex.graphListHead = vertex.graphListHead;
						AugmentAncestorFlags(lowerVertex.arbitraryVisit);
					}
					vertex.graphListHead = edge;
					if (edge == null)
					{
						AugmentAncestorFlags(vertex.arbitraryVisit);
					}
					else if (edge.vertex1 == vertex)
					{
						edge.prev1 = null;
					}
					else
					{
						edge.prev2 = null;
					}

					if (replacementEdge != null)
					{
						return replacementEdge;
					}
				}

				// Iterate to the next node with hasGraphEdge == true. Note that nodes' hasGraphEdge fields can change as we
				// push down edges.
				if (node.right.hasGraphEdge)
				{
					for (node = node.right; node.left.hasGraphEdge; node = node.left)
					{
						;
					}
				}
				else
				{
					while (node.parent != null && (node.parent.right == node || !node.parent.hasGraphEdge))
					{
						node = node.parent;
					}
					node = node.parent;
				}
			}
			return null;
		}

		/// <summary>
		/// Removes the edge from srcInfo to destVertex from the edge map for srcInfo (srcInfo.edges), if it is present.
		/// Returns the edge that we removed, if any.
		/// </summary>
		private ConnEdge RemoveFromEdgeMap(VertexInfo srcInfo, ConnVertex destVertex)
		{
			if (srcInfo.edges.TryGetValue(destVertex, out var edge))
			{
				srcInfo.edges.Remove(destVertex);
				if (4 * srcInfo.edges.Count <= srcInfo.maxEdgeCountSinceRebuild && srcInfo.maxEdgeCountSinceRebuild > 6)
				{
					// The capacity of a HashMap is not automatically reduced as the number of entries decreases. To avoid
					// violating our O(V log V + E) space guarantee, we copy srcInfo.edges to a new HashMap, which will have a
					// suitable capacity.
					srcInfo.edges = new Dictionary<ConnVertex, ConnEdge>(srcInfo.edges);
					srcInfo.maxEdgeCountSinceRebuild = srcInfo.edges.Count;
				}
				return edge;
			}

			return null;
		}

		/// <summary>
		/// Removes the edge between the specified vertices, if there is such an edge. Taken together with addEdge, this
		/// method takes O(log^2 N) amortized time with high probability. </summary>
		/// <returns> Whether there was an edge between the vertices. </returns>
		public virtual bool RemoveEdge(ConnVertex vertex1, ConnVertex vertex2)
		{
			if (vertex1 == vertex2)
			{
				throw new ArgumentException("Self-loops are not allowed");
			}

			if (!_vertexInfo.TryGetValue(vertex1, out var info1))
				return false;

			ConnEdge edge = RemoveFromEdgeMap(info1, vertex2);
			if (edge == null)
			{
				return false;
			}
			VertexInfo info2 = _vertexInfo[vertex2];
			RemoveFromEdgeMap(info2, vertex1);

			RemoveFromLinkedLists(edge);
			AugmentAncestorFlags(edge.vertex1.arbitraryVisit);
			AugmentAncestorFlags(edge.vertex2.arbitraryVisit);

			if (edge.eulerTourEdge != null)
			{
				for (EulerTourEdge levelEdge = edge.eulerTourEdge; levelEdge != null; levelEdge = levelEdge.higherEdge)
				{
					RemoveForestEdge(levelEdge);
				}
				edge.eulerTourEdge = null;

				// Search for a replacement edge
				ConnEdge replacementEdge = null;
				EulerTourVertex levelVertex1 = edge.vertex1;
				EulerTourVertex levelVertex2 = edge.vertex2;
				while (levelVertex1 != null)
				{
					EulerTourNode root1 = levelVertex1.arbitraryVisit.Root();
					EulerTourNode root2 = levelVertex2.arbitraryVisit.Root();

					// Optimization: if hasGraphEdge is false for one of the roots, then there definitely isn't a
					// replacement edge at this level
					if (root1.hasGraphEdge && root2.hasGraphEdge)
					{
						EulerTourNode root;
						if (root1.size < root2.size)
						{
							root = root1;
						}
						else
						{
							root = root2;
						}

						PushForestEdges(root);
						replacementEdge = FindReplacementEdge(root);
						if (replacementEdge != null)
						{
							break;
						}
					}

					// To save space, get rid of trees with one node
					if (root1.size == 1 && levelVertex1.higherVertex != null)
					{
						levelVertex1.higherVertex.lowerVertex = null;
					}
					if (root2.size == 1 && levelVertex2.higherVertex != null)
					{
						levelVertex2.higherVertex.lowerVertex = null;
					}

					levelVertex1 = levelVertex1.higherVertex;
					levelVertex2 = levelVertex2.higherVertex;
				}

				if (replacementEdge != null)
				{
					// Add the replacement edge to all of the forests at or above the current level
					RemoveFromLinkedLists(replacementEdge);
					AddToForestLinkedLists(replacementEdge);
					EulerTourVertex replacementVertex1 = replacementEdge.vertex1;
					EulerTourVertex replacementVertex2 = replacementEdge.vertex2;
					AugmentAncestorFlags(replacementVertex1.arbitraryVisit);
					AugmentAncestorFlags(replacementVertex2.arbitraryVisit);
					EulerTourEdge lowerEdge = null;
					while (replacementVertex1 != null)
					{
						EulerTourEdge levelEdge = AddForestEdge(replacementVertex1, replacementVertex2);
						if (lowerEdge == null)
						{
							replacementEdge.eulerTourEdge = levelEdge;
						}
						else
						{
							lowerEdge.higherEdge = levelEdge;
						}

						lowerEdge = levelEdge;
						replacementVertex1 = replacementVertex1.higherVertex;
						replacementVertex2 = replacementVertex2.higherVertex;
					}
				}
			}

			if (info1.edges.Count == 0 && !info1.vertex.hasAugmentation)
			{
				Remove(vertex1);
			}
			if (info2.edges.Count == 0 && !info2.vertex.hasAugmentation)
			{
				Remove(vertex2);
			}
			return true;
		}

		/// <summary>
		/// Returns whether the specified vertices are connected - whether there is a path between them. Returns true if
		/// vertex1 == vertex2. This method takes O(log N) time with high probability.
		/// </summary>
		public virtual bool IsConnected(ConnVertex vertex1, ConnVertex vertex2)
		{
			if (vertex1 == vertex2)
			{
				return true;
			}

			if (_vertexInfo.TryGetValue(vertex1, out var info1)
			 && _vertexInfo.TryGetValue(vertex2, out var info2))
			{
				return info1.vertex.arbitraryVisit.Root() == info2.vertex.arbitraryVisit.Root();
			}

			return false;
		}

		/// <summary>
		/// Returns the vertices that are directly adjacent to the specified vertex. </summary>
		public virtual ICollection<ConnVertex> AdjacentVertices(ConnVertex vertex)
		{
			if (_vertexInfo.TryGetValue(vertex, out var info))
			{
				return new List<ConnVertex>(info.edges.Keys);
			}
			else
			{
				return new ConnVertex[0];
			}
		}

		/// <summary>
		/// Sets the augmentation associated with the specified vertex. This method takes O(log N) time with high
		/// probability.
		/// 
		/// Note that passing a null value for the second argument is not the same as removing the augmentation. For that,
		/// you need to call removeVertexAugmentation.
		/// </summary>
		/// <returns> The augmentation that was previously associated with the vertex. Returns null if it did not have any
		///     associated augmentation. </returns>
		public virtual object SetVertexAugmentation(ConnVertex connVertex, object vertexAugmentation)
		{
			AssertIsAugmented();
			EulerTourVertex vertex = EnsureInfo(connVertex).vertex;
			object oldAugmentation = vertex.augmentation;
			if (!vertex.hasAugmentation || (!vertexAugmentation?.Equals(oldAugmentation) ?? oldAugmentation != null))
			{
				vertex.augmentation = vertexAugmentation;
				vertex.hasAugmentation = true;
				for (EulerTourNode node = vertex.arbitraryVisit; node != null; node = node.parent)
				{
					if (!node.Augment())
					{
						break;
					}
				}
			}
			return oldAugmentation;
		}

		/// <summary>
		/// Removes any augmentation associated with the specified vertex. This method takes O(log N) time with high
		/// probability. </summary>
		/// <returns> The augmentation that was previously associated with the vertex. Returns null if it did not have any
		///     associated augmentation. </returns>
		public virtual object RemoveVertexAugmentation(ConnVertex connVertex)
		{
			AssertIsAugmented();
			VertexInfo info = _vertexInfo[connVertex];
			if (info == null)
			{
				return null;
			}

			EulerTourVertex vertex = info.vertex;
			object oldAugmentation = vertex.augmentation;
			if (info.edges.Count == 0)
			{
				Remove(connVertex);
			}
			else if (vertex.hasAugmentation)
			{
				vertex.augmentation = null;
				vertex.hasAugmentation = false;
				for (EulerTourNode node = vertex.arbitraryVisit; node != null; node = node.parent)
				{
					if (!node.Augment())
					{
						break;
					}
				}
			}
			return oldAugmentation;
		}

		/// <summary>
		/// Returns the augmentation associated with the specified vertex. Returns null if it does not have any associated
		/// augmentation. At present, this method takes constant expected time. Contrast with getComponentAugmentation.
		/// </summary>
		public virtual object GetVertexAugmentation(ConnVertex vertex)
		{
			AssertIsAugmented();
			if (_vertexInfo.TryGetValue(vertex, out var info))
			{
				return info.vertex.augmentation;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns the result of combining the augmentations associated with all of the vertices in the connected component
		/// containing the specified vertex. Returns null if none of those vertices has any associated augmentation. This
		/// method takes O(log N) time with high probability.
		/// </summary>
		public virtual object GetComponentAugmentation(ConnVertex vertex)
		{
			AssertIsAugmented();
			if (_vertexInfo.TryGetValue(vertex, out var info))
			{
				return info.vertex.arbitraryVisit.Root().augmentation;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Returns whether the specified vertex has any associated augmentation. At present, this method takes constant
		/// expected time. Contrast with componentHasAugmentation.
		/// </summary>
		public virtual bool VertexHasAugmentation(ConnVertex vertex)
		{
			AssertIsAugmented();
			if (_vertexInfo.TryGetValue(vertex, out var info))
			{
				return info.vertex.hasAugmentation;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Returns whether any of the vertices in the connected component containing the specified vertex has any associated
		/// augmentation. This method takes O(log N) time with high probability.
		/// </summary>
		public virtual bool ComponentHasAugmentation(ConnVertex vertex)
		{
			if (_vertexInfo.TryGetValue(vertex, out var info))
			{
				return info.vertex.arbitraryVisit.Root().hasAugmentation;
			}
			else
			{
				return false;
			}
		}

		/// <summary>
		/// Clears this graph, by removing all edges and vertices, and removing all augmentation information from the
		/// vertices.
		/// </summary>
		public virtual void Clear()
		{
			// Note that we construct a new HashMap rather than calling vertexInfo.clear() in order to ensure a reduction in
			// space
			_vertexInfo = new Dictionary<ConnVertex, VertexInfo>();
			_maxLogVertexCountSinceRebuild = 0;
			_maxVertexInfoSize = 0;
		}

		/// <summary>
		/// Pushes all forest edges as far down as possible, so that any further pushes would violate the constraint on the
		/// size of connected components. The current implementation of this method takes O(V log^2 V) time.
		/// </summary>
		private void OptimizeForestEdges()
		{
			foreach (VertexInfo info in _vertexInfo.Values)
			{
				int level = _maxLogVertexCountSinceRebuild;
				EulerTourVertex vertex;
				for (vertex = info.vertex; vertex.lowerVertex != null; vertex = vertex.lowerVertex)
				{
					level--;
				}

				while (vertex != null)
				{
					EulerTourNode node = vertex.arbitraryVisit;
					ConnEdge edge = vertex.forestListHead;
					while (edge != null)
					{
						if (vertex == edge.vertex2)
						{
							// We'll address this edge when we visit edge.vertex1
							edge = edge.next2;
							continue;
						}
						ConnEdge nextEdge = edge.next1;

						EulerTourVertex lowerVertex1 = vertex;
						EulerTourVertex lowerVertex2 = edge.vertex2;
						for (int lowerLevel = level - 1; lowerLevel > 0; lowerLevel--)
						{
							// Compute the total size if we combine the Euler tour trees
							int combinedSize = 1;
							if (lowerVertex1.lowerVertex != null)
							{
								combinedSize += lowerVertex1.lowerVertex.arbitraryVisit.Root().size;
							}
							else
							{
								combinedSize++;
							}
							if (lowerVertex2.lowerVertex != null)
							{
								combinedSize += lowerVertex2.lowerVertex.arbitraryVisit.Root().size;
							}
							else
							{
								combinedSize++;
							}

							// X EulerTourVertices = (2 * X - 1) EulerTourNodes
							if (combinedSize > 2 * (1 << lowerLevel) - 1)
							{
								break;
							}

							lowerVertex1 = EnsureLowerVertex(lowerVertex1);
							lowerVertex2 = EnsureLowerVertex(lowerVertex2);
							EulerTourEdge lowerEdge = AddForestEdge(lowerVertex1, lowerVertex2);
							lowerEdge.higherEdge = edge.eulerTourEdge;
							edge.eulerTourEdge = lowerEdge;
						}

						if (lowerVertex1 != vertex)
						{
							// We pushed the edge down at least one level
							RemoveFromLinkedLists(edge);
							AugmentAncestorFlags(node);
							AugmentAncestorFlags(edge.vertex2.arbitraryVisit);

							edge.vertex1 = lowerVertex1;
							edge.vertex2 = lowerVertex2;
							AddToForestLinkedLists(edge);
							AugmentAncestorFlags(lowerVertex1.arbitraryVisit);
							AugmentAncestorFlags(lowerVertex2.arbitraryVisit);
						}

						edge = nextEdge;
					}

					vertex = vertex.higherVertex;
					level++;
				}
			}
		}

		/// <summary>
		/// Pushes each non-forest edge down to the lowest level where the endpoints are in the same connected component. The
		/// current implementation of this method takes O(V log V + E log V log log V) time.
		/// </summary>
		private void OptimizeGraphEdges()
		{
			foreach (VertexInfo info in _vertexInfo.Values)
			{
				EulerTourVertex vertex;
				for (vertex = info.vertex; vertex.lowerVertex != null; vertex = vertex.lowerVertex)
				{
					;
				}
				while (vertex != null)
				{
					EulerTourNode node = vertex.arbitraryVisit;
					ConnEdge edge = vertex.graphListHead;
					while (edge != null)
					{
						if (vertex == edge.vertex2)
						{
							// We'll address this edge when we visit edge.vertex1
							edge = edge.next2;
							continue;
						}
						ConnEdge nextEdge = edge.next1;

						// Use binary search to identify the lowest level where the two vertices are in the same connected
						// component
						int maxLevelsDown = 0;
						EulerTourVertex lowerVertex1 = vertex.lowerVertex;
						EulerTourVertex lowerVertex2 = edge.vertex2.lowerVertex;
						while (lowerVertex1 != null && lowerVertex2 != null)
						{
							maxLevelsDown++;
							lowerVertex1 = lowerVertex1.lowerVertex;
							lowerVertex2 = lowerVertex2.lowerVertex;
						}
						EulerTourVertex levelVertex1 = vertex;
						EulerTourVertex levelVertex2 = edge.vertex2;
						while (maxLevelsDown > 0)
						{
							int levelsDown = (maxLevelsDown + 1) / 2;
							lowerVertex1 = levelVertex1;
							lowerVertex2 = levelVertex2;
							for (int i = 0; i < levelsDown; i++)
							{
								lowerVertex1 = lowerVertex1.lowerVertex;
								lowerVertex2 = lowerVertex2.lowerVertex;
							}

							if (lowerVertex1.arbitraryVisit.Root() != lowerVertex2.arbitraryVisit.Root())
							{
								maxLevelsDown = levelsDown - 1;
							}
							else
							{
								levelVertex1 = lowerVertex1;
								levelVertex2 = lowerVertex2;
								maxLevelsDown -= levelsDown;
							}
						}

						if (levelVertex1 != vertex)
						{
							RemoveFromLinkedLists(edge);
							AugmentAncestorFlags(node);
							AugmentAncestorFlags(edge.vertex2.arbitraryVisit);

							edge.vertex1 = levelVertex1;
							edge.vertex2 = levelVertex2;
							AddToGraphLinkedLists(edge);
							AugmentAncestorFlags(levelVertex1.arbitraryVisit);
							AugmentAncestorFlags(levelVertex2.arbitraryVisit);
						}

						edge = nextEdge;
					}
					vertex = vertex.higherVertex;
				}
			}
		}

		/// <summary>
		/// Attempts to optimize the internal representation of the graph so that future updates will take less time. This
		/// method does not affect how long queries such as "connected" will take. You may find it beneficial to call
		/// optimize() when there is some downtime. Note that this method generally increases the amount of space the
		/// ConnGraph uses, but not beyond the bound of O(V log V + E).
		/// </summary>
		public virtual void Optimize()
		{
			// The current implementation of optimize() takes O(V log^2 V + E log V log log V) time
			Rebuild();
			OptimizeForestEdges();
			OptimizeGraphEdges();
		}
	}

}