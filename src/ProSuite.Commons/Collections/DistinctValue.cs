using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Collections
{
	public class DistinctValue<T>
	{
		public DistinctValue([NotNull] T value, int count)
		{
			Assert.ArgumentNotNull(value, nameof(value));

			Value = value;
			Count = count;
		}

		[NotNull]
		public T Value { get; }

		public int Count { get; set; }
	}
}
