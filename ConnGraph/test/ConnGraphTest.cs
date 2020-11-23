using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Pii = System.Tuple<int, int>;

namespace Connectivity.test
{
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertEquals;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertFalse;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertNull;
//JAVA TO C# CONVERTER TODO TASK: This Java 'import static' statement cannot be converted to C#:
//	import static org.junit.Assert.assertTrue;

	/* Note that most of the ConnGraphTest test methods use the one-argument ConnVertex constructor, in order to make their
	 * behavior more predictable. That way, there are consistent test results, and test failures are easier to debug.
	 */
	public class ConnGraphTest
	{
		private readonly ITestOutputHelper log;

		public ConnGraphTest(ITestOutputHelper log)
		{
			this.log = log;
		}

		/// <summary>
		/// Tests ConnectivityGraph on a small forest and a binary tree-like subgraph. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testForestAndBinaryTree()

		private void assertTrue(bool b)
		{
			Assert.True(b);
		}
		private void assertFalse(bool b)
		{
			Assert.False(b);
		}
		private void assertNull<T>(T t) where T : class
		{
			Assert.Null(t);
		}
		private void assertEqualsIE<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			Assert.True(ScrambledEquals(a, b));
		}
		private void assertEquals<T>(T a, T b)
		{
			Assert.Equal(a, b);
		}
		public static bool ScrambledEquals<T>(IEnumerable<T> list1, IEnumerable<T> list2) {
			var cnt = new Dictionary<T, int>();
			foreach (T s in list1) {
				if (cnt.ContainsKey(s)) {
					cnt[s]++;
				} else {
					cnt.Add(s, 1);
				}
			}
			foreach (T s in list2) {
				if (cnt.ContainsKey(s)) {
					cnt[s]--;
				} else {
					return false;
				}
			}
			return cnt.Values.All(c => c == 0);
		}

		private static int BitCount(int n)
		{
			int count = 0;
			while (n != 0)
			{
				count++;
				n &= (n - 1); //walking through all the bits which are set to one
			}

			return count;
		}

		[Theory]
		[InlineData(100000, 300000, 100000, 100000, 11439)]
		[InlineData(1000, 3000, 1000, 1000, 159475)]
		[InlineData(10000, 300000, 100000, 100000, 1184)]
		public void testBenchmark(int nV, int nE, int nQ, int nO, int seed)
		{
			var swA = new Stopwatch();
			var swD = new Stopwatch();
			var swQ = new Stopwatch();
			int nT = 0; // number of queries returning true
			int nF = 0; // number of queries returning false
			int hash = 0;

			int nD = nO / 2; // number of deletions
			int nA = nO - nD; // number of additions
			int maxE = nV * (nV - 1) / 2;

			var rand = new Random(seed);
			var rand2 = new Random(seed);

			swA.Start();
			var graph = new ConnGraph();
			swA.Stop();

			var V = new ConnVertex[nV];
			for (int i = 0; i < nV; i++)
			{
				V[i] = new ConnVertex(rand2);
			}

			var E = new HashSet<Pii>();
			var EList = new List<Pii>(nE);
			for (int i = 0; i < nE; i++)
			{
				AddRandomEdge();
			}
			log.WriteLine($"Init time: {swA.Elapsed.TotalMilliseconds}\n");
			swA.Reset();

			char[] O = new char[nO + nQ];
			for (int i = 0; i < nA; i++)
			{
				O[i] = 'a';
			}
			for (int i = nA; i < nO; i++)
			{
				O[i] = 'd';
			}
			for (int i = nO; i < nO + nQ; i++)
			{
				O[i] = 'q';
			}
			Shuffle(O, rand);

			foreach (char o in O)
			{
				switch (o)
				{
					case 'a':
						AddRandomEdge();
						break;
					case 'd':
						DeleteRandomEdge();
						break;
					case 'q':
						QueryRandom();
						break;
				}
			}
			log.WriteLine($"Add time: {swA.Elapsed.TotalMilliseconds}");
			log.WriteLine($"Delete time: {swD.Elapsed.TotalMilliseconds}");
			log.WriteLine($"Query time: {swQ.Elapsed.TotalMilliseconds}");
			log.WriteLine("");
			log.WriteLine($"nT: {nT}");
			log.WriteLine($"nF: {nF}");
			log.WriteLine($"hash: {hash}");

			Pii AddRandomEdge()
			{
				if (EList.Count == maxE)
					return new Pii(-1, -1);

				while (true)
				{
					int a1 = rand.Next(1, nV);
					int b1 = rand.Next(0, a1);
					var pii = new Pii(a1, b1);
					if (E.Add(pii))
					{
						EList.Add(pii);
						swA.Start();
						graph.addEdge(V[a1], V[b1]);
						swA.Stop();
						return pii;
					}
				}
			}

			Pii DeleteRandomEdge()
			{
				int count = EList.Count;
				if (count == 0)
					return new Pii(-1, -1);
				int i1 = rand.Next(0, count);
				var pii = EList[i1];
				E.Remove(pii);
				EList[i1] = EList[count - 1];
				EList.RemoveAt(count - 1);
				swD.Start();
				graph.removeEdge(V[pii.Item1], V[pii.Item2]);
				swD.Stop();
				return pii;
			}

			Tuple<Pii, bool> QueryRandom()
			{
				int a1 = rand.Next(1, nV);
				int b1 = rand.Next(0, a1);
				var pii = new Pii(a1, b1);
				swQ.Start();
				bool result = graph.connected(V[a1], V[b1]);
				swQ.Stop();

				hash = hash * 31 + (result ? 402653189 : 786433);
				if (result)
					nT++;
				else
					nF++;

				return new Tuple<Pii, bool>(pii, result);
			}
		}

		private static void Shuffle<T>(IList<T> list, Random rand)
		{
			for (int i = list.Count - 1; i > 0; i--)
			{
				int j = rand.Next(0, i + 1);
				T temp = list[i];
				list[i] = list[j];
				list[j] = temp;
			}
		}

		[Fact]
		public virtual void testForestAndBinaryTree()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex1 = new ConnVertex(random);
			ConnVertex vertex2 = new ConnVertex(random);
			Debug.Assert(graph.addEdge(vertex1, vertex2));
			ConnVertex vertex3 = new ConnVertex(random);
			assertTrue(graph.addEdge(vertex3, vertex1));
			ConnVertex vertex4 = new ConnVertex(random);
			assertTrue(graph.addEdge(vertex1, vertex4));
			ConnVertex vertex5 = new ConnVertex(random);
			ConnVertex vertex6 = new ConnVertex(random);
			ConnVertex vertex7 = new ConnVertex(random);
			assertTrue(graph.addEdge(vertex6, vertex7));
			assertTrue(graph.addEdge(vertex6, vertex5));
			assertTrue(graph.addEdge(vertex4, vertex5));
			assertFalse(graph.addEdge(vertex1, vertex3));
			ConnVertex vertex8 = new ConnVertex(random);
			ConnVertex vertex9 = new ConnVertex(random);
			assertTrue(graph.addEdge(vertex8, vertex9));
			ConnVertex vertex10 = new ConnVertex(random);
			assertTrue(graph.addEdge(vertex8, vertex10));
			assertFalse(graph.removeEdge(vertex7, vertex1));
			assertTrue(graph.connected(vertex1, vertex4));
			assertTrue(graph.connected(vertex1, vertex1));
			assertTrue(graph.connected(vertex1, vertex2));
			assertTrue(graph.connected(vertex3, vertex6));
			assertTrue(graph.connected(vertex7, vertex4));
			assertTrue(graph.connected(vertex8, vertex9));
			assertTrue(graph.connected(vertex5, vertex2));
			assertTrue(graph.connected(vertex8, vertex10));
			assertTrue(graph.connected(vertex9, vertex10));
			assertFalse(graph.connected(vertex1, vertex8));
			assertFalse(graph.connected(vertex2, vertex10));
			assertTrue(graph.removeEdge(vertex4, vertex5));
			assertTrue(graph.connected(vertex1, vertex3));
			assertTrue(graph.connected(vertex2, vertex4));
			assertTrue(graph.connected(vertex5, vertex6));
			assertTrue(graph.connected(vertex5, vertex7));
			assertTrue(graph.connected(vertex8, vertex9));
			assertTrue(graph.connected(vertex3, vertex3));
			assertFalse(graph.connected(vertex1, vertex5));
			assertFalse(graph.connected(vertex4, vertex7));
			assertFalse(graph.connected(vertex1, vertex8));
			assertFalse(graph.connected(vertex6, vertex9));

