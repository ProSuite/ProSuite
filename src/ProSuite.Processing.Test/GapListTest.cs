using System.Linq;
using NUnit.Framework;
using ProSuite.Processing.Utils;

namespace ProSuite.Processing.Test
{
	[TestFixture]
	public class GapListTest
	{
		[Test]
		public void CanAddGap()
		{
			var gaps = new GapList();

			gaps.AddGap(1, 2);
			gaps.AddGap(3, 4);
			gaps.AddGap(6, 8);
			Assert.AreEqual(3, gaps.Count);
			gaps.AddGap(7, 9); // should merge with [6,8]
			Assert.AreEqual(3, gaps.Count);
			gaps.AddGap(3, 5); // existing start, should merge with [3,4]
			Assert.AreEqual(3, gaps.Count);
			gaps.AddGap(0, 3); // should merge with [1,2] and [3,5]
			Assert.AreEqual(2, gaps.Count); // [0,5] and [6,9]
			gaps.AddGap(5, 6);
			var gap = gaps.Gaps.Single();
			Assert.AreEqual(0, gap.Min);
			Assert.AreEqual(9, gap.Max);

			gaps = new GapList();
			gaps.AddGap(4, 5);
			gaps.AddGap(3, 6);
			gaps.AddGap(2, 7);
			gaps.AddGap(1, 8);
			gaps.AddGap(0, 9);
			gap = gaps.Gaps.Single();
			Assert.AreEqual(0, gap.Min);
			Assert.AreEqual(9, gap.Max);

			gaps = new GapList();
			gaps.AddGap(1, 2);
			gaps.AddGap(3, 4);
			gaps.AddGap(5, 6);
			gaps.AddGap(7, 8);
			Assert.AreEqual(4, gaps.Gaps.Count());
			gaps.AddGap(0, 9);
			Assert.AreEqual(1, gaps.Gaps.Count());
		}

		[Test]
		public void CanDropShortGaps()
		{
			var gaps = new GapList();
			gaps.AddGap(1, 2);
			gaps.AddGap(2.5, 2.6);
			gaps.AddGap(2.7, 2.75);
			gaps.AddGap(3, 4);
			Assert.AreEqual(4, gaps.Count);
			Assert.AreEqual(1, gaps.DropShortGaps(0.099999));
			Assert.AreEqual(3, gaps.Count);
			Assert.AreEqual(1, gaps.DropShortGaps(0.100001));
			Assert.AreEqual(2, gaps.Count);
		}

		[Test]
		public void CanMergeNearGaps()
		{
			var gaps = new GapList();
			gaps.AddGap(1, 2); // 0.1
			gaps.AddGap(2.1, 3); // 0.09
			gaps.AddGap(3.09, 4); // 0.001
			gaps.AddGap(4.001, 5);
			Assert.AreEqual(4, gaps.Count);
			Assert.AreEqual(1, gaps.MergeNearGaps(0.089999));
			Assert.AreEqual(3, gaps.Count);
			Assert.AreEqual(1, gaps.MergeNearGaps(0.090001));
			Assert.AreEqual(2, gaps.Count);
			Assert.AreEqual(0, gaps.MergeNearGaps(0.099999));
			Assert.AreEqual(2, gaps.Count);
			Assert.AreEqual(1, gaps.MergeNearGaps(0.100001));
			Assert.AreEqual(1, gaps.Count);
			Assert.AreEqual(1.0, gaps.Gaps.Single().Min);
			Assert.AreEqual(5.0, gaps.Gaps.Single().Max);
		}
	}
}
