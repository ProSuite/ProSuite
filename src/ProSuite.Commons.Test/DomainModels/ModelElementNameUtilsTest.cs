using NUnit.Framework;
using ProSuite.Commons.DomainModels;

namespace ProSuite.Commons.Test.DomainModels
{
	[TestFixture]
	public class ModelElementNameUtilsTest
	{
		[Test]
		public void CanUnqualifyName()
		{
			string unqualified;
			Assert.IsTrue(ModelElementNameUtils.TryUnqualifyName("SOMEDB.DBO.SOMETABLE",
			                                                     out unqualified));
			Assert.AreEqual("SOMETABLE", unqualified);
		}

		[Test]
		public void CanUnqualifySingleCharDatasetName()
		{
			string unqualified;
			Assert.IsTrue(ModelElementNameUtils.TryUnqualifyName("SOMEDB.DBO.T",
			                                                     out unqualified));
			Assert.AreEqual("T", unqualified);
		}

		[Test]
		public void CantUnqualifyUnqualifiedName()
		{
			string unqualified;
			Assert.IsFalse(ModelElementNameUtils.TryUnqualifyName("SOMETABLE",
			                                                      out unqualified));
			Assert.AreEqual("SOMETABLE", unqualified);
		}

		[Test]
		public void CantUnqualifyNameWithTrailingSeparator()
		{
			string unqualified;
			Assert.IsFalse(ModelElementNameUtils.TryUnqualifyName("SOMETABLE.",
			                                                      out unqualified));
			Assert.AreEqual("SOMETABLE.", unqualified);

			// with trailing blank:
			Assert.IsFalse(ModelElementNameUtils.TryUnqualifyName("SOMETABLE. ",
			                                                      out unqualified));
			Assert.AreEqual("SOMETABLE. ", unqualified);
		}

		[Test]
		public void CanUnqualifyNameWithLeadingSeparator()
		{
			string unqualified;
			Assert.IsTrue(ModelElementNameUtils.TryUnqualifyName(".SOMETABLE",
			                                                     out unqualified));
			Assert.AreEqual("SOMETABLE", unqualified);
		}
	}
}