			ISet<ConnVertex> expectedAdjVertices = new HashSet<ConnVertex>();
			expectedAdjVertices.Add(vertex2);
			expectedAdjVertices.Add(vertex3);
			expectedAdjVertices.Add(vertex4);
			assertEquals(expectedAdjVertices, new HashSet<ConnVertex>(graph.adjacentVertices(vertex1)));
			expectedAdjVertices.Clear();
			expectedAdjVertices.Add(vertex5);
			expectedAdjVertices.Add(vertex7);
			assertEqualsIE(expectedAdjVertices, new HashSet<ConnVertex>(graph.adjacentVertices(vertex6)));
			assertEqualsIE(new[]{vertex8}, new HashSet<ConnVertex>(graph.adjacentVertices(vertex9)));
			assertEqualsIE(new ConnVertex[0], new HashSet<ConnVertex>(graph.adjacentVertices(new ConnVertex(random))));
			graph.optimize();

			IList<ConnVertex> vertices = new List<ConnVertex>(1000);
			for (int i = 0; i < 1000; i++)
			{
				vertices.Add(new ConnVertex(random));
			}
			for (int i = 0; i < 1000; i++)
			{
				if (i > 0 && BitCount(i) <= 3)
				{
					graph.addEdge(vertices[i], vertices[(i - 1) / 2]);
				}
			}
			for (int i = 0; i < 1000; i++)
			{
				if (BitCount(i) > 3)
				{
					graph.addEdge(vertices[(i - 1) / 2], vertices[i]);
				}
			}
			for (int i = 15; i < 31; i++)
			{
				graph.removeEdge(vertices[i], vertices[(i - 1) / 2]);
			}
			assertTrue(graph.connected(vertices[0], vertices[0]));
			assertTrue(graph.connected(vertices[11], vertices[2]));
			assertTrue(graph.connected(vertices[7], vertices[14]));
			assertTrue(graph.connected(vertices[0], vertices[10]));
			assertFalse(graph.connected(vertices[0], vertices[15]));
			assertFalse(graph.connected(vertices[15], vertices[16]));
			assertFalse(graph.connected(vertices[14], vertices[15]));
			assertFalse(graph.connected(vertices[7], vertices[605]));
			assertFalse(graph.connected(vertices[5], vertices[87]));
			assertTrue(graph.connected(vertices[22], vertices[22]));
			assertTrue(graph.connected(vertices[16], vertices[70]));
			assertTrue(graph.connected(vertices[113], vertices[229]));
			assertTrue(graph.connected(vertices[21], vertices[715]));
			assertTrue(graph.connected(vertices[175], vertices[715]));
			assertTrue(graph.connected(vertices[30], vertices[999]));
			assertTrue(graph.connected(vertices[991], vertices[999]));
		}

		/// <summary>
		/// Tests ConnectivityGraph on a small graph that has cycles. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSmallCycles()
		[Fact]
		public virtual void testSmallCycles()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex1 = new ConnVertex(random);
			ConnVertex vertex2 = new ConnVertex(random);
			ConnVertex vertex3 = new ConnVertex(random);
			ConnVertex vertex4 = new ConnVertex(random);
			ConnVertex vertex5 = new ConnVertex(random);
			assertTrue(graph.addEdge(vertex1, vertex2));
			assertTrue(graph.addEdge(vertex2, vertex3));
			assertTrue(graph.addEdge(vertex1, vertex3));
			assertTrue(graph.addEdge(vertex2, vertex4));
			assertTrue(graph.addEdge(vertex3, vertex4));
			assertTrue(graph.addEdge(vertex4, vertex5));
			assertTrue(graph.connected(vertex5, vertex1));
			assertTrue(graph.connected(vertex1, vertex4));
			assertTrue(graph.removeEdge(vertex4, vertex5));
			assertFalse(graph.connected(vertex4, vertex5));
			assertFalse(graph.connected(vertex5, vertex1));
			assertTrue(graph.connected(vertex1, vertex4));
			assertTrue(graph.removeEdge(vertex1, vertex2));
			assertTrue(graph.removeEdge(vertex3, vertex4));
			assertTrue(graph.connected(vertex1, vertex4));
			assertTrue(graph.removeEdge(vertex2, vertex3));
			assertTrue(graph.connected(vertex1, vertex3));
			assertTrue(graph.connected(vertex2, vertex4));
			assertFalse(graph.connected(vertex1, vertex4));
		}

		/// <summary>
		/// Tests ConnectivityGraph on a grid-based graph. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testGrid()
		[Fact]
		public virtual void testGrid()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex = new ConnVertex(random);
			assertTrue(graph.connected(vertex, vertex));

			graph = new ConnGraph(SumAndMax.AUGMENTATION);
			IList<IList<ConnVertex>> vertices = new List<IList<ConnVertex>>(20);
			for (int y = 0; y < 20; y++)
			{
				IList<ConnVertex> row = new List<ConnVertex>(20);
				for (int x = 0; x < 20; x++)
				{
					row.Add(new ConnVertex(random));
				}
				vertices.Add(row);
			}
			for (int y = 0; y < 19; y++)
			{
				for (int x = 0; x < 19; x++)
				{
					assertTrue(graph.addEdge(vertices[y][x], vertices[y][x + 1]));
					assertTrue(graph.addEdge(vertices[y][x], vertices[y + 1][x]));
				}
			}
			graph.optimize();

			assertTrue(graph.connected(vertices[0][0], vertices[15][12]));
			assertTrue(graph.connected(vertices[0][0], vertices[18][19]));
			assertFalse(graph.connected(vertices[0][0], vertices[19][19]));
			assertFalse(graph.removeEdge(vertices[18][19], vertices[19][19]));
			assertFalse(graph.removeEdge(vertices[0][0], vertices[2][2]));

			assertTrue(graph.removeEdge(vertices[12][8], vertices[11][8]));
			assertTrue(graph.removeEdge(vertices[12][9], vertices[11][9]));
			assertTrue(graph.removeEdge(vertices[12][8], vertices[12][7]));
			assertTrue(graph.removeEdge(vertices[13][8], vertices[13][7]));
			assertTrue(graph.removeEdge(vertices[13][8], vertices[14][8]));
			assertTrue(graph.removeEdge(vertices[12][9], vertices[12][10]));
			assertTrue(graph.removeEdge(vertices[13][9], vertices[13][10]));
			assertTrue(graph.connected(vertices[2][1], vertices[12][8]));
			assertTrue(graph.connected(vertices[12][8], vertices[13][9]));
			assertTrue(graph.removeEdge(vertices[13][9], vertices[14][9]));
			assertFalse(graph.connected(vertices[2][1], vertices[12][8]));
			assertTrue(graph.connected(vertices[12][8], vertices[13][9]));
			assertFalse(graph.connected(vertices[11][8], vertices[12][8]));
			assertTrue(graph.connected(vertices[16][18], vertices[6][15]));
			assertTrue(graph.removeEdge(vertices[12][9], vertices[12][8]));
			assertTrue(graph.removeEdge(vertices[12][8], vertices[13][8]));
			assertFalse(graph.connected(vertices[2][1], vertices[12][8]));
			assertFalse(graph.connected(vertices[12][8], vertices[13][9]));
			assertFalse(graph.connected(vertices[11][8], vertices[12][8]));
			assertTrue(graph.connected(vertices[13][8], vertices[12][9]));

			assertTrue(graph.removeEdge(vertices[6][15], vertices[5][15]));
			assertTrue(graph.removeEdge(vertices[6][15], vertices[7][15]));
			assertTrue(graph.removeEdge(vertices[6][15], vertices[6][14]));
			assertTrue(graph.removeEdge(vertices[6][15], vertices[6][16]));
			assertFalse(graph.removeEdge(vertices[6][15], vertices[5][15]));
			assertFalse(graph.connected(vertices[16][18], vertices[6][15]));
			assertFalse(graph.connected(vertices[7][15], vertices[6][15]));
			graph.addEdge(vertices[6][15], vertices[7][15]);
			assertTrue(graph.connected(vertices[16][18], vertices[6][15]));

			for (int y = 1; y < 19; y++)
			{
				for (int x = 1; x < 19; x++)
				{
					graph.removeEdge(vertices[y][x], vertices[y][x + 1]);
					graph.removeEdge(vertices[y][x], vertices[y + 1][x]);
				}
			}

			assertTrue(graph.addEdge(vertices[5][6], vertices[0][7]));
			assertTrue(graph.addEdge(vertices[12][8], vertices[5][6]));
			assertTrue(graph.connected(vertices[5][6], vertices[14][0]));
			assertTrue(graph.connected(vertices[12][8], vertices[0][17]));
			assertFalse(graph.connected(vertices[3][5], vertices[0][9]));
			assertFalse(graph.connected(vertices[14][2], vertices[11][18]));

			assertNull(graph.getVertexAugmentation(vertices[13][8]));
			assertNull(graph.getVertexAugmentation(vertices[6][4]));
			assertNull(graph.getComponentAugmentation(vertices[13][8]));
			assertNull(graph.getComponentAugmentation(vertices[6][4]));
			assertFalse(graph.vertexHasAugmentation(vertices[13][8]));
			assertFalse(graph.vertexHasAugmentation(vertices[6][4]));
			assertFalse(graph.componentHasAugmentation(vertices[13][8]));
			assertFalse(graph.componentHasAugmentation(vertices[6][4]));
		}

		/// <summary>
		/// Tests a graph with a hub-and-spokes subgraph and a clique subgraph. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testWheelAndClique()
		[Fact]
		public virtual void testWheelAndClique()
		{
			ConnGraph graph = new ConnGraph(SumAndMax.AUGMENTATION);
			Random random = new Random(6170);
			ConnVertex hub = new ConnVertex(random);
			IList<ConnVertex> spokes1 = new List<ConnVertex>(10);
			IList<ConnVertex> spokes2 = new List<ConnVertex>(10);
			for (int i = 0; i < 10; i++)
			{
				ConnVertex spoke1 = new ConnVertex(random);
				ConnVertex spoke2 = new ConnVertex(random);
				assertTrue(graph.addEdge(spoke1, spoke2));
				assertNull(graph.setVertexAugmentation(spoke1, new SumAndMax(i, i)));
				assertNull(graph.setVertexAugmentation(spoke2, new SumAndMax(i, i + 10)));
				spokes1.Add(spoke1);
				spokes2.Add(spoke2);
			}
			for (int i = 0; i < 10; i++)
			{
				assertTrue(graph.addEdge(spokes1[i], hub));
			}
			for (int i = 0; i < 10; i++)
			{
				assertTrue(graph.addEdge(hub, spokes2[i]));
			}

			IList<ConnVertex> clique = new List<ConnVertex>(10);
			for (int i = 0; i < 10; i++)
			{
				ConnVertex vertex = new ConnVertex(random);
				assertNull(graph.setVertexAugmentation(vertex, new SumAndMax(i, i + 20)));
				clique.Add(vertex);
			}
			for (int i = 0; i < 10; i++)
			{
				for (int j = i + 1; j < 10; j++)
				{
					assertTrue(graph.addEdge(clique[i], clique[j]));
				}
			}
			assertTrue(graph.addEdge(hub, clique[0]));

			assertTrue(graph.connected(spokes1[5], clique[3]));
			assertTrue(graph.connected(spokes1[3], spokes2[8]));
			assertTrue(graph.connected(spokes1[4], spokes2[4]));
			assertTrue(graph.connected(clique[5], hub));
			SumAndMax expectedAugmentation = new SumAndMax(135, 29);
			assertEquals(expectedAugmentation, graph.getComponentAugmentation(spokes2[8]));
			assertTrue(graph.componentHasAugmentation(spokes2[8]));
			assertEquals(expectedAugmentation, graph.getComponentAugmentation(hub));
			assertEquals(expectedAugmentation, graph.getComponentAugmentation(clique[9]));
			assertEquals(new SumAndMax(4, 4), graph.getVertexAugmentation(spokes1[4]));
			assertTrue(graph.vertexHasAugmentation(spokes1[4]));
			assertNull(graph.getVertexAugmentation(hub));
			assertFalse(graph.vertexHasAugmentation(hub));

			assertTrue(graph.removeEdge(spokes1[5], hub));
			assertTrue(graph.connected(spokes1[5], clique[2]));
			assertTrue(graph.connected(spokes1[5], spokes1[8]));
			assertTrue(graph.connected(spokes1[5], spokes2[5]));
			assertEquals(new SumAndMax(135, 29), graph.getComponentAugmentation(hub));
			assertTrue(graph.removeEdge(spokes2[5], hub));
			assertFalse(graph.connected(spokes1[5], clique[2]));
			assertFalse(graph.connected(spokes1[5], spokes1[8]));
			assertTrue(graph.connected(spokes1[5], spokes2[5]));
			assertEquals(new SumAndMax(125, 29), graph.getComponentAugmentation(hub));
			assertTrue(graph.addEdge(spokes1[5], hub));
			assertTrue(graph.connected(spokes1[5], clique[2]));
			assertTrue(graph.connected(spokes1[5], spokes1[8]));
			assertTrue(graph.connected(spokes1[5], spokes2[5]));
			assertEquals(new SumAndMax(135, 29), graph.getComponentAugmentation(hub));

			assertTrue(graph.removeEdge(hub, clique[0]));
			assertFalse(graph.connected(spokes1[3], clique[4]));
			assertTrue(graph.connected(spokes2[7], hub));
			assertFalse(graph.connected(hub, clique[0]));
			assertTrue(graph.connected(spokes2[9], spokes1[5]));
			assertEquals(new SumAndMax(90, 19), graph.getComponentAugmentation(hub));
			assertEquals(new SumAndMax(90, 19), graph.getComponentAugmentation(spokes2[4]));
			assertEquals(new SumAndMax(45, 29), graph.getComponentAugmentation(clique[1]));

			assertEquals(new SumAndMax(9, 29), graph.setVertexAugmentation(clique[9], new SumAndMax(-20, 4)));
			for (int i = 0; i < 10; i++)
			{
				assertEquals(new SumAndMax(i, i + 10), graph.setVertexAugmentation(spokes2[i], new SumAndMax(i - 1, i)));
			}
			assertNull(graph.removeVertexAugmentation(hub));
			assertEquals(new SumAndMax(4, 4), graph.removeVertexAugmentation(spokes1[4]));
			assertEquals(new SumAndMax(6, 7), graph.removeVertexAugmentation(spokes2[7]));

			assertEquals(new SumAndMax(70, 9), graph.getComponentAugmentation(hub));
			assertTrue(graph.componentHasAugmentation(hub));
			assertEquals(new SumAndMax(70, 9), graph.getComponentAugmentation(spokes1[6]));
			assertEquals(new SumAndMax(16, 28), graph.getComponentAugmentation(clique[4]));

			assertTrue(graph.addEdge(hub, clique[1]));
			expectedAugmentation = new SumAndMax(86, 28);
			assertEquals(expectedAugmentation, graph.getComponentAugmentation(hub));
			assertTrue(graph.componentHasAugmentation(hub));
			assertEquals(expectedAugmentation, graph.getComponentAugmentation(spokes2[7]));
			assertEquals(expectedAugmentation, graph.getComponentAugmentation(clique[3]));

			for (int i = 0; i < 10; i++)
			{
				assertTrue(graph.removeEdge(hub, spokes1[i]));
				if (i != 5)
				{
					assertTrue(graph.removeEdge(hub, spokes2[i]));
				}
			}
			assertFalse(graph.connected(hub, spokes1[8]));
			assertFalse(graph.connected(hub, spokes2[4]));
			assertTrue(graph.connected(hub, clique[5]));

			graph.clear();
			assertTrue(graph.addEdge(hub, spokes1[0]));
			assertTrue(graph.addEdge(hub, spokes2[0]));
			assertTrue(graph.addEdge(spokes1[0], spokes2[0]));
			assertTrue(graph.connected(hub, spokes1[0]));
			assertFalse(graph.connected(hub, spokes2[4]));
			assertTrue(graph.connected(clique[5], clique[5]));
			assertNull(graph.getComponentAugmentation(hub));
			assertNull(graph.getVertexAugmentation(spokes2[8]));
		}

		/// <summary>
		/// Sets the matching between vertices.get(columnIndex) and vertices.get(columnIndex + 1) to the permutation
		/// suggested by newPermutation. See the comments for the implementation of testPermutations(). </summary>
		/// <param name="graph"> The graph. </param>
		/// <param name="vertices"> The vertices. </param>
		/// <param name="columnIndex"> The index of the column. </param>
		/// <param name="oldPermutation"> The permutation for the current matching between vertices.get(columnIndex) and
		///     vertices.get(columnIndex + 1). setPermutation removes the edges in this matching. If there are currently no
		///     edges between those columns, then oldPermutation is null. </param>
		/// <param name="newPermutation"> The permutation for the new matching. </param>
		/// <returns> newPermutation. </returns>
		private int[] setPermutation(ConnGraph graph, IList<IList<ConnVertex>> vertices, int columnIndex, int[] oldPermutation, int[] newPermutation)
		{
			IList<ConnVertex> column1 = vertices[columnIndex];
			IList<ConnVertex> column2 = vertices[columnIndex + 1];
			if (oldPermutation != null)
			{
				for (int i = 0; i < oldPermutation.Length; i++)
				{
					assertTrue(graph.removeEdge(column1[i], column2[oldPermutation[i]]));
				}
			}
			for (int i = 0; i < newPermutation.Length; i++)
			{
				assertTrue(graph.addEdge(column1[i], column2[newPermutation[i]]));
			}
			return newPermutation;
		}

		/// <summary>
		/// Asserts that the specified permutation is the correct composite permutation for the specified column, i.e. that
		/// for all i, vertices.get(0).get(i) is in the same connected component as
		/// vertices.get(columnIndex + 1).get(expectedPermutation[i]). See the comments for the implementation of
		/// testPermutations().
		/// </summary>
		private void checkPermutation(ConnGraph graph, IList<IList<ConnVertex>> vertices, int columnIndex, int[] expectedPermutation)
		{
			IList<ConnVertex> firstColumn = vertices[0];
			IList<ConnVertex> column = vertices[columnIndex + 1];
			for (int i = 0; i < expectedPermutation.Length; i++)
			{
				assertTrue(graph.connected(firstColumn[i], column[expectedPermutation[i]]));
			}
		}

		/// <summary>
		/// Asserts that the specified permutation differs from the correct composite permutation for the specified column in
		/// every position, i.e. that for all i, vertices.get(0).get(i) is in a different connected component from
		/// vertices.get(columnIndex + 1).get(wrongPermutation[i]). See the comments for the implementation of
		/// testPermutations().
		/// </summary>
		private void checkWrongPermutation(ConnGraph graph, IList<IList<ConnVertex>> vertices, int columnIndex, int[] wrongPermutation)
		{
			IList<ConnVertex> firstColumn = vertices[0];
			IList<ConnVertex> column = vertices[columnIndex + 1];
			for (int i = 0; i < wrongPermutation.Length; i++)
			{
				assertFalse(graph.connected(firstColumn[i], column[wrongPermutation[i]]));
			}
		}

		/// <summary>
		/// Tests a graph in the style used to prove lower bounds on the performance of dynamic connectivity, as presented in
		/// https://ocw.mit.edu/courses/electrical-engineering-and-computer-science/6-851-advanced-data-structures-spring-2012/lecture-videos/session-21-dynamic-connectivity-lower-bound/ .
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testPermutations()
		[Fact]
		public virtual void testPermutations()
		{
			// The graph used in testPermutations() uses an 8 x 9 grid of vertices, such that vertices.get(i).get(j) is the
			// vertex at row j, column i. There is a perfect matching between each pair of columns i and i + 1 - that is,
			// there are eight non-adjacent edges from vertices in column i to vertices in column i + 1. These form a
			// permutation, so that the element j of the permutation is the row number of the vertex in column i + 1 that is
			// adjacent to the vertex at row j, column i.
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			IList<IList<ConnVertex>> vertices = new List<IList<ConnVertex>>(9);
			for (int i = 0; i < 9; i++)
			{
				IList<ConnVertex> column = new List<ConnVertex>(8);
				for (int j = 0; j < 8; j++)
				{
					column.Add(new ConnVertex(random));
				}
				vertices.Add(column);
			}

			int[] permutation0 = setPermutation(graph, vertices, 0, null, new int[]{2, 5, 0, 4, 7, 1, 3, 6});
			int[] permutation1 = setPermutation(graph, vertices, 1, null, new int[]{6, 5, 0, 7, 1, 2, 4, 3});
			int[] permutation2 = setPermutation(graph, vertices, 2, null, new int[]{2, 1, 7, 5, 6, 0, 4, 3});
			int[] permutation3 = setPermutation(graph, vertices, 3, null, new int[]{5, 2, 4, 6, 3, 0, 7, 1});
			int[] permutation4 = setPermutation(graph, vertices, 4, null, new int[]{5, 0, 2, 7, 4, 3, 1, 6});
			int[] permutation5 = setPermutation(graph, vertices, 5, null, new int[]{4, 7, 0, 1, 3, 6, 2, 5});
			int[] permutation6 = setPermutation(graph, vertices, 6, null, new int[]{4, 5, 3, 1, 7, 6, 2, 0});
			int[] permutation7 = setPermutation(graph, vertices, 7, null, new int[]{6, 7, 3, 0, 5, 1, 2, 4});

			permutation0 = setPermutation(graph, vertices, 0, permutation0, new int[]{7, 5, 3, 0, 4, 2, 1, 6});
			checkWrongPermutation(graph, vertices, 0, new int[]{5, 3, 0, 4, 2, 1, 6, 7});
			checkPermutation(graph, vertices, 0, new int[]{7, 5, 3, 0, 4, 2, 1, 6});
			permutation4 = setPermutation(graph, vertices, 4, permutation4, new int[]{2, 7, 0, 6, 5, 4, 1, 3});
			checkWrongPermutation(graph, vertices, 4, new int[]{7, 1, 6, 0, 5, 4, 3, 2});
			checkPermutation(graph, vertices, 4, new int[]{2, 7, 1, 6, 0, 5, 4, 3});
			permutation2 = setPermutation(graph, vertices, 2, permutation2, new int[]{3, 5, 6, 1, 4, 2, 7, 0});
			checkWrongPermutation(graph, vertices, 2, new int[]{6, 0, 7, 5, 3, 2, 4, 1});
			checkPermutation(graph, vertices, 2, new int[]{1, 6, 0, 7, 5, 3, 2, 4});
			permutation6 = setPermutation(graph, vertices, 6, permutation6, new int[]{4, 7, 1, 3, 6, 0, 5, 2});
			checkWrongPermutation(graph, vertices, 6, new int[]{7, 3, 0, 4, 2, 5, 1, 6});
			checkPermutation(graph, vertices, 6, new int[]{6, 7, 3, 0, 4, 2, 5, 1});
			permutation1 = setPermutation(graph, vertices, 1, permutation1, new int[]{2, 4, 0, 5, 6, 3, 7, 1});
			checkWrongPermutation(graph, vertices, 1, new int[]{3, 5, 2, 6, 0, 4, 7, 1});
			checkPermutation(graph, vertices, 1, new int[]{1, 3, 5, 2, 6, 0, 4, 7});
			permutation5 = setPermutation(graph, vertices, 5, permutation5, new int[]{5, 3, 2, 0, 7, 1, 6, 4});
			checkWrongPermutation(graph, vertices, 5, new int[]{5, 1, 0, 4, 3, 6, 7, 2});
			checkPermutation(graph, vertices, 5, new int[]{2, 5, 1, 0, 4, 3, 6, 7});
			permutation3 = setPermutation(graph, vertices, 3, permutation3, new int[]{1, 7, 3, 0, 4, 5, 6, 2});
			checkWrongPermutation(graph, vertices, 3, new int[]{7, 3, 6, 2, 0, 4, 1, 5});
			checkPermutation(graph, vertices, 3, new int[]{5, 7, 3, 6, 2, 0, 4, 1});
			permutation7 = setPermutation(graph, vertices, 7, permutation7, new int[]{4, 7, 5, 6, 2, 0, 1, 3});
			checkWrongPermutation(graph, vertices, 7, new int[]{2, 0, 6, 4, 7, 3, 1, 5});
			checkPermutation(graph, vertices, 7, new int[]{5, 2, 0, 6, 4, 7, 3, 1});
		}

		/// <summary>
		/// Tests a graph based on the United States. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUnitedStates()
		[Fact]
		public virtual void testUnitedStates()
		{
			ConnGraph graph = new ConnGraph(SumAndMax.AUGMENTATION);
			Random random = new Random(6170);
			ConnVertex alabama = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(alabama, new SumAndMax(7, 1819)));
			ConnVertex alaska = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(alaska, new SumAndMax(1, 1959)));
			ConnVertex arizona = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(arizona, new SumAndMax(9, 1912)));
			ConnVertex arkansas = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(arkansas, new SumAndMax(4, 1836)));
			ConnVertex california = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(california, new SumAndMax(53, 1850)));
			ConnVertex colorado = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(colorado, new SumAndMax(7, 1876)));
			ConnVertex connecticut = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(connecticut, new SumAndMax(5, 1788)));
			ConnVertex delaware = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(delaware, new SumAndMax(1, 1787)));
			ConnVertex florida = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(florida, new SumAndMax(27, 1845)));
			ConnVertex georgia = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(georgia, new SumAndMax(14, 1788)));
			ConnVertex hawaii = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(hawaii, new SumAndMax(2, 1959)));
			ConnVertex idaho = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(idaho, new SumAndMax(2, 1890)));
			ConnVertex illinois = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(illinois, new SumAndMax(18, 1818)));
			ConnVertex indiana = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(indiana, new SumAndMax(9, 1816)));
			ConnVertex iowa = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(iowa, new SumAndMax(4, 1846)));
			ConnVertex kansas = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(kansas, new SumAndMax(4, 1861)));
			ConnVertex kentucky = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(kentucky, new SumAndMax(6, 1792)));
			ConnVertex louisiana = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(louisiana, new SumAndMax(6, 1812)));
			ConnVertex maine = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(maine, new SumAndMax(2, 1820)));
			ConnVertex maryland = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(maryland, new SumAndMax(8, 1788)));
			ConnVertex massachusetts = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(massachusetts, new SumAndMax(9, 1788)));
			ConnVertex michigan = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(michigan, new SumAndMax(14, 1837)));
			ConnVertex minnesota = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(minnesota, new SumAndMax(8, 1858)));
			ConnVertex mississippi = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(mississippi, new SumAndMax(4, 1817)));
			ConnVertex missouri = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(missouri, new SumAndMax(8, 1821)));
			ConnVertex montana = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(montana, new SumAndMax(1, 1889)));
			ConnVertex nebraska = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(nebraska, new SumAndMax(3, 1867)));
			ConnVertex nevada = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(nevada, new SumAndMax(4, 1864)));
			ConnVertex newHampshire = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(newHampshire, new SumAndMax(2, 1788)));
			ConnVertex newJersey = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(newJersey, new SumAndMax(12, 1787)));
			ConnVertex newMexico = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(newMexico, new SumAndMax(3, 1912)));
			ConnVertex newYork = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(newYork, new SumAndMax(27, 1788)));
			ConnVertex northCarolina = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(northCarolina, new SumAndMax(13, 1789)));
			ConnVertex northDakota = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(northDakota, new SumAndMax(1, 1889)));
			ConnVertex ohio = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(ohio, new SumAndMax(16, 1803)));
			ConnVertex oklahoma = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(oklahoma, new SumAndMax(5, 1907)));
			ConnVertex oregon = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(oregon, new SumAndMax(5, 1859)));
			ConnVertex pennsylvania = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(pennsylvania, new SumAndMax(18, 1787)));
			ConnVertex rhodeIsland = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(rhodeIsland, new SumAndMax(2, 1790)));
			ConnVertex southCarolina = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(southCarolina, new SumAndMax(7, 1788)));
			ConnVertex southDakota = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(southDakota, new SumAndMax(1, 1889)));
			ConnVertex tennessee = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(tennessee, new SumAndMax(9, 1796)));
			ConnVertex texas = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(texas, new SumAndMax(36, 1845)));
			ConnVertex utah = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(utah, new SumAndMax(4, 1896)));
			ConnVertex vermont = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(vermont, new SumAndMax(1, 1791)));
			ConnVertex virginia = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(virginia, new SumAndMax(11, 1788)));
			ConnVertex washington = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(washington, new SumAndMax(10, 1889)));
			ConnVertex westVirginia = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(westVirginia, new SumAndMax(3, 1863)));
			ConnVertex wisconsin = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(wisconsin, new SumAndMax(8, 1848)));
			ConnVertex wyoming = new ConnVertex(random);
			assertNull(graph.setVertexAugmentation(wyoming, new SumAndMax(1, 1890)));

			assertTrue(graph.addEdge(alabama, florida));
			assertTrue(graph.addEdge(alabama, georgia));
			assertTrue(graph.addEdge(alabama, mississippi));
			assertTrue(graph.addEdge(alabama, tennessee));
			assertTrue(graph.addEdge(arizona, california));
			assertTrue(graph.addEdge(arizona, colorado));
			assertTrue(graph.addEdge(arizona, nevada));
			assertTrue(graph.addEdge(arizona, newMexico));
			assertTrue(graph.addEdge(arizona, utah));
			assertTrue(graph.addEdge(arkansas, louisiana));
			assertTrue(graph.addEdge(arkansas, mississippi));
			assertTrue(graph.addEdge(arkansas, missouri));
			assertTrue(graph.addEdge(arkansas, oklahoma));
			assertTrue(graph.addEdge(arkansas, tennessee));
			assertTrue(graph.addEdge(arkansas, texas));
			assertTrue(graph.addEdge(california, nevada));
			assertTrue(graph.addEdge(california, oregon));
			assertTrue(graph.addEdge(colorado, kansas));
			assertTrue(graph.addEdge(colorado, nebraska));
			assertTrue(graph.addEdge(colorado, newMexico));
			assertTrue(graph.addEdge(colorado, oklahoma));
			assertTrue(graph.addEdge(colorado, utah));
			assertTrue(graph.addEdge(colorado, wyoming));
			assertTrue(graph.addEdge(connecticut, massachusetts));
			assertTrue(graph.addEdge(connecticut, newYork));
			assertTrue(graph.addEdge(connecticut, rhodeIsland));
			assertTrue(graph.addEdge(delaware, maryland));
			assertTrue(graph.addEdge(delaware, newJersey));
			assertTrue(graph.addEdge(delaware, pennsylvania));
			assertTrue(graph.addEdge(florida, georgia));
			assertTrue(graph.addEdge(georgia, northCarolina));
			assertTrue(graph.addEdge(georgia, southCarolina));
			assertTrue(graph.addEdge(georgia, tennessee));
			assertTrue(graph.addEdge(idaho, montana));
			assertTrue(graph.addEdge(idaho, nevada));
			assertTrue(graph.addEdge(idaho, oregon));
			assertTrue(graph.addEdge(idaho, utah));
			assertTrue(graph.addEdge(idaho, washington));
			assertTrue(graph.addEdge(idaho, wyoming));
			assertTrue(graph.addEdge(illinois, indiana));
			assertTrue(graph.addEdge(illinois, iowa));
			assertTrue(graph.addEdge(illinois, kentucky));
			assertTrue(graph.addEdge(illinois, missouri));
			assertTrue(graph.addEdge(illinois, wisconsin));
			assertTrue(graph.addEdge(indiana, kentucky));
			assertTrue(graph.addEdge(indiana, michigan));
			assertTrue(graph.addEdge(indiana, ohio));
			assertTrue(graph.addEdge(iowa, minnesota));
			assertTrue(graph.addEdge(iowa, missouri));
			assertTrue(graph.addEdge(iowa, nebraska));
			assertTrue(graph.addEdge(iowa, southDakota));
			assertTrue(graph.addEdge(iowa, wisconsin));
			assertTrue(graph.addEdge(kansas, missouri));
			assertTrue(graph.addEdge(kansas, nebraska));
			assertTrue(graph.addEdge(kansas, oklahoma));
			assertTrue(graph.addEdge(kentucky, missouri));
			assertTrue(graph.addEdge(kentucky, ohio));
			assertTrue(graph.addEdge(kentucky, tennessee));
			assertTrue(graph.addEdge(kentucky, virginia));
			assertTrue(graph.addEdge(kentucky, westVirginia));
			assertTrue(graph.addEdge(louisiana, mississippi));
			assertTrue(graph.addEdge(louisiana, texas));
			assertTrue(graph.addEdge(maine, newHampshire));
			assertTrue(graph.addEdge(maryland, pennsylvania));
			assertTrue(graph.addEdge(maryland, virginia));
			assertTrue(graph.addEdge(maryland, westVirginia));
			assertTrue(graph.addEdge(massachusetts, newHampshire));
			assertTrue(graph.addEdge(massachusetts, newYork));
			assertTrue(graph.addEdge(massachusetts, rhodeIsland));
			assertTrue(graph.addEdge(massachusetts, vermont));
			assertTrue(graph.addEdge(michigan, ohio));
			assertTrue(graph.addEdge(michigan, wisconsin));
			assertTrue(graph.addEdge(minnesota, northDakota));
			assertTrue(graph.addEdge(minnesota, southDakota));
			assertTrue(graph.addEdge(minnesota, wisconsin));
			assertTrue(graph.addEdge(mississippi, tennessee));
			assertTrue(graph.addEdge(missouri, nebraska));
			assertTrue(graph.addEdge(missouri, oklahoma));
			assertTrue(graph.addEdge(missouri, tennessee));
			assertTrue(graph.addEdge(montana, northDakota));
			assertTrue(graph.addEdge(montana, southDakota));
			assertTrue(graph.addEdge(montana, wyoming));
			assertTrue(graph.addEdge(nebraska, southDakota));
			assertTrue(graph.addEdge(nebraska, wyoming));
			assertTrue(graph.addEdge(nevada, oregon));
			assertTrue(graph.addEdge(nevada, utah));
			assertTrue(graph.addEdge(newHampshire, vermont));
			assertTrue(graph.addEdge(newJersey, newYork));
			assertTrue(graph.addEdge(newJersey, pennsylvania));
			assertTrue(graph.addEdge(newMexico, oklahoma));
			assertTrue(graph.addEdge(newMexico, texas));
			assertTrue(graph.addEdge(newMexico, utah));
			assertTrue(graph.addEdge(newYork, pennsylvania));
			assertTrue(graph.addEdge(newYork, vermont));
			assertTrue(graph.addEdge(northCarolina, southCarolina));
			assertTrue(graph.addEdge(northCarolina, tennessee));
			assertTrue(graph.addEdge(northCarolina, virginia));
			assertTrue(graph.addEdge(northDakota, southDakota));
			assertTrue(graph.addEdge(ohio, pennsylvania));
			assertTrue(graph.addEdge(ohio, westVirginia));
			assertTrue(graph.addEdge(oklahoma, texas));
			assertTrue(graph.addEdge(oregon, washington));
			assertTrue(graph.addEdge(pennsylvania, westVirginia));
			assertTrue(graph.addEdge(southDakota, wyoming));
			assertTrue(graph.addEdge(tennessee, virginia));
			assertTrue(graph.addEdge(utah, wyoming));
			assertTrue(graph.addEdge(virginia, westVirginia));

			assertTrue(graph.connected(florida, washington));
			assertTrue(graph.connected(rhodeIsland, michigan));
			assertTrue(graph.connected(delaware, texas));
			assertFalse(graph.connected(alaska, newYork));
			assertFalse(graph.connected(hawaii, idaho));
			assertEquals(new SumAndMax(432, 1912), graph.getComponentAugmentation(newJersey));
			assertEquals(new SumAndMax(2, 1959), graph.getComponentAugmentation(hawaii));

			// 2186: Aliens attack, split nation in two using lasers
			assertTrue(graph.removeEdge(northDakota, minnesota));
			assertTrue(graph.removeEdge(southDakota, minnesota));
			assertTrue(graph.removeEdge(southDakota, iowa));
			assertTrue(graph.removeEdge(nebraska, iowa));
			assertTrue(graph.removeEdge(nebraska, missouri));
			assertTrue(graph.removeEdge(kansas, missouri));
			assertTrue(graph.removeEdge(oklahoma, missouri));
			assertTrue(graph.removeEdge(oklahoma, arkansas));
			assertTrue(graph.removeEdge(texas, arkansas));
			assertTrue(graph.connected(california, massachusetts));
			assertTrue(graph.connected(montana, virginia));
			assertTrue(graph.connected(idaho, southDakota));
			assertTrue(graph.connected(maine, tennessee));
			assertEquals(new SumAndMax(432, 1912), graph.getComponentAugmentation(vermont));
			assertTrue(graph.removeEdge(texas, louisiana));
			assertFalse(graph.connected(california, massachusetts));
			assertFalse(graph.connected(montana, virginia));
			assertTrue(graph.connected(idaho, southDakota));
			assertTrue(graph.connected(maine, tennessee));
			assertEquals(new SumAndMax(149, 1912), graph.getComponentAugmentation(wyoming));
			assertEquals(new SumAndMax(283, 1863), graph.getComponentAugmentation(vermont));

			// 2254: California breaks off into ocean, secedes
			assertTrue(graph.removeEdge(california, oregon));
			assertTrue(graph.removeEdge(california, nevada));
			assertTrue(graph.removeEdge(california, arizona));
			assertEquals(new SumAndMax(53, 1850), graph.removeVertexAugmentation(california));
			assertFalse(graph.connected(california, utah));
			assertFalse(graph.connected(california, oregon));
			assertNull(graph.getComponentAugmentation(california));
			assertEquals(new SumAndMax(96, 1912), graph.getComponentAugmentation(washington));
			assertEquals(new SumAndMax(283, 1863), graph.getComponentAugmentation(vermont));

			// 2367: Nuclear armageddon
			assertEquals(new SumAndMax(7, 1819), graph.removeVertexAugmentation(alabama));
			assertTrue(graph.removeEdge(alabama, florida));
			assertTrue(graph.removeEdge(alabama, georgia));
			assertTrue(graph.removeEdge(alabama, mississippi));
			assertTrue(graph.removeEdge(alabama, tennessee));
			assertEquals(new SumAndMax(1, 1959), graph.removeVertexAugmentation(alaska));
			assertEquals(new SumAndMax(9, 1912), graph.removeVertexAugmentation(arizona));
			assertTrue(graph.removeEdge(arizona, colorado));
			assertTrue(graph.removeEdge(arizona, nevada));
			assertTrue(graph.removeEdge(arizona, newMexico));
			assertTrue(graph.removeEdge(arizona, utah));
			assertEquals(new SumAndMax(4, 1836), graph.removeVertexAugmentation(arkansas));
			assertTrue(graph.removeEdge(arkansas, louisiana));
			assertTrue(graph.removeEdge(arkansas, mississippi));
			assertTrue(graph.removeEdge(arkansas, missouri));
			assertTrue(graph.removeEdge(arkansas, tennessee));
			assertEquals(new SumAndMax(7, 1876), graph.removeVertexAugmentation(colorado));
			assertTrue(graph.removeEdge(colorado, kansas));
			assertTrue(graph.removeEdge(colorado, nebraska));
			assertTrue(graph.removeEdge(colorado, newMexico));
			assertTrue(graph.removeEdge(colorado, oklahoma));
			assertTrue(graph.removeEdge(colorado, utah));
			assertTrue(graph.removeEdge(colorado, wyoming));
			assertEquals(new SumAndMax(5, 1788), graph.removeVertexAugmentation(connecticut));
			assertTrue(graph.removeEdge(connecticut, massachusetts));
			assertTrue(graph.removeEdge(connecticut, newYork));
			assertTrue(graph.removeEdge(connecticut, rhodeIsland));
			assertEquals(new SumAndMax(1, 1787), graph.removeVertexAugmentation(delaware));
			assertTrue(graph.removeEdge(delaware, maryland));
			assertTrue(graph.removeEdge(delaware, newJersey));
			assertTrue(graph.removeEdge(delaware, pennsylvania));
			assertEquals(new SumAndMax(27, 1845), graph.removeVertexAugmentation(florida));
			assertTrue(graph.removeEdge(florida, georgia));
			assertEquals(new SumAndMax(14, 1788), graph.removeVertexAugmentation(georgia));
			assertTrue(graph.removeEdge(georgia, northCarolina));
			assertTrue(graph.removeEdge(georgia, southCarolina));
			assertTrue(graph.removeEdge(georgia, tennessee));
			assertEquals(new SumAndMax(2, 1959), graph.removeVertexAugmentation(hawaii));
			assertEquals(new SumAndMax(2, 1890), graph.removeVertexAugmentation(idaho));
			assertTrue(graph.removeEdge(idaho, montana));
			assertTrue(graph.removeEdge(idaho, nevada));
			assertTrue(graph.removeEdge(idaho, oregon));
			assertTrue(graph.removeEdge(idaho, utah));
			assertTrue(graph.removeEdge(idaho, washington));
			assertTrue(graph.removeEdge(idaho, wyoming));
			assertEquals(new SumAndMax(18, 1818), graph.removeVertexAugmentation(illinois));
			assertTrue(graph.removeEdge(illinois, indiana));
			assertTrue(graph.removeEdge(illinois, iowa));
			assertTrue(graph.removeEdge(illinois, kentucky));
			assertTrue(graph.removeEdge(illinois, missouri));
			assertTrue(graph.removeEdge(illinois, wisconsin));
			assertEquals(new SumAndMax(9, 1816), graph.removeVertexAugmentation(indiana));
			assertTrue(graph.removeEdge(indiana, kentucky));
			assertTrue(graph.removeEdge(indiana, michigan));
			assertTrue(graph.removeEdge(indiana, ohio));
			assertEquals(new SumAndMax(4, 1846), graph.removeVertexAugmentation(iowa));
			assertTrue(graph.removeEdge(iowa, minnesota));
			assertTrue(graph.removeEdge(iowa, missouri));
			assertTrue(graph.removeEdge(iowa, wisconsin));
			assertEquals(new SumAndMax(4, 1861), graph.removeVertexAugmentation(kansas));
			assertTrue(graph.removeEdge(kansas, nebraska));
			assertTrue(graph.removeEdge(kansas, oklahoma));
			assertEquals(new SumAndMax(6, 1792), graph.removeVertexAugmentation(kentucky));
			assertTrue(graph.removeEdge(kentucky, missouri));
			assertTrue(graph.removeEdge(kentucky, ohio));
			assertTrue(graph.removeEdge(kentucky, tennessee));
			assertTrue(graph.removeEdge(kentucky, virginia));
			assertTrue(graph.removeEdge(kentucky, westVirginia));
			assertEquals(new SumAndMax(6, 1812), graph.removeVertexAugmentation(louisiana));
			assertTrue(graph.removeEdge(louisiana, mississippi));
			assertEquals(new SumAndMax(2, 1820), graph.removeVertexAugmentation(maine));
			assertTrue(graph.removeEdge(maine, newHampshire));
			assertEquals(new SumAndMax(8, 1788), graph.removeVertexAugmentation(maryland));
			assertTrue(graph.removeEdge(maryland, pennsylvania));
			assertTrue(graph.removeEdge(maryland, virginia));
			assertTrue(graph.removeEdge(maryland, westVirginia));
			assertEquals(new SumAndMax(9, 1788), graph.removeVertexAugmentation(massachusetts));
			assertTrue(graph.removeEdge(massachusetts, newHampshire));
			assertTrue(graph.removeEdge(massachusetts, newYork));
			assertTrue(graph.removeEdge(massachusetts, rhodeIsland));
			assertTrue(graph.removeEdge(massachusetts, vermont));
			assertEquals(new SumAndMax(14, 1837), graph.removeVertexAugmentation(michigan));
			assertTrue(graph.removeEdge(michigan, ohio));
			assertTrue(graph.removeEdge(michigan, wisconsin));
			assertEquals(new SumAndMax(8, 1858), graph.removeVertexAugmentation(minnesota));
			assertTrue(graph.removeEdge(minnesota, wisconsin));
			assertEquals(new SumAndMax(4, 1817), graph.removeVertexAugmentation(mississippi));
			assertTrue(graph.removeEdge(mississippi, tennessee));
			assertEquals(new SumAndMax(8, 1821), graph.removeVertexAugmentation(missouri));
			assertTrue(graph.removeEdge(missouri, tennessee));
			assertEquals(new SumAndMax(1, 1889), graph.removeVertexAugmentation(montana));
			assertTrue(graph.removeEdge(montana, northDakota));
			assertTrue(graph.removeEdge(montana, southDakota));
			assertTrue(graph.removeEdge(montana, wyoming));
			assertEquals(new SumAndMax(3, 1867), graph.removeVertexAugmentation(nebraska));
			assertTrue(graph.removeEdge(nebraska, southDakota));
			assertTrue(graph.removeEdge(nebraska, wyoming));
			assertEquals(new SumAndMax(4, 1864), graph.removeVertexAugmentation(nevada));
			assertTrue(graph.removeEdge(nevada, oregon));
			assertTrue(graph.removeEdge(nevada, utah));
			assertEquals(new SumAndMax(2, 1788), graph.removeVertexAugmentation(newHampshire));
			assertTrue(graph.removeEdge(newHampshire, vermont));
			assertEquals(new SumAndMax(12, 1787), graph.removeVertexAugmentation(newJersey));
			assertTrue(graph.removeEdge(newJersey, newYork));
			assertTrue(graph.removeEdge(newJersey, pennsylvania));
			assertEquals(new SumAndMax(3, 1912), graph.removeVertexAugmentation(newMexico));
			assertTrue(graph.removeEdge(newMexico, oklahoma));
			assertTrue(graph.removeEdge(newMexico, texas));
			assertTrue(graph.removeEdge(newMexico, utah));
			assertEquals(new SumAndMax(27, 1788), graph.removeVertexAugmentation(newYork));
			assertTrue(graph.removeEdge(newYork, pennsylvania));
			assertTrue(graph.removeEdge(newYork, vermont));
			assertEquals(new SumAndMax(13, 1789), graph.removeVertexAugmentation(northCarolina));
			assertTrue(graph.removeEdge(northCarolina, southCarolina));
			assertTrue(graph.removeEdge(northCarolina, tennessee));
			assertTrue(graph.removeEdge(northCarolina, virginia));
			assertEquals(new SumAndMax(1, 1889), graph.removeVertexAugmentation(northDakota));
			assertTrue(graph.removeEdge(northDakota, southDakota));
			assertEquals(new SumAndMax(16, 1803), graph.removeVertexAugmentation(ohio));
			assertTrue(graph.removeEdge(ohio, pennsylvania));
			assertTrue(graph.removeEdge(ohio, westVirginia));
			assertEquals(new SumAndMax(5, 1907), graph.removeVertexAugmentation(oklahoma));
			assertTrue(graph.removeEdge(oklahoma, texas));
			assertEquals(new SumAndMax(5, 1859), graph.removeVertexAugmentation(oregon));
			assertTrue(graph.removeEdge(oregon, washington));
			assertEquals(new SumAndMax(18, 1787), graph.removeVertexAugmentation(pennsylvania));
			assertTrue(graph.removeEdge(pennsylvania, westVirginia));
			assertEquals(new SumAndMax(2, 1790), graph.removeVertexAugmentation(rhodeIsland));
			assertEquals(new SumAndMax(7, 1788), graph.removeVertexAugmentation(southCarolina));
			assertEquals(new SumAndMax(1, 1889), graph.removeVertexAugmentation(southDakota));
			assertTrue(graph.removeEdge(southDakota, wyoming));
			assertEquals(new SumAndMax(9, 1796), graph.removeVertexAugmentation(tennessee));
			assertTrue(graph.removeEdge(tennessee, virginia));
			assertEquals(new SumAndMax(36, 1845), graph.removeVertexAugmentation(texas));
			assertEquals(new SumAndMax(4, 1896), graph.removeVertexAugmentation(utah));
			assertTrue(graph.removeEdge(utah, wyoming));
			assertEquals(new SumAndMax(1, 1791), graph.removeVertexAugmentation(vermont));
			assertEquals(new SumAndMax(11, 1788), graph.removeVertexAugmentation(virginia));
			assertTrue(graph.removeEdge(virginia, westVirginia));
			assertEquals(new SumAndMax(10, 1889), graph.removeVertexAugmentation(washington));
			assertEquals(new SumAndMax(3, 1863), graph.removeVertexAugmentation(westVirginia));
			assertEquals(new SumAndMax(8, 1848), graph.removeVertexAugmentation(wisconsin));
			assertEquals(new SumAndMax(1, 1890), graph.removeVertexAugmentation(wyoming));

			assertFalse(graph.connected(georgia, newMexico));
			assertFalse(graph.connected(wisconsin, michigan));
			assertFalse(graph.connected(ohio, kentucky));
			assertFalse(graph.connected(alaska, connecticut));
			assertNull(graph.getComponentAugmentation(southDakota));
			assertNull(graph.getComponentAugmentation(arkansas));
		}

		/// <summary>
		/// Tests ConnectivityGraph on the graph for a dodecahedron. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDodecahedron()
		[Fact]
		public virtual void testDodecahedron()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex1 = new ConnVertex(random);
			ConnVertex vertex2 = new ConnVertex(random);
			ConnVertex vertex3 = new ConnVertex(random);
			ConnVertex vertex4 = new ConnVertex(random);
			ConnVertex vertex5 = new ConnVertex(random);
			ConnVertex vertex6 = new ConnVertex(random);
			ConnVertex vertex7 = new ConnVertex(random);
			ConnVertex vertex8 = new ConnVertex(random);
			ConnVertex vertex9 = new ConnVertex(random);
			ConnVertex vertex10 = new ConnVertex(random);
			ConnVertex vertex11 = new ConnVertex(random);
			ConnVertex vertex12 = new ConnVertex(random);
			ConnVertex vertex13 = new ConnVertex(random);
			ConnVertex vertex14 = new ConnVertex(random);
			ConnVertex vertex15 = new ConnVertex(random);
			ConnVertex vertex16 = new ConnVertex(random);
			ConnVertex vertex17 = new ConnVertex(random);
			ConnVertex vertex18 = new ConnVertex(random);
			ConnVertex vertex19 = new ConnVertex(random);
			ConnVertex vertex20 = new ConnVertex(random);

			assertTrue(graph.addEdge(vertex1, vertex2));
			assertTrue(graph.addEdge(vertex1, vertex5));
			assertTrue(graph.addEdge(vertex1, vertex6));
			assertTrue(graph.addEdge(vertex2, vertex3));
			assertTrue(graph.addEdge(vertex2, vertex8));
			assertTrue(graph.addEdge(vertex3, vertex4));
			assertTrue(graph.addEdge(vertex3, vertex10));
			assertTrue(graph.addEdge(vertex4, vertex5));
			assertTrue(graph.addEdge(vertex4, vertex12));
			assertTrue(graph.addEdge(vertex5, vertex14));
			assertTrue(graph.addEdge(vertex6, vertex7));
			assertTrue(graph.addEdge(vertex6, vertex15));
			assertTrue(graph.addEdge(vertex7, vertex8));
			assertTrue(graph.addEdge(vertex7, vertex16));
			assertTrue(graph.addEdge(vertex8, vertex9));
			assertTrue(graph.addEdge(vertex9, vertex10));
			assertTrue(graph.addEdge(vertex9, vertex17));
			assertTrue(graph.addEdge(vertex10, vertex11));
			assertTrue(graph.addEdge(vertex11, vertex12));
			assertTrue(graph.addEdge(vertex11, vertex18));
			assertTrue(graph.addEdge(vertex12, vertex13));
			assertTrue(graph.addEdge(vertex13, vertex14));
			assertTrue(graph.addEdge(vertex13, vertex19));
			assertTrue(graph.addEdge(vertex14, vertex15));
			assertTrue(graph.addEdge(vertex15, vertex20));
			assertTrue(graph.addEdge(vertex16, vertex17));
			assertTrue(graph.addEdge(vertex16, vertex20));
			assertTrue(graph.addEdge(vertex17, vertex18));
			assertTrue(graph.addEdge(vertex18, vertex19));
			assertTrue(graph.addEdge(vertex19, vertex20));
			graph.optimize();

			assertTrue(graph.connected(vertex1, vertex17));
			assertTrue(graph.connected(vertex7, vertex15));

			assertTrue(graph.removeEdge(vertex5, vertex14));
			assertTrue(graph.removeEdge(vertex6, vertex15));
			assertTrue(graph.removeEdge(vertex7, vertex16));
			assertTrue(graph.removeEdge(vertex12, vertex13));
			assertTrue(graph.removeEdge(vertex16, vertex17));
			assertTrue(graph.connected(vertex1, vertex14));
			assertTrue(graph.connected(vertex4, vertex20));
			assertTrue(graph.connected(vertex14, vertex16));

			assertTrue(graph.removeEdge(vertex18, vertex19));
			assertFalse(graph.connected(vertex1, vertex14));
			assertFalse(graph.connected(vertex4, vertex20));
			assertTrue(graph.connected(vertex14, vertex16));

			graph.clear();
			graph.optimize();
			assertTrue(graph.connected(vertex7, vertex7));
			assertFalse(graph.connected(vertex1, vertex2));
		}

		/// <summary>
		/// Tests the zero-argument ConnVertex constructor. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDefaultConnVertexConstructor()
		[Fact]
		public virtual void testDefaultConnVertexConstructor()
		{
			ConnGraph graph = new ConnGraph();
			ConnVertex vertex1 = new ConnVertex();
			ConnVertex vertex2 = new ConnVertex();
			ConnVertex vertex3 = new ConnVertex();
			ConnVertex vertex4 = new ConnVertex();
			ConnVertex vertex5 = new ConnVertex();
			ConnVertex vertex6 = new ConnVertex();
			assertTrue(graph.addEdge(vertex1, vertex2));
			assertTrue(graph.addEdge(vertex2, vertex3));
			assertTrue(graph.addEdge(vertex1, vertex3));
			assertTrue(graph.addEdge(vertex4, vertex5));
			assertTrue(graph.connected(vertex1, vertex3));
			assertTrue(graph.connected(vertex4, vertex5));
			assertFalse(graph.connected(vertex1, vertex4));

			graph.optimize();
			assertTrue(graph.removeEdge(vertex1, vertex3));
			assertTrue(graph.connected(vertex1, vertex3));
			assertTrue(graph.connected(vertex4, vertex5));
			assertFalse(graph.connected(vertex1, vertex4));
			assertTrue(graph.removeEdge(vertex1, vertex2));
			assertFalse(graph.connected(vertex1, vertex3));
			assertTrue(graph.connected(vertex4, vertex5));
			assertFalse(graph.connected(vertex1, vertex4));

			assertEqualsIE(new [] {vertex3}, new HashSet<ConnVertex>(graph.adjacentVertices(vertex2)));
			assertTrue(graph.adjacentVertices(vertex1).Count == 0);
			assertTrue(graph.adjacentVertices(vertex6).Count == 0);
		}
	}

}