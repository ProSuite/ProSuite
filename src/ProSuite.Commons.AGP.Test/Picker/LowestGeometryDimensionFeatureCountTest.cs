using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;

namespace ProSuite.Commons.AGP.Test.Picker
{
	[TestFixture]
	public class LowestGeometryDimensionFeatureCountTest
	{
		[Test]
		public void EmptyInput_ReturnsZero()
		{
			Assert.AreEqual(0, Count());
		}

		[Test]
		public void SingleSelection_ReturnsItsCount()
		{
			Assert.AreEqual(3, Count(Sel(dimension: 0, count: 3)));
		}

		[Test]
		public void OnlyLowestDimensionCounts()
		{
			// Points (dim 0) present, so lines (dim 1) are ignored.
			Assert.AreEqual(2, Count(Sel(dimension: 0, count: 2),
			                         Sel(dimension: 1, count: 5)));
		}

		[Test]
		public void InputOrderDoesNotMatter()
		{
			// Highest dimension listed first; the method orders internally.
			Assert.AreEqual(2, Count(Sel(dimension: 2, count: 9),
			                         Sel(dimension: 0, count: 2)));
		}

		[Test]
		public void EmptyLowestDimension_IsSkipped()
		{
			// No point features (dim 0, count 0) => fall through to the lines.
			Assert.AreEqual(4, Count(Sel(dimension: 0, count: 0),
			                         Sel(dimension: 1, count: 4)));
		}

		[Test]
		public void MultipleSelectionsAtLowestDimension_AreSummed()
		{
			Assert.AreEqual(5, Count(Sel(dimension: 1, count: 2),
			                         Sel(dimension: 1, count: 3),
			                         Sel(dimension: 2, count: 10)));
		}

		[Test]
		public void AllEmpty_ReturnsZero()
		{
			Assert.AreEqual(0, Count(Sel(dimension: 0, count: 0),
			                         Sel(dimension: 1, count: 0)));
		}

		private static int Count(params IFeatureSelection[] selections)
		{
			return PickerModeUtils.GetLowestGeometryDimensionFeatureCount(selections);
		}

		private static IFeatureSelection Sel(int dimension, int count)
		{
			return new FakeFeatureSelection(dimension, count);
		}

		private sealed class FakeFeatureSelection : IFeatureSelection
		{
			private readonly int _count;

			public FakeFeatureSelection(int shapeDimension, int count)
			{
				ShapeDimension = shapeDimension;
				_count = count;
			}

			public int ShapeDimension { get; }

			public int GetCount()
			{
				return _count;
			}

			public IEnumerable<long> GetOids()
			{
				return Enumerable.Range(0, _count).Select(i => (long) i);
			}
		}
	}
}
