using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class InvolvedTest
	{
		[Test]
		public void CanTestEquality()
		{
			Involved i0 = new InvolvedRow("table", 1);
			Involved i1 = new InvolvedRow("table", 1);
			Involved i2 = new InvolvedRow("table", 2);

			Assert.AreEqual(i0, i1);
			Assert.AreNotEqual(i0, i2);

			InvolvedNested n0 = new InvolvedNested("der0", new List<Involved> { i0, i2 });
			InvolvedNested n1 = new InvolvedNested("der0", new List<Involved> { i2, i0 });
			InvolvedNested n2 = new InvolvedNested("der0", new List<Involved> { i1, i2 });

			Assert.AreEqual(n0, n1);
			Assert.AreEqual(n0, n2);
			Assert.IsTrue(Equals(n0, n1));
			Assert.IsTrue(Equals(n0, n2));

			Assert.IsTrue(n0.BaseRows.Contains(i1));

			InvolvedNested n3 = new InvolvedNested("der1", new List<Involved> { i0, i2 });
			Assert.AreNotEqual(n0, n3);
			InvolvedNested n4 = new InvolvedNested("der0", new List<Involved> { i0, i1 });
			Assert.AreNotEqual(n0, n4);
			Assert.AreNotEqual(n4, n0);
		}
	}
}
