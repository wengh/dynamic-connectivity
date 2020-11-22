using System.Threading;

namespace com.github.btrekkie.red_black_node
{
	internal interface IReference
	{
		object Value { get; }
	}

	/// <summary>
	/// Wraps a value using reference equality.  In other words, two references are equal only if their values are the same
	/// object instance, as in ==. </summary>
	/// @param <T>The type of value. </param>
	internal class Reference<T> : IReference
	{
		private static int _i = 0;

		/// <summary>
		/// The value this wraps. </summary>
		private readonly T value;

		private readonly int id;

		public Reference(T value)
		{
			this.value = value;
			id = Interlocked.Increment(ref _i);
		}

		public override bool Equals(object obj)
		{
			if (obj is IReference other)
			{
				return ReferenceEquals(value, other.Value);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return value.GetHashCode();
		}

		public object Value => value;
	}

}