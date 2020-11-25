using System;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;

namespace Connectivity.test
{
    public class ConnGraphTest2
    {
	    public struct Pii
	    {
		    public int a;
		    public int b;

		    public Pii(int a, int b)
		    {
			    this.a = a;
			    this.b = b;
		    }
	    }

	    private readonly ITestOutputHelper _log;

	    public ConnGraphTest2(ITestOutputHelper log)
	    {
		    _log = log;
	    }

		[Theory]
		// [InlineData(100000, 300000, 100000, 100000, 11439)]
		// [InlineData(1000, 3000, 1000, 1000, 159475)]
		// [InlineData(10000, 300000, 100000, 100000, 1184)]
		[InlineData(10000, 30000, 1000, 10000, 11439, true)]
		// [InlineData(100000, 300000, 100000, 100000, 0)]
		// [InlineData(1000, 3000, 1000, 1000, 0)]
		// [InlineData(10000, 300000, 100000, 100000, 0)]
		public void TestBenchmark(int nV, int nE, int nQ, int nO, int seed, bool naive = false)
		{
			if (seed == 0)
			{
				seed = new Random().Next();
			}

			var swA = new PerformanceMeasurement(10);
			var swD = new PerformanceMeasurement(10);
			var swQ = new PerformanceMeasurement(1);
			int hash = 0;

			int nD = nO / 2; // number of deletions
			int nA = nO - nD; // number of additions
			int maxE = nV * (nV - 1) / 2;

			var rand = new Random(seed);
			var rand2 = new Random(seed + 31);

			IConnGraph graph;

			swA.Start();
			if (naive)
				graph = new NaiveConnGraph(SumAndMax.AUGMENTATION);
			else
				graph = new ConnGraph(SumAndMax.AUGMENTATION);
			swA.Stop();

			var V = new ConnVertex[nV];
			for (int i = 0; i < nV; i++)
			{
				V[i] = new ConnVertex(rand2);
				graph.SetVertexAugmentation(V[i], new SumAndMax(1, 1));
			}

			var E = new HashSet<Pii>();
			var EList = new List<Pii>(nE);
			// for (int i = 1; i < nV; i++)
			// {
			// 	graph.AddEdge(V[i - 1], V[i]);
			// }
			for (int i = 0; i < nE; i++)
			{
				AddRandomEdge();
			}
			_log.WriteLine($"Init time:   {swA}\n");
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
			_log.WriteLine($"Add time:    {swA}");
			_log.WriteLine($"Delete time: {swD}");
			_log.WriteLine($"Query time:  {swQ}");
			_log.WriteLine("");
			_log.WriteLine($"hash: {hash}");

			void AddRandomEdge()
			{
				if (EList.Count == maxE)
					return;

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
						return;
					}
				}
			}

			void DeleteRandomEdge()
			{
				int count = EList.Count;
				if (count == 0)
					return;
				int i1 = rand.Next(0, count);
				var pii = EList[i1];
				E.Remove(pii);
				EList[i1] = EList[count - 1];
				EList.RemoveAt(count - 1);
				swD.Start();
				graph.RemoveEdge(V[pii.a], V[pii.b]);
				swD.Stop();
			}

			void QueryRandom()
			{
				int a1 = rand.Next(0, nV);

				swQ.Start();
				var result = (SumAndMax) graph.GetComponentAugmentation(V[a1]);
				swQ.Stop();

				hash = hash + result.sum;
			}

			void QueryRandom1()
			{
				int a1 = rand.Next(1, nV);
				int b1 = rand.Next(0, a1);
				swQ.Start();
				bool result = graph.IsConnected(V[a1], V[b1]);
				swQ.Stop();

				hash = hash * 31 + (result ? 402653189 : 786433);
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
    }
}