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
		private readonly ITestOutputHelper _log;

		public ConnGraphTest(ITestOutputHelper log)
		{
			_log = log;
		}

		/// <summary>
		/// Tests ConnectivityGraph on a small forest and a binary tree-like subgraph. </summary>

		private static void AssertTrue(bool b)
		{
			Assert.True(b);
		}
		private static void AssertFalse(bool b)
		{
			Assert.False(b);
		}
		private static void AssertNull<T>(T t) where T : class
		{
			Assert.Null(t);
		}
		private static void AssertEqualsUnordered<T>(IEnumerable<T> a, IEnumerable<T> b)
		{
			Assert.True(ScrambledEquals(a, b));
		}
		private static void AssertEquals<T>(T a, T b)
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
		public void TestBenchmark(int nV, int nE, int nQ, int nO, int seed)
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
			_log.WriteLine($"Init time: {swA.Elapsed.TotalMilliseconds}\n");
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
			_log.WriteLine($"Add time: {swA.Elapsed.TotalMilliseconds}");
			_log.WriteLine($"Delete time: {swD.Elapsed.TotalMilliseconds}");
			_log.WriteLine($"Query time: {swQ.Elapsed.TotalMilliseconds}");
			_log.WriteLine("");
			_log.WriteLine($"nT: {nT}");
			_log.WriteLine($"nF: {nF}");
			_log.WriteLine($"hash: {hash}");

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
						graph.AddEdge(V[a1], V[b1]);
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
				graph.RemoveEdge(V[pii.Item1], V[pii.Item2]);
				swD.Stop();
				return pii;
			}

			Tuple<Pii, bool> QueryRandom()
			{
				int a1 = rand.Next(1, nV);
				int b1 = rand.Next(0, a1);
				var pii = new Pii(a1, b1);
				swQ.Start();
				bool result = graph.Connected(V[a1], V[b1]);
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
		public virtual void TestForestAndBinaryTree()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex1 = new ConnVertex(random);
			ConnVertex vertex2 = new ConnVertex(random);
			Debug.Assert(graph.AddEdge(vertex1, vertex2));
			ConnVertex vertex3 = new ConnVertex(random);
			AssertTrue(graph.AddEdge(vertex3, vertex1));
			ConnVertex vertex4 = new ConnVertex(random);
			AssertTrue(graph.AddEdge(vertex1, vertex4));
			ConnVertex vertex5 = new ConnVertex(random);
			ConnVertex vertex6 = new ConnVertex(random);
			ConnVertex vertex7 = new ConnVertex(random);
			AssertTrue(graph.AddEdge(vertex6, vertex7));
			AssertTrue(graph.AddEdge(vertex6, vertex5));
			AssertTrue(graph.AddEdge(vertex4, vertex5));
			AssertFalse(graph.AddEdge(vertex1, vertex3));
			ConnVertex vertex8 = new ConnVertex(random);
			ConnVertex vertex9 = new ConnVertex(random);
			AssertTrue(graph.AddEdge(vertex8, vertex9));
			ConnVertex vertex10 = new ConnVertex(random);
			AssertTrue(graph.AddEdge(vertex8, vertex10));
			AssertFalse(graph.RemoveEdge(vertex7, vertex1));
			AssertTrue(graph.Connected(vertex1, vertex4));
			AssertTrue(graph.Connected(vertex1, vertex1));
			AssertTrue(graph.Connected(vertex1, vertex2));
			AssertTrue(graph.Connected(vertex3, vertex6));
			AssertTrue(graph.Connected(vertex7, vertex4));
			AssertTrue(graph.Connected(vertex8, vertex9));
			AssertTrue(graph.Connected(vertex5, vertex2));
			AssertTrue(graph.Connected(vertex8, vertex10));
			AssertTrue(graph.Connected(vertex9, vertex10));
			AssertFalse(graph.Connected(vertex1, vertex8));
			AssertFalse(graph.Connected(vertex2, vertex10));
			AssertTrue(graph.RemoveEdge(vertex4, vertex5));
			AssertTrue(graph.Connected(vertex1, vertex3));
			AssertTrue(graph.Connected(vertex2, vertex4));
			AssertTrue(graph.Connected(vertex5, vertex6));
			AssertTrue(graph.Connected(vertex5, vertex7));
			AssertTrue(graph.Connected(vertex8, vertex9));
			AssertTrue(graph.Connected(vertex3, vertex3));
			AssertFalse(graph.Connected(vertex1, vertex5));
			AssertFalse(graph.Connected(vertex4, vertex7));
			AssertFalse(graph.Connected(vertex1, vertex8));
			AssertFalse(graph.Connected(vertex6, vertex9));

			ISet<ConnVertex> expectedAdjVertices = new HashSet<ConnVertex>();
			expectedAdjVertices.Add(vertex2);
			expectedAdjVertices.Add(vertex3);
			expectedAdjVertices.Add(vertex4);
			AssertEquals(expectedAdjVertices, new HashSet<ConnVertex>(graph.AdjacentVertices(vertex1)));
			expectedAdjVertices.Clear();
			expectedAdjVertices.Add(vertex5);
			expectedAdjVertices.Add(vertex7);
			AssertEqualsUnordered(expectedAdjVertices, new HashSet<ConnVertex>(graph.AdjacentVertices(vertex6)));
			AssertEqualsUnordered(new[]{vertex8}, new HashSet<ConnVertex>(graph.AdjacentVertices(vertex9)));
			AssertEqualsUnordered(new ConnVertex[0], new HashSet<ConnVertex>(graph.AdjacentVertices(new ConnVertex(random))));
			graph.Optimize();

			IList<ConnVertex> vertices = new List<ConnVertex>(1000);
			for (int i = 0; i < 1000; i++)
			{
				vertices.Add(new ConnVertex(random));
			}
			for (int i = 0; i < 1000; i++)
			{
				if (i > 0 && BitCount(i) <= 3)
				{
					graph.AddEdge(vertices[i], vertices[(i - 1) / 2]);
				}
			}
			for (int i = 0; i < 1000; i++)
			{
				if (BitCount(i) > 3)
				{
					graph.AddEdge(vertices[(i - 1) / 2], vertices[i]);
				}
			}
			for (int i = 15; i < 31; i++)
			{
				graph.RemoveEdge(vertices[i], vertices[(i - 1) / 2]);
			}
			AssertTrue(graph.Connected(vertices[0], vertices[0]));
			AssertTrue(graph.Connected(vertices[11], vertices[2]));
			AssertTrue(graph.Connected(vertices[7], vertices[14]));
			AssertTrue(graph.Connected(vertices[0], vertices[10]));
			AssertFalse(graph.Connected(vertices[0], vertices[15]));
			AssertFalse(graph.Connected(vertices[15], vertices[16]));
			AssertFalse(graph.Connected(vertices[14], vertices[15]));
			AssertFalse(graph.Connected(vertices[7], vertices[605]));
			AssertFalse(graph.Connected(vertices[5], vertices[87]));
			AssertTrue(graph.Connected(vertices[22], vertices[22]));
			AssertTrue(graph.Connected(vertices[16], vertices[70]));
			AssertTrue(graph.Connected(vertices[113], vertices[229]));
			AssertTrue(graph.Connected(vertices[21], vertices[715]));
			AssertTrue(graph.Connected(vertices[175], vertices[715]));
			AssertTrue(graph.Connected(vertices[30], vertices[999]));
			AssertTrue(graph.Connected(vertices[991], vertices[999]));
		}

		/// <summary>
		/// Tests ConnectivityGraph on a small graph that has cycles. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testSmallCycles()
		[Fact]
		public virtual void TestSmallCycles()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex1 = new ConnVertex(random);
			ConnVertex vertex2 = new ConnVertex(random);
			ConnVertex vertex3 = new ConnVertex(random);
			ConnVertex vertex4 = new ConnVertex(random);
			ConnVertex vertex5 = new ConnVertex(random);
			AssertTrue(graph.AddEdge(vertex1, vertex2));
			AssertTrue(graph.AddEdge(vertex2, vertex3));
			AssertTrue(graph.AddEdge(vertex1, vertex3));
			AssertTrue(graph.AddEdge(vertex2, vertex4));
			AssertTrue(graph.AddEdge(vertex3, vertex4));
			AssertTrue(graph.AddEdge(vertex4, vertex5));
			AssertTrue(graph.Connected(vertex5, vertex1));
			AssertTrue(graph.Connected(vertex1, vertex4));
			AssertTrue(graph.RemoveEdge(vertex4, vertex5));
			AssertFalse(graph.Connected(vertex4, vertex5));
			AssertFalse(graph.Connected(vertex5, vertex1));
			AssertTrue(graph.Connected(vertex1, vertex4));
			AssertTrue(graph.RemoveEdge(vertex1, vertex2));
			AssertTrue(graph.RemoveEdge(vertex3, vertex4));
			AssertTrue(graph.Connected(vertex1, vertex4));
			AssertTrue(graph.RemoveEdge(vertex2, vertex3));
			AssertTrue(graph.Connected(vertex1, vertex3));
			AssertTrue(graph.Connected(vertex2, vertex4));
			AssertFalse(graph.Connected(vertex1, vertex4));
		}

		/// <summary>
		/// Tests ConnectivityGraph on a grid-based graph. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testGrid()
		[Fact]
		public virtual void TestGrid()
		{
			ConnGraph graph = new ConnGraph();
			Random random = new Random(6170);
			ConnVertex vertex = new ConnVertex(random);
			AssertTrue(graph.Connected(vertex, vertex));

			graph = new ConnGraph(SumAndMax.augmentation);
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
					AssertTrue(graph.AddEdge(vertices[y][x], vertices[y][x + 1]));
					AssertTrue(graph.AddEdge(vertices[y][x], vertices[y + 1][x]));
				}
			}
			graph.Optimize();

			AssertTrue(graph.Connected(vertices[0][0], vertices[15][12]));
			AssertTrue(graph.Connected(vertices[0][0], vertices[18][19]));
			AssertFalse(graph.Connected(vertices[0][0], vertices[19][19]));
			AssertFalse(graph.RemoveEdge(vertices[18][19], vertices[19][19]));
			AssertFalse(graph.RemoveEdge(vertices[0][0], vertices[2][2]));

			AssertTrue(graph.RemoveEdge(vertices[12][8], vertices[11][8]));
			AssertTrue(graph.RemoveEdge(vertices[12][9], vertices[11][9]));
			AssertTrue(graph.RemoveEdge(vertices[12][8], vertices[12][7]));
			AssertTrue(graph.RemoveEdge(vertices[13][8], vertices[13][7]));
			AssertTrue(graph.RemoveEdge(vertices[13][8], vertices[14][8]));
			AssertTrue(graph.RemoveEdge(vertices[12][9], vertices[12][10]));
			AssertTrue(graph.RemoveEdge(vertices[13][9], vertices[13][10]));
			AssertTrue(graph.Connected(vertices[2][1], vertices[12][8]));
			AssertTrue(graph.Connected(vertices[12][8], vertices[13][9]));
			AssertTrue(graph.RemoveEdge(vertices[13][9], vertices[14][9]));
			AssertFalse(graph.Connected(vertices[2][1], vertices[12][8]));
			AssertTrue(graph.Connected(vertices[12][8], vertices[13][9]));
			AssertFalse(graph.Connected(vertices[11][8], vertices[12][8]));
			AssertTrue(graph.Connected(vertices[16][18], vertices[6][15]));
			AssertTrue(graph.RemoveEdge(vertices[12][9], vertices[12][8]));
			AssertTrue(graph.RemoveEdge(vertices[12][8], vertices[13][8]));
			AssertFalse(graph.Connected(vertices[2][1], vertices[12][8]));
			AssertFalse(graph.Connected(vertices[12][8], vertices[13][9]));
			AssertFalse(graph.Connected(vertices[11][8], vertices[12][8]));
			AssertTrue(graph.Connected(vertices[13][8], vertices[12][9]));

			AssertTrue(graph.RemoveEdge(vertices[6][15], vertices[5][15]));
			AssertTrue(graph.RemoveEdge(vertices[6][15], vertices[7][15]));
			AssertTrue(graph.RemoveEdge(vertices[6][15], vertices[6][14]));
			AssertTrue(graph.RemoveEdge(vertices[6][15], vertices[6][16]));
			AssertFalse(graph.RemoveEdge(vertices[6][15], vertices[5][15]));
			AssertFalse(graph.Connected(vertices[16][18], vertices[6][15]));
			AssertFalse(graph.Connected(vertices[7][15], vertices[6][15]));
			graph.AddEdge(vertices[6][15], vertices[7][15]);
			AssertTrue(graph.Connected(vertices[16][18], vertices[6][15]));

			for (int y = 1; y < 19; y++)
			{
				for (int x = 1; x < 19; x++)
				{
					graph.RemoveEdge(vertices[y][x], vertices[y][x + 1]);
					graph.RemoveEdge(vertices[y][x], vertices[y + 1][x]);
				}
			}

			AssertTrue(graph.AddEdge(vertices[5][6], vertices[0][7]));
			AssertTrue(graph.AddEdge(vertices[12][8], vertices[5][6]));
			AssertTrue(graph.Connected(vertices[5][6], vertices[14][0]));
			AssertTrue(graph.Connected(vertices[12][8], vertices[0][17]));
			AssertFalse(graph.Connected(vertices[3][5], vertices[0][9]));
			AssertFalse(graph.Connected(vertices[14][2], vertices[11][18]));

			AssertNull(graph.GetVertexAugmentation(vertices[13][8]));
			AssertNull(graph.GetVertexAugmentation(vertices[6][4]));
			AssertNull(graph.GetComponentAugmentation(vertices[13][8]));
			AssertNull(graph.GetComponentAugmentation(vertices[6][4]));
			AssertFalse(graph.VertexHasAugmentation(vertices[13][8]));
			AssertFalse(graph.VertexHasAugmentation(vertices[6][4]));
			AssertFalse(graph.ComponentHasAugmentation(vertices[13][8]));
			AssertFalse(graph.ComponentHasAugmentation(vertices[6][4]));
		}

		/// <summary>
		/// Tests a graph with a hub-and-spokes subgraph and a clique subgraph. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testWheelAndClique()
		[Fact]
		public virtual void TestWheelAndClique()
		{
			ConnGraph graph = new ConnGraph(SumAndMax.augmentation);
			Random random = new Random(6170);
			ConnVertex hub = new ConnVertex(random);
			IList<ConnVertex> spokes1 = new List<ConnVertex>(10);
			IList<ConnVertex> spokes2 = new List<ConnVertex>(10);
			for (int i = 0; i < 10; i++)
			{
				ConnVertex spoke1 = new ConnVertex(random);
				ConnVertex spoke2 = new ConnVertex(random);
				AssertTrue(graph.AddEdge(spoke1, spoke2));
				AssertNull(graph.SetVertexAugmentation(spoke1, new SumAndMax(i, i)));
				AssertNull(graph.SetVertexAugmentation(spoke2, new SumAndMax(i, i + 10)));
				spokes1.Add(spoke1);
				spokes2.Add(spoke2);
			}
			for (int i = 0; i < 10; i++)
			{
				AssertTrue(graph.AddEdge(spokes1[i], hub));
			}
			for (int i = 0; i < 10; i++)
			{
				AssertTrue(graph.AddEdge(hub, spokes2[i]));
			}

			IList<ConnVertex> clique = new List<ConnVertex>(10);
			for (int i = 0; i < 10; i++)
			{
				ConnVertex vertex = new ConnVertex(random);
				AssertNull(graph.SetVertexAugmentation(vertex, new SumAndMax(i, i + 20)));
				clique.Add(vertex);
			}
			for (int i = 0; i < 10; i++)
			{
				for (int j = i + 1; j < 10; j++)
				{
					AssertTrue(graph.AddEdge(clique[i], clique[j]));
				}
			}
			AssertTrue(graph.AddEdge(hub, clique[0]));

			AssertTrue(graph.Connected(spokes1[5], clique[3]));
			AssertTrue(graph.Connected(spokes1[3], spokes2[8]));
			AssertTrue(graph.Connected(spokes1[4], spokes2[4]));
			AssertTrue(graph.Connected(clique[5], hub));
			SumAndMax expectedAugmentation = new SumAndMax(135, 29);
			AssertEquals(expectedAugmentation, graph.GetComponentAugmentation(spokes2[8]));
			AssertTrue(graph.ComponentHasAugmentation(spokes2[8]));
			AssertEquals(expectedAugmentation, graph.GetComponentAugmentation(hub));
			AssertEquals(expectedAugmentation, graph.GetComponentAugmentation(clique[9]));
			AssertEquals(new SumAndMax(4, 4), graph.GetVertexAugmentation(spokes1[4]));
			AssertTrue(graph.VertexHasAugmentation(spokes1[4]));
			AssertNull(graph.GetVertexAugmentation(hub));
			AssertFalse(graph.VertexHasAugmentation(hub));

			AssertTrue(graph.RemoveEdge(spokes1[5], hub));
			AssertTrue(graph.Connected(spokes1[5], clique[2]));
			AssertTrue(graph.Connected(spokes1[5], spokes1[8]));
			AssertTrue(graph.Connected(spokes1[5], spokes2[5]));
			AssertEquals(new SumAndMax(135, 29), graph.GetComponentAugmentation(hub));
			AssertTrue(graph.RemoveEdge(spokes2[5], hub));
			AssertFalse(graph.Connected(spokes1[5], clique[2]));
			AssertFalse(graph.Connected(spokes1[5], spokes1[8]));
			AssertTrue(graph.Connected(spokes1[5], spokes2[5]));
			AssertEquals(new SumAndMax(125, 29), graph.GetComponentAugmentation(hub));
			AssertTrue(graph.AddEdge(spokes1[5], hub));
			AssertTrue(graph.Connected(spokes1[5], clique[2]));
			AssertTrue(graph.Connected(spokes1[5], spokes1[8]));
			AssertTrue(graph.Connected(spokes1[5], spokes2[5]));
			AssertEquals(new SumAndMax(135, 29), graph.GetComponentAugmentation(hub));

			AssertTrue(graph.RemoveEdge(hub, clique[0]));
			AssertFalse(graph.Connected(spokes1[3], clique[4]));
			AssertTrue(graph.Connected(spokes2[7], hub));
			AssertFalse(graph.Connected(hub, clique[0]));
			AssertTrue(graph.Connected(spokes2[9], spokes1[5]));
			AssertEquals(new SumAndMax(90, 19), graph.GetComponentAugmentation(hub));
			AssertEquals(new SumAndMax(90, 19), graph.GetComponentAugmentation(spokes2[4]));
			AssertEquals(new SumAndMax(45, 29), graph.GetComponentAugmentation(clique[1]));

			AssertEquals(new SumAndMax(9, 29), graph.SetVertexAugmentation(clique[9], new SumAndMax(-20, 4)));
			for (int i = 0; i < 10; i++)
			{
				AssertEquals(new SumAndMax(i, i + 10), graph.SetVertexAugmentation(spokes2[i], new SumAndMax(i - 1, i)));
			}
			AssertNull(graph.RemoveVertexAugmentation(hub));
			AssertEquals(new SumAndMax(4, 4), graph.RemoveVertexAugmentation(spokes1[4]));
			AssertEquals(new SumAndMax(6, 7), graph.RemoveVertexAugmentation(spokes2[7]));

			AssertEquals(new SumAndMax(70, 9), graph.GetComponentAugmentation(hub));
			AssertTrue(graph.ComponentHasAugmentation(hub));
			AssertEquals(new SumAndMax(70, 9), graph.GetComponentAugmentation(spokes1[6]));
			AssertEquals(new SumAndMax(16, 28), graph.GetComponentAugmentation(clique[4]));

			AssertTrue(graph.AddEdge(hub, clique[1]));
			expectedAugmentation = new SumAndMax(86, 28);
			AssertEquals(expectedAugmentation, graph.GetComponentAugmentation(hub));
			AssertTrue(graph.ComponentHasAugmentation(hub));
			AssertEquals(expectedAugmentation, graph.GetComponentAugmentation(spokes2[7]));
			AssertEquals(expectedAugmentation, graph.GetComponentAugmentation(clique[3]));

			for (int i = 0; i < 10; i++)
			{
				AssertTrue(graph.RemoveEdge(hub, spokes1[i]));
				if (i != 5)
				{
					AssertTrue(graph.RemoveEdge(hub, spokes2[i]));
				}
			}
			AssertFalse(graph.Connected(hub, spokes1[8]));
			AssertFalse(graph.Connected(hub, spokes2[4]));
			AssertTrue(graph.Connected(hub, clique[5]));

			graph.Clear();
			AssertTrue(graph.AddEdge(hub, spokes1[0]));
			AssertTrue(graph.AddEdge(hub, spokes2[0]));
			AssertTrue(graph.AddEdge(spokes1[0], spokes2[0]));
			AssertTrue(graph.Connected(hub, spokes1[0]));
			AssertFalse(graph.Connected(hub, spokes2[4]));
			AssertTrue(graph.Connected(clique[5], clique[5]));
			AssertNull(graph.GetComponentAugmentation(hub));
			AssertNull(graph.GetVertexAugmentation(spokes2[8]));
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
		private int[] SetPermutation(ConnGraph graph, IList<IList<ConnVertex>> vertices, int columnIndex, int[] oldPermutation, int[] newPermutation)
		{
			IList<ConnVertex> column1 = vertices[columnIndex];
			IList<ConnVertex> column2 = vertices[columnIndex + 1];
			if (oldPermutation != null)
			{
				for (int i = 0; i < oldPermutation.Length; i++)
				{
					AssertTrue(graph.RemoveEdge(column1[i], column2[oldPermutation[i]]));
				}
			}
			for (int i = 0; i < newPermutation.Length; i++)
			{
				AssertTrue(graph.AddEdge(column1[i], column2[newPermutation[i]]));
			}
			return newPermutation;
		}

		/// <summary>
		/// Asserts that the specified permutation is the correct composite permutation for the specified column, i.e. that
		/// for all i, vertices.get(0).get(i) is in the same connected component as
		/// vertices.get(columnIndex + 1).get(expectedPermutation[i]). See the comments for the implementation of
		/// testPermutations().
		/// </summary>
		private void CheckPermutation(ConnGraph graph, IList<IList<ConnVertex>> vertices, int columnIndex, int[] expectedPermutation)
		{
			IList<ConnVertex> firstColumn = vertices[0];
			IList<ConnVertex> column = vertices[columnIndex + 1];
			for (int i = 0; i < expectedPermutation.Length; i++)
			{
				AssertTrue(graph.Connected(firstColumn[i], column[expectedPermutation[i]]));
			}
		}

		/// <summary>
		/// Asserts that the specified permutation differs from the correct composite permutation for the specified column in
		/// every position, i.e. that for all i, vertices.get(0).get(i) is in a different connected component from
		/// vertices.get(columnIndex + 1).get(wrongPermutation[i]). See the comments for the implementation of
		/// testPermutations().
		/// </summary>
		private void CheckWrongPermutation(ConnGraph graph, IList<IList<ConnVertex>> vertices, int columnIndex, int[] wrongPermutation)
		{
			IList<ConnVertex> firstColumn = vertices[0];
			IList<ConnVertex> column = vertices[columnIndex + 1];
			for (int i = 0; i < wrongPermutation.Length; i++)
			{
				AssertFalse(graph.Connected(firstColumn[i], column[wrongPermutation[i]]));
			}
		}

		/// <summary>
		/// Tests a graph in the style used to prove lower bounds on the performance of dynamic connectivity, as presented in
		/// https://ocw.mit.edu/courses/electrical-engineering-and-computer-science/6-851-advanced-data-structures-spring-2012/lecture-videos/session-21-dynamic-connectivity-lower-bound/ .
		/// </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testPermutations()
		[Fact]
		public virtual void TestPermutations()
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

			int[] permutation0 = SetPermutation(graph, vertices, 0, null, new int[]{2, 5, 0, 4, 7, 1, 3, 6});
			int[] permutation1 = SetPermutation(graph, vertices, 1, null, new int[]{6, 5, 0, 7, 1, 2, 4, 3});
			int[] permutation2 = SetPermutation(graph, vertices, 2, null, new int[]{2, 1, 7, 5, 6, 0, 4, 3});
			int[] permutation3 = SetPermutation(graph, vertices, 3, null, new int[]{5, 2, 4, 6, 3, 0, 7, 1});
			int[] permutation4 = SetPermutation(graph, vertices, 4, null, new int[]{5, 0, 2, 7, 4, 3, 1, 6});
			int[] permutation5 = SetPermutation(graph, vertices, 5, null, new int[]{4, 7, 0, 1, 3, 6, 2, 5});
			int[] permutation6 = SetPermutation(graph, vertices, 6, null, new int[]{4, 5, 3, 1, 7, 6, 2, 0});
			int[] permutation7 = SetPermutation(graph, vertices, 7, null, new int[]{6, 7, 3, 0, 5, 1, 2, 4});

			permutation0 = SetPermutation(graph, vertices, 0, permutation0, new int[]{7, 5, 3, 0, 4, 2, 1, 6});
			CheckWrongPermutation(graph, vertices, 0, new int[]{5, 3, 0, 4, 2, 1, 6, 7});
			CheckPermutation(graph, vertices, 0, new int[]{7, 5, 3, 0, 4, 2, 1, 6});
			permutation4 = SetPermutation(graph, vertices, 4, permutation4, new int[]{2, 7, 0, 6, 5, 4, 1, 3});
			CheckWrongPermutation(graph, vertices, 4, new int[]{7, 1, 6, 0, 5, 4, 3, 2});
			CheckPermutation(graph, vertices, 4, new int[]{2, 7, 1, 6, 0, 5, 4, 3});
			permutation2 = SetPermutation(graph, vertices, 2, permutation2, new int[]{3, 5, 6, 1, 4, 2, 7, 0});
			CheckWrongPermutation(graph, vertices, 2, new int[]{6, 0, 7, 5, 3, 2, 4, 1});
			CheckPermutation(graph, vertices, 2, new int[]{1, 6, 0, 7, 5, 3, 2, 4});
			permutation6 = SetPermutation(graph, vertices, 6, permutation6, new int[]{4, 7, 1, 3, 6, 0, 5, 2});
			CheckWrongPermutation(graph, vertices, 6, new int[]{7, 3, 0, 4, 2, 5, 1, 6});
			CheckPermutation(graph, vertices, 6, new int[]{6, 7, 3, 0, 4, 2, 5, 1});
			permutation1 = SetPermutation(graph, vertices, 1, permutation1, new int[]{2, 4, 0, 5, 6, 3, 7, 1});
			CheckWrongPermutation(graph, vertices, 1, new int[]{3, 5, 2, 6, 0, 4, 7, 1});
			CheckPermutation(graph, vertices, 1, new int[]{1, 3, 5, 2, 6, 0, 4, 7});
			permutation5 = SetPermutation(graph, vertices, 5, permutation5, new int[]{5, 3, 2, 0, 7, 1, 6, 4});
			CheckWrongPermutation(graph, vertices, 5, new int[]{5, 1, 0, 4, 3, 6, 7, 2});
			CheckPermutation(graph, vertices, 5, new int[]{2, 5, 1, 0, 4, 3, 6, 7});
			permutation3 = SetPermutation(graph, vertices, 3, permutation3, new int[]{1, 7, 3, 0, 4, 5, 6, 2});
			CheckWrongPermutation(graph, vertices, 3, new int[]{7, 3, 6, 2, 0, 4, 1, 5});
			CheckPermutation(graph, vertices, 3, new int[]{5, 7, 3, 6, 2, 0, 4, 1});
			permutation7 = SetPermutation(graph, vertices, 7, permutation7, new int[]{4, 7, 5, 6, 2, 0, 1, 3});
			CheckWrongPermutation(graph, vertices, 7, new int[]{2, 0, 6, 4, 7, 3, 1, 5});
			CheckPermutation(graph, vertices, 7, new int[]{5, 2, 0, 6, 4, 7, 3, 1});
		}

		/// <summary>
		/// Tests a graph based on the United States. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testUnitedStates()
		[Fact]
		public virtual void TestUnitedStates()
		{
			ConnGraph graph = new ConnGraph(SumAndMax.augmentation);
			Random random = new Random(6170);
			ConnVertex alabama = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(alabama, new SumAndMax(7, 1819)));
			ConnVertex alaska = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(alaska, new SumAndMax(1, 1959)));
			ConnVertex arizona = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(arizona, new SumAndMax(9, 1912)));
			ConnVertex arkansas = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(arkansas, new SumAndMax(4, 1836)));
			ConnVertex california = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(california, new SumAndMax(53, 1850)));
			ConnVertex colorado = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(colorado, new SumAndMax(7, 1876)));
			ConnVertex connecticut = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(connecticut, new SumAndMax(5, 1788)));
			ConnVertex delaware = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(delaware, new SumAndMax(1, 1787)));
			ConnVertex florida = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(florida, new SumAndMax(27, 1845)));
			ConnVertex georgia = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(georgia, new SumAndMax(14, 1788)));
			ConnVertex hawaii = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(hawaii, new SumAndMax(2, 1959)));
			ConnVertex idaho = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(idaho, new SumAndMax(2, 1890)));
			ConnVertex illinois = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(illinois, new SumAndMax(18, 1818)));
			ConnVertex indiana = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(indiana, new SumAndMax(9, 1816)));
			ConnVertex iowa = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(iowa, new SumAndMax(4, 1846)));
			ConnVertex kansas = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(kansas, new SumAndMax(4, 1861)));
			ConnVertex kentucky = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(kentucky, new SumAndMax(6, 1792)));
			ConnVertex louisiana = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(louisiana, new SumAndMax(6, 1812)));
			ConnVertex maine = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(maine, new SumAndMax(2, 1820)));
			ConnVertex maryland = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(maryland, new SumAndMax(8, 1788)));
			ConnVertex massachusetts = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(massachusetts, new SumAndMax(9, 1788)));
			ConnVertex michigan = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(michigan, new SumAndMax(14, 1837)));
			ConnVertex minnesota = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(minnesota, new SumAndMax(8, 1858)));
			ConnVertex mississippi = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(mississippi, new SumAndMax(4, 1817)));
			ConnVertex missouri = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(missouri, new SumAndMax(8, 1821)));
			ConnVertex montana = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(montana, new SumAndMax(1, 1889)));
			ConnVertex nebraska = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(nebraska, new SumAndMax(3, 1867)));
			ConnVertex nevada = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(nevada, new SumAndMax(4, 1864)));
			ConnVertex newHampshire = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(newHampshire, new SumAndMax(2, 1788)));
			ConnVertex newJersey = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(newJersey, new SumAndMax(12, 1787)));
			ConnVertex newMexico = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(newMexico, new SumAndMax(3, 1912)));
			ConnVertex newYork = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(newYork, new SumAndMax(27, 1788)));
			ConnVertex northCarolina = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(northCarolina, new SumAndMax(13, 1789)));
			ConnVertex northDakota = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(northDakota, new SumAndMax(1, 1889)));
			ConnVertex ohio = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(ohio, new SumAndMax(16, 1803)));
			ConnVertex oklahoma = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(oklahoma, new SumAndMax(5, 1907)));
			ConnVertex oregon = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(oregon, new SumAndMax(5, 1859)));
			ConnVertex pennsylvania = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(pennsylvania, new SumAndMax(18, 1787)));
			ConnVertex rhodeIsland = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(rhodeIsland, new SumAndMax(2, 1790)));
			ConnVertex southCarolina = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(southCarolina, new SumAndMax(7, 1788)));
			ConnVertex southDakota = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(southDakota, new SumAndMax(1, 1889)));
			ConnVertex tennessee = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(tennessee, new SumAndMax(9, 1796)));
			ConnVertex texas = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(texas, new SumAndMax(36, 1845)));
			ConnVertex utah = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(utah, new SumAndMax(4, 1896)));
			ConnVertex vermont = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(vermont, new SumAndMax(1, 1791)));
			ConnVertex virginia = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(virginia, new SumAndMax(11, 1788)));
			ConnVertex washington = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(washington, new SumAndMax(10, 1889)));
			ConnVertex westVirginia = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(westVirginia, new SumAndMax(3, 1863)));
			ConnVertex wisconsin = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(wisconsin, new SumAndMax(8, 1848)));
			ConnVertex wyoming = new ConnVertex(random);
			AssertNull(graph.SetVertexAugmentation(wyoming, new SumAndMax(1, 1890)));

			AssertTrue(graph.AddEdge(alabama, florida));
			AssertTrue(graph.AddEdge(alabama, georgia));
			AssertTrue(graph.AddEdge(alabama, mississippi));
			AssertTrue(graph.AddEdge(alabama, tennessee));
			AssertTrue(graph.AddEdge(arizona, california));
			AssertTrue(graph.AddEdge(arizona, colorado));
			AssertTrue(graph.AddEdge(arizona, nevada));
			AssertTrue(graph.AddEdge(arizona, newMexico));
			AssertTrue(graph.AddEdge(arizona, utah));
			AssertTrue(graph.AddEdge(arkansas, louisiana));
			AssertTrue(graph.AddEdge(arkansas, mississippi));
			AssertTrue(graph.AddEdge(arkansas, missouri));
			AssertTrue(graph.AddEdge(arkansas, oklahoma));
			AssertTrue(graph.AddEdge(arkansas, tennessee));
			AssertTrue(graph.AddEdge(arkansas, texas));
			AssertTrue(graph.AddEdge(california, nevada));
			AssertTrue(graph.AddEdge(california, oregon));
			AssertTrue(graph.AddEdge(colorado, kansas));
			AssertTrue(graph.AddEdge(colorado, nebraska));
			AssertTrue(graph.AddEdge(colorado, newMexico));
			AssertTrue(graph.AddEdge(colorado, oklahoma));
			AssertTrue(graph.AddEdge(colorado, utah));
			AssertTrue(graph.AddEdge(colorado, wyoming));
			AssertTrue(graph.AddEdge(connecticut, massachusetts));
			AssertTrue(graph.AddEdge(connecticut, newYork));
			AssertTrue(graph.AddEdge(connecticut, rhodeIsland));
			AssertTrue(graph.AddEdge(delaware, maryland));
			AssertTrue(graph.AddEdge(delaware, newJersey));
			AssertTrue(graph.AddEdge(delaware, pennsylvania));
			AssertTrue(graph.AddEdge(florida, georgia));
			AssertTrue(graph.AddEdge(georgia, northCarolina));
			AssertTrue(graph.AddEdge(georgia, southCarolina));
			AssertTrue(graph.AddEdge(georgia, tennessee));
			AssertTrue(graph.AddEdge(idaho, montana));
			AssertTrue(graph.AddEdge(idaho, nevada));
			AssertTrue(graph.AddEdge(idaho, oregon));
			AssertTrue(graph.AddEdge(idaho, utah));
			AssertTrue(graph.AddEdge(idaho, washington));
			AssertTrue(graph.AddEdge(idaho, wyoming));
			AssertTrue(graph.AddEdge(illinois, indiana));
			AssertTrue(graph.AddEdge(illinois, iowa));
			AssertTrue(graph.AddEdge(illinois, kentucky));
			AssertTrue(graph.AddEdge(illinois, missouri));
			AssertTrue(graph.AddEdge(illinois, wisconsin));
			AssertTrue(graph.AddEdge(indiana, kentucky));
			AssertTrue(graph.AddEdge(indiana, michigan));
			AssertTrue(graph.AddEdge(indiana, ohio));
			AssertTrue(graph.AddEdge(iowa, minnesota));
			AssertTrue(graph.AddEdge(iowa, missouri));
			AssertTrue(graph.AddEdge(iowa, nebraska));
			AssertTrue(graph.AddEdge(iowa, southDakota));
			AssertTrue(graph.AddEdge(iowa, wisconsin));
			AssertTrue(graph.AddEdge(kansas, missouri));
			AssertTrue(graph.AddEdge(kansas, nebraska));
			AssertTrue(graph.AddEdge(kansas, oklahoma));
			AssertTrue(graph.AddEdge(kentucky, missouri));
			AssertTrue(graph.AddEdge(kentucky, ohio));
			AssertTrue(graph.AddEdge(kentucky, tennessee));
			AssertTrue(graph.AddEdge(kentucky, virginia));
			AssertTrue(graph.AddEdge(kentucky, westVirginia));
			AssertTrue(graph.AddEdge(louisiana, mississippi));
			AssertTrue(graph.AddEdge(louisiana, texas));
			AssertTrue(graph.AddEdge(maine, newHampshire));
			AssertTrue(graph.AddEdge(maryland, pennsylvania));
			AssertTrue(graph.AddEdge(maryland, virginia));
			AssertTrue(graph.AddEdge(maryland, westVirginia));
			AssertTrue(graph.AddEdge(massachusetts, newHampshire));
			AssertTrue(graph.AddEdge(massachusetts, newYork));
			AssertTrue(graph.AddEdge(massachusetts, rhodeIsland));
			AssertTrue(graph.AddEdge(massachusetts, vermont));
			AssertTrue(graph.AddEdge(michigan, ohio));
			AssertTrue(graph.AddEdge(michigan, wisconsin));
			AssertTrue(graph.AddEdge(minnesota, northDakota));
			AssertTrue(graph.AddEdge(minnesota, southDakota));
			AssertTrue(graph.AddEdge(minnesota, wisconsin));
			AssertTrue(graph.AddEdge(mississippi, tennessee));
			AssertTrue(graph.AddEdge(missouri, nebraska));
			AssertTrue(graph.AddEdge(missouri, oklahoma));
			AssertTrue(graph.AddEdge(missouri, tennessee));
			AssertTrue(graph.AddEdge(montana, northDakota));
			AssertTrue(graph.AddEdge(montana, southDakota));
			AssertTrue(graph.AddEdge(montana, wyoming));
			AssertTrue(graph.AddEdge(nebraska, southDakota));
			AssertTrue(graph.AddEdge(nebraska, wyoming));
			AssertTrue(graph.AddEdge(nevada, oregon));
			AssertTrue(graph.AddEdge(nevada, utah));
			AssertTrue(graph.AddEdge(newHampshire, vermont));
			AssertTrue(graph.AddEdge(newJersey, newYork));
			AssertTrue(graph.AddEdge(newJersey, pennsylvania));
			AssertTrue(graph.AddEdge(newMexico, oklahoma));
			AssertTrue(graph.AddEdge(newMexico, texas));
			AssertTrue(graph.AddEdge(newMexico, utah));
			AssertTrue(graph.AddEdge(newYork, pennsylvania));
			AssertTrue(graph.AddEdge(newYork, vermont));
			AssertTrue(graph.AddEdge(northCarolina, southCarolina));
			AssertTrue(graph.AddEdge(northCarolina, tennessee));
			AssertTrue(graph.AddEdge(northCarolina, virginia));
			AssertTrue(graph.AddEdge(northDakota, southDakota));
			AssertTrue(graph.AddEdge(ohio, pennsylvania));
			AssertTrue(graph.AddEdge(ohio, westVirginia));
			AssertTrue(graph.AddEdge(oklahoma, texas));
			AssertTrue(graph.AddEdge(oregon, washington));
			AssertTrue(graph.AddEdge(pennsylvania, westVirginia));
			AssertTrue(graph.AddEdge(southDakota, wyoming));
			AssertTrue(graph.AddEdge(tennessee, virginia));
			AssertTrue(graph.AddEdge(utah, wyoming));
			AssertTrue(graph.AddEdge(virginia, westVirginia));

			AssertTrue(graph.Connected(florida, washington));
			AssertTrue(graph.Connected(rhodeIsland, michigan));
			AssertTrue(graph.Connected(delaware, texas));
			AssertFalse(graph.Connected(alaska, newYork));
			AssertFalse(graph.Connected(hawaii, idaho));
			AssertEquals(new SumAndMax(432, 1912), graph.GetComponentAugmentation(newJersey));
			AssertEquals(new SumAndMax(2, 1959), graph.GetComponentAugmentation(hawaii));

			// 2186: Aliens attack, split nation in two using lasers
			AssertTrue(graph.RemoveEdge(northDakota, minnesota));
			AssertTrue(graph.RemoveEdge(southDakota, minnesota));
			AssertTrue(graph.RemoveEdge(southDakota, iowa));
			AssertTrue(graph.RemoveEdge(nebraska, iowa));
			AssertTrue(graph.RemoveEdge(nebraska, missouri));
			AssertTrue(graph.RemoveEdge(kansas, missouri));
			AssertTrue(graph.RemoveEdge(oklahoma, missouri));
			AssertTrue(graph.RemoveEdge(oklahoma, arkansas));
			AssertTrue(graph.RemoveEdge(texas, arkansas));
			AssertTrue(graph.Connected(california, massachusetts));
			AssertTrue(graph.Connected(montana, virginia));
			AssertTrue(graph.Connected(idaho, southDakota));
			AssertTrue(graph.Connected(maine, tennessee));
			AssertEquals(new SumAndMax(432, 1912), graph.GetComponentAugmentation(vermont));
			AssertTrue(graph.RemoveEdge(texas, louisiana));
			AssertFalse(graph.Connected(california, massachusetts));
			AssertFalse(graph.Connected(montana, virginia));
			AssertTrue(graph.Connected(idaho, southDakota));
			AssertTrue(graph.Connected(maine, tennessee));
			AssertEquals(new SumAndMax(149, 1912), graph.GetComponentAugmentation(wyoming));
			AssertEquals(new SumAndMax(283, 1863), graph.GetComponentAugmentation(vermont));

			// 2254: California breaks off into ocean, secedes
			AssertTrue(graph.RemoveEdge(california, oregon));
			AssertTrue(graph.RemoveEdge(california, nevada));
			AssertTrue(graph.RemoveEdge(california, arizona));
			AssertEquals(new SumAndMax(53, 1850), graph.RemoveVertexAugmentation(california));
			AssertFalse(graph.Connected(california, utah));
			AssertFalse(graph.Connected(california, oregon));
			AssertNull(graph.GetComponentAugmentation(california));
			AssertEquals(new SumAndMax(96, 1912), graph.GetComponentAugmentation(washington));
			AssertEquals(new SumAndMax(283, 1863), graph.GetComponentAugmentation(vermont));

			// 2367: Nuclear armageddon
			AssertEquals(new SumAndMax(7, 1819), graph.RemoveVertexAugmentation(alabama));
			AssertTrue(graph.RemoveEdge(alabama, florida));
			AssertTrue(graph.RemoveEdge(alabama, georgia));
			AssertTrue(graph.RemoveEdge(alabama, mississippi));
			AssertTrue(graph.RemoveEdge(alabama, tennessee));
			AssertEquals(new SumAndMax(1, 1959), graph.RemoveVertexAugmentation(alaska));
			AssertEquals(new SumAndMax(9, 1912), graph.RemoveVertexAugmentation(arizona));
			AssertTrue(graph.RemoveEdge(arizona, colorado));
			AssertTrue(graph.RemoveEdge(arizona, nevada));
			AssertTrue(graph.RemoveEdge(arizona, newMexico));
			AssertTrue(graph.RemoveEdge(arizona, utah));
			AssertEquals(new SumAndMax(4, 1836), graph.RemoveVertexAugmentation(arkansas));
			AssertTrue(graph.RemoveEdge(arkansas, louisiana));
			AssertTrue(graph.RemoveEdge(arkansas, mississippi));
			AssertTrue(graph.RemoveEdge(arkansas, missouri));
			AssertTrue(graph.RemoveEdge(arkansas, tennessee));
			AssertEquals(new SumAndMax(7, 1876), graph.RemoveVertexAugmentation(colorado));
			AssertTrue(graph.RemoveEdge(colorado, kansas));
			AssertTrue(graph.RemoveEdge(colorado, nebraska));
			AssertTrue(graph.RemoveEdge(colorado, newMexico));
			AssertTrue(graph.RemoveEdge(colorado, oklahoma));
			AssertTrue(graph.RemoveEdge(colorado, utah));
			AssertTrue(graph.RemoveEdge(colorado, wyoming));
			AssertEquals(new SumAndMax(5, 1788), graph.RemoveVertexAugmentation(connecticut));
			AssertTrue(graph.RemoveEdge(connecticut, massachusetts));
			AssertTrue(graph.RemoveEdge(connecticut, newYork));
			AssertTrue(graph.RemoveEdge(connecticut, rhodeIsland));
			AssertEquals(new SumAndMax(1, 1787), graph.RemoveVertexAugmentation(delaware));
			AssertTrue(graph.RemoveEdge(delaware, maryland));
			AssertTrue(graph.RemoveEdge(delaware, newJersey));
			AssertTrue(graph.RemoveEdge(delaware, pennsylvania));
			AssertEquals(new SumAndMax(27, 1845), graph.RemoveVertexAugmentation(florida));
			AssertTrue(graph.RemoveEdge(florida, georgia));
			AssertEquals(new SumAndMax(14, 1788), graph.RemoveVertexAugmentation(georgia));
			AssertTrue(graph.RemoveEdge(georgia, northCarolina));
			AssertTrue(graph.RemoveEdge(georgia, southCarolina));
			AssertTrue(graph.RemoveEdge(georgia, tennessee));
			AssertEquals(new SumAndMax(2, 1959), graph.RemoveVertexAugmentation(hawaii));
			AssertEquals(new SumAndMax(2, 1890), graph.RemoveVertexAugmentation(idaho));
			AssertTrue(graph.RemoveEdge(idaho, montana));
			AssertTrue(graph.RemoveEdge(idaho, nevada));
			AssertTrue(graph.RemoveEdge(idaho, oregon));
			AssertTrue(graph.RemoveEdge(idaho, utah));
			AssertTrue(graph.RemoveEdge(idaho, washington));
			AssertTrue(graph.RemoveEdge(idaho, wyoming));
			AssertEquals(new SumAndMax(18, 1818), graph.RemoveVertexAugmentation(illinois));
			AssertTrue(graph.RemoveEdge(illinois, indiana));
			AssertTrue(graph.RemoveEdge(illinois, iowa));
			AssertTrue(graph.RemoveEdge(illinois, kentucky));
			AssertTrue(graph.RemoveEdge(illinois, missouri));
			AssertTrue(graph.RemoveEdge(illinois, wisconsin));
			AssertEquals(new SumAndMax(9, 1816), graph.RemoveVertexAugmentation(indiana));
			AssertTrue(graph.RemoveEdge(indiana, kentucky));
			AssertTrue(graph.RemoveEdge(indiana, michigan));
			AssertTrue(graph.RemoveEdge(indiana, ohio));
			AssertEquals(new SumAndMax(4, 1846), graph.RemoveVertexAugmentation(iowa));
			AssertTrue(graph.RemoveEdge(iowa, minnesota));
			AssertTrue(graph.RemoveEdge(iowa, missouri));
			AssertTrue(graph.RemoveEdge(iowa, wisconsin));
			AssertEquals(new SumAndMax(4, 1861), graph.RemoveVertexAugmentation(kansas));
			AssertTrue(graph.RemoveEdge(kansas, nebraska));
			AssertTrue(graph.RemoveEdge(kansas, oklahoma));
			AssertEquals(new SumAndMax(6, 1792), graph.RemoveVertexAugmentation(kentucky));
			AssertTrue(graph.RemoveEdge(kentucky, missouri));
			AssertTrue(graph.RemoveEdge(kentucky, ohio));
			AssertTrue(graph.RemoveEdge(kentucky, tennessee));
			AssertTrue(graph.RemoveEdge(kentucky, virginia));
			AssertTrue(graph.RemoveEdge(kentucky, westVirginia));
			AssertEquals(new SumAndMax(6, 1812), graph.RemoveVertexAugmentation(louisiana));
			AssertTrue(graph.RemoveEdge(louisiana, mississippi));
			AssertEquals(new SumAndMax(2, 1820), graph.RemoveVertexAugmentation(maine));
			AssertTrue(graph.RemoveEdge(maine, newHampshire));
			AssertEquals(new SumAndMax(8, 1788), graph.RemoveVertexAugmentation(maryland));
			AssertTrue(graph.RemoveEdge(maryland, pennsylvania));
			AssertTrue(graph.RemoveEdge(maryland, virginia));
			AssertTrue(graph.RemoveEdge(maryland, westVirginia));
			AssertEquals(new SumAndMax(9, 1788), graph.RemoveVertexAugmentation(massachusetts));
			AssertTrue(graph.RemoveEdge(massachusetts, newHampshire));
			AssertTrue(graph.RemoveEdge(massachusetts, newYork));
			AssertTrue(graph.RemoveEdge(massachusetts, rhodeIsland));
			AssertTrue(graph.RemoveEdge(massachusetts, vermont));
			AssertEquals(new SumAndMax(14, 1837), graph.RemoveVertexAugmentation(michigan));
			AssertTrue(graph.RemoveEdge(michigan, ohio));
			AssertTrue(graph.RemoveEdge(michigan, wisconsin));
			AssertEquals(new SumAndMax(8, 1858), graph.RemoveVertexAugmentation(minnesota));
			AssertTrue(graph.RemoveEdge(minnesota, wisconsin));
			AssertEquals(new SumAndMax(4, 1817), graph.RemoveVertexAugmentation(mississippi));
			AssertTrue(graph.RemoveEdge(mississippi, tennessee));
			AssertEquals(new SumAndMax(8, 1821), graph.RemoveVertexAugmentation(missouri));
			AssertTrue(graph.RemoveEdge(missouri, tennessee));
			AssertEquals(new SumAndMax(1, 1889), graph.RemoveVertexAugmentation(montana));
			AssertTrue(graph.RemoveEdge(montana, northDakota));
			AssertTrue(graph.RemoveEdge(montana, southDakota));
			AssertTrue(graph.RemoveEdge(montana, wyoming));
			AssertEquals(new SumAndMax(3, 1867), graph.RemoveVertexAugmentation(nebraska));
			AssertTrue(graph.RemoveEdge(nebraska, southDakota));
			AssertTrue(graph.RemoveEdge(nebraska, wyoming));
			AssertEquals(new SumAndMax(4, 1864), graph.RemoveVertexAugmentation(nevada));
			AssertTrue(graph.RemoveEdge(nevada, oregon));
			AssertTrue(graph.RemoveEdge(nevada, utah));
			AssertEquals(new SumAndMax(2, 1788), graph.RemoveVertexAugmentation(newHampshire));
			AssertTrue(graph.RemoveEdge(newHampshire, vermont));
			AssertEquals(new SumAndMax(12, 1787), graph.RemoveVertexAugmentation(newJersey));
			AssertTrue(graph.RemoveEdge(newJersey, newYork));
			AssertTrue(graph.RemoveEdge(newJersey, pennsylvania));
			AssertEquals(new SumAndMax(3, 1912), graph.RemoveVertexAugmentation(newMexico));
			AssertTrue(graph.RemoveEdge(newMexico, oklahoma));
			AssertTrue(graph.RemoveEdge(newMexico, texas));
			AssertTrue(graph.RemoveEdge(newMexico, utah));
			AssertEquals(new SumAndMax(27, 1788), graph.RemoveVertexAugmentation(newYork));
			AssertTrue(graph.RemoveEdge(newYork, pennsylvania));
			AssertTrue(graph.RemoveEdge(newYork, vermont));
			AssertEquals(new SumAndMax(13, 1789), graph.RemoveVertexAugmentation(northCarolina));
			AssertTrue(graph.RemoveEdge(northCarolina, southCarolina));
			AssertTrue(graph.RemoveEdge(northCarolina, tennessee));
			AssertTrue(graph.RemoveEdge(northCarolina, virginia));
			AssertEquals(new SumAndMax(1, 1889), graph.RemoveVertexAugmentation(northDakota));
			AssertTrue(graph.RemoveEdge(northDakota, southDakota));
			AssertEquals(new SumAndMax(16, 1803), graph.RemoveVertexAugmentation(ohio));
			AssertTrue(graph.RemoveEdge(ohio, pennsylvania));
			AssertTrue(graph.RemoveEdge(ohio, westVirginia));
			AssertEquals(new SumAndMax(5, 1907), graph.RemoveVertexAugmentation(oklahoma));
			AssertTrue(graph.RemoveEdge(oklahoma, texas));
			AssertEquals(new SumAndMax(5, 1859), graph.RemoveVertexAugmentation(oregon));
			AssertTrue(graph.RemoveEdge(oregon, washington));
			AssertEquals(new SumAndMax(18, 1787), graph.RemoveVertexAugmentation(pennsylvania));
			AssertTrue(graph.RemoveEdge(pennsylvania, westVirginia));
			AssertEquals(new SumAndMax(2, 1790), graph.RemoveVertexAugmentation(rhodeIsland));
			AssertEquals(new SumAndMax(7, 1788), graph.RemoveVertexAugmentation(southCarolina));
			AssertEquals(new SumAndMax(1, 1889), graph.RemoveVertexAugmentation(southDakota));
			AssertTrue(graph.RemoveEdge(southDakota, wyoming));
			AssertEquals(new SumAndMax(9, 1796), graph.RemoveVertexAugmentation(tennessee));
			AssertTrue(graph.RemoveEdge(tennessee, virginia));
			AssertEquals(new SumAndMax(36, 1845), graph.RemoveVertexAugmentation(texas));
			AssertEquals(new SumAndMax(4, 1896), graph.RemoveVertexAugmentation(utah));
			AssertTrue(graph.RemoveEdge(utah, wyoming));
			AssertEquals(new SumAndMax(1, 1791), graph.RemoveVertexAugmentation(vermont));
			AssertEquals(new SumAndMax(11, 1788), graph.RemoveVertexAugmentation(virginia));
			AssertTrue(graph.RemoveEdge(virginia, westVirginia));
			AssertEquals(new SumAndMax(10, 1889), graph.RemoveVertexAugmentation(washington));
			AssertEquals(new SumAndMax(3, 1863), graph.RemoveVertexAugmentation(westVirginia));
			AssertEquals(new SumAndMax(8, 1848), graph.RemoveVertexAugmentation(wisconsin));
			AssertEquals(new SumAndMax(1, 1890), graph.RemoveVertexAugmentation(wyoming));

			AssertFalse(graph.Connected(georgia, newMexico));
			AssertFalse(graph.Connected(wisconsin, michigan));
			AssertFalse(graph.Connected(ohio, kentucky));
			AssertFalse(graph.Connected(alaska, connecticut));
			AssertNull(graph.GetComponentAugmentation(southDakota));
			AssertNull(graph.GetComponentAugmentation(arkansas));
		}

		/// <summary>
		/// Tests ConnectivityGraph on the graph for a dodecahedron. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDodecahedron()
		[Fact]
		public virtual void TestDodecahedron()
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

			AssertTrue(graph.AddEdge(vertex1, vertex2));
			AssertTrue(graph.AddEdge(vertex1, vertex5));
			AssertTrue(graph.AddEdge(vertex1, vertex6));
			AssertTrue(graph.AddEdge(vertex2, vertex3));
			AssertTrue(graph.AddEdge(vertex2, vertex8));
			AssertTrue(graph.AddEdge(vertex3, vertex4));
			AssertTrue(graph.AddEdge(vertex3, vertex10));
			AssertTrue(graph.AddEdge(vertex4, vertex5));
			AssertTrue(graph.AddEdge(vertex4, vertex12));
			AssertTrue(graph.AddEdge(vertex5, vertex14));
			AssertTrue(graph.AddEdge(vertex6, vertex7));
			AssertTrue(graph.AddEdge(vertex6, vertex15));
			AssertTrue(graph.AddEdge(vertex7, vertex8));
			AssertTrue(graph.AddEdge(vertex7, vertex16));
			AssertTrue(graph.AddEdge(vertex8, vertex9));
			AssertTrue(graph.AddEdge(vertex9, vertex10));
			AssertTrue(graph.AddEdge(vertex9, vertex17));
			AssertTrue(graph.AddEdge(vertex10, vertex11));
			AssertTrue(graph.AddEdge(vertex11, vertex12));
			AssertTrue(graph.AddEdge(vertex11, vertex18));
			AssertTrue(graph.AddEdge(vertex12, vertex13));
			AssertTrue(graph.AddEdge(vertex13, vertex14));
			AssertTrue(graph.AddEdge(vertex13, vertex19));
			AssertTrue(graph.AddEdge(vertex14, vertex15));
			AssertTrue(graph.AddEdge(vertex15, vertex20));
			AssertTrue(graph.AddEdge(vertex16, vertex17));
			AssertTrue(graph.AddEdge(vertex16, vertex20));
			AssertTrue(graph.AddEdge(vertex17, vertex18));
			AssertTrue(graph.AddEdge(vertex18, vertex19));
			AssertTrue(graph.AddEdge(vertex19, vertex20));
			graph.Optimize();

			AssertTrue(graph.Connected(vertex1, vertex17));
			AssertTrue(graph.Connected(vertex7, vertex15));

			AssertTrue(graph.RemoveEdge(vertex5, vertex14));
			AssertTrue(graph.RemoveEdge(vertex6, vertex15));
			AssertTrue(graph.RemoveEdge(vertex7, vertex16));
			AssertTrue(graph.RemoveEdge(vertex12, vertex13));
			AssertTrue(graph.RemoveEdge(vertex16, vertex17));
			AssertTrue(graph.Connected(vertex1, vertex14));
			AssertTrue(graph.Connected(vertex4, vertex20));
			AssertTrue(graph.Connected(vertex14, vertex16));

			AssertTrue(graph.RemoveEdge(vertex18, vertex19));
			AssertFalse(graph.Connected(vertex1, vertex14));
			AssertFalse(graph.Connected(vertex4, vertex20));
			AssertTrue(graph.Connected(vertex14, vertex16));

			graph.Clear();
			graph.Optimize();
			AssertTrue(graph.Connected(vertex7, vertex7));
			AssertFalse(graph.Connected(vertex1, vertex2));
		}

		/// <summary>
		/// Tests the zero-argument ConnVertex constructor. </summary>
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @Test public void testDefaultConnVertexConstructor()
		[Fact]
		public virtual void TestDefaultConnVertexConstructor()
		{
			ConnGraph graph = new ConnGraph();
			ConnVertex vertex1 = new ConnVertex();
			ConnVertex vertex2 = new ConnVertex();
			ConnVertex vertex3 = new ConnVertex();
			ConnVertex vertex4 = new ConnVertex();
			ConnVertex vertex5 = new ConnVertex();
			ConnVertex vertex6 = new ConnVertex();
			AssertTrue(graph.AddEdge(vertex1, vertex2));
			AssertTrue(graph.AddEdge(vertex2, vertex3));
			AssertTrue(graph.AddEdge(vertex1, vertex3));
			AssertTrue(graph.AddEdge(vertex4, vertex5));
			AssertTrue(graph.Connected(vertex1, vertex3));
			AssertTrue(graph.Connected(vertex4, vertex5));
			AssertFalse(graph.Connected(vertex1, vertex4));

			graph.Optimize();
			AssertTrue(graph.RemoveEdge(vertex1, vertex3));
			AssertTrue(graph.Connected(vertex1, vertex3));
			AssertTrue(graph.Connected(vertex4, vertex5));
			AssertFalse(graph.Connected(vertex1, vertex4));
			AssertTrue(graph.RemoveEdge(vertex1, vertex2));
			AssertFalse(graph.Connected(vertex1, vertex3));
			AssertTrue(graph.Connected(vertex4, vertex5));
			AssertFalse(graph.Connected(vertex1, vertex4));

			AssertEqualsUnordered(new [] {vertex3}, new HashSet<ConnVertex>(graph.AdjacentVertices(vertex2)));
			AssertTrue(graph.AdjacentVertices(vertex1).Count == 0);
			AssertTrue(graph.AdjacentVertices(vertex6).Count == 0);
		}
	}

}