using System;

namespace Connectivity.test
{
	/// <summary>
	/// Stores two values: a sum and a maximum. Used for testing augmentation in ConnGraph. </summary>
	internal class SumAndMax
	{
		/// <summary>
		/// An Augmentation that combines two SumAndMaxes into one. </summary>
		public static readonly IAugmentation AUGMENTATION = new AugmentationAnonymousInnerClass();

		private class AugmentationAnonymousInnerClass : IAugmentation
		{
			public object Combine(object value1, object value2)
			{
				SumAndMax sumAndMax1 = (SumAndMax)value1;
				SumAndMax sumAndMax2 = (SumAndMax)value2;
				return new SumAndMax(sumAndMax1.sum + sumAndMax2.sum, Math.Max(sumAndMax1.max, sumAndMax2.max));
			}
		}

		public readonly int sum;

		public readonly int max;

		public SumAndMax(int sum, int max)
		{
			this.sum = sum;
			this.max = max;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is SumAndMax))
			{
				return false;
			}
			SumAndMax sumAndMax = (SumAndMax)obj;
			return sum == sumAndMax.sum && max == sumAndMax.max;
		}

		public override int GetHashCode()
		{
			return 31 * sum + max;
		}
	}

}