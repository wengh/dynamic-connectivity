using System;
using System.Threading;

namespace Connectivity
{

	/// <summary>
	/// A vertex in a ConnGraph. See the comments for ConnGraph. </summary>
	public class ConnVertex
	{
		/// <summary>
		/// The thread-local random number generator we use by default to set the "hash" field. </summary>
		private static readonly ThreadLocal<Random> random = new ThreadLocal<Random>(() => new Random());

		/// <summary>
		/// A randomly generated integer to use as the return value of hashCode(). ConnGraph relies on random hash codes for
		/// its performance guarantees.
		/// </summary>
		private readonly int hash;

		public ConnVertex()
		{
			hash = random.Value.Next(int.MinValue, int.MaxValue);
		}

		/// <summary>
		/// Constructs a new ConnVertex. </summary>
		/// <param name="random"> The random number generator to use to produce a random hash code. ConnGraph relies on random hash
		///     codes for its performance guarantees. </param>
		public ConnVertex(Random random)
		{
			hash = random.Next();
		}

		public override int GetHashCode()
		{
			return hash;
		}
	}

}