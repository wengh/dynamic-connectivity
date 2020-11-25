using System.Threading;

namespace Connectivity.RedBlack
{
    internal interface IReference
    {
        object Value { get; }
    }

    /// <summary>
    /// Wraps a value using reference equality.  In other words, two references are equal only if their values are the same
    /// object instance, as in ==. </summary>
    internal class Reference<T> : IReference
    {
        /// <summary>
        /// The value this wraps. </summary>
        private readonly T value;

        public Reference(T value)
        {
            this.value = value;
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