using System;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;

namespace ProSuite.Commons.AGP.Core.Test
{
	[TestFixture]
	public class DatasetNameUtilsTest
	{
		[Test]
		public void CanHasDatasetPrefix()
		{
			Assert.IsFalse(DatasetNameUtils.HasDatasetPrefix("ANY", null));
			Assert.IsTrue(DatasetNameUtils.HasDatasetPrefix("ANY", string.Empty));

			Assert.IsFalse(DatasetNameUtils.HasDatasetPrefix("FOO", "PRE"));
			Assert.IsTrue(DatasetNameUtils.HasDatasetPrefix("PRE_FOO", "PRE"));
			Assert.IsTrue(DatasetNameUtils.HasDatasetPrefix(" PRE_FOO ", "PRE"));

			Assert.IsFalse(DatasetNameUtils.HasDatasetPrefix("QFR.FOO", "PRE"));
			Assert.IsTrue(DatasetNameUtils.HasDatasetPrefix("QFR.PRE_FOO", "PRE"));
			Assert.IsTrue(DatasetNameUtils.HasDatasetPrefix(" QFR . PRE_FOO ", "PRE"));
		}

		[Test]
		public void CanChangeDatasetPrefix()
		{
			// special case: null remains null:
			Assert.IsNull(DatasetNameUtils.ChangeDatasetPrefix(null, "FOO", "BAR"));

			// fromPrefix must match, otherwise an error occurs:
			Assert.Catch<InvalidOperationException>(() => DatasetNameUtils.ChangeDatasetPrefix(string.Empty, "FOO", "BAR"));
			Assert.Catch<InvalidOperationException>(() => DatasetNameUtils.ChangeDatasetPrefix("NAME", "FOO", "BAR"));
			Assert.AreEqual("BARNAME", DatasetNameUtils.ChangeDatasetPrefix("FOONAME", "FOO", "BAR"));
			Assert.AreEqual("BARNAME", DatasetNameUtils.ChangeDatasetPrefix(" FOONAME ", "FOO", "BAR"));

			// and same for qualified names:
			Assert.Catch<InvalidOperationException>(() => DatasetNameUtils.ChangeDatasetPrefix("QUUX.NAME", "FOO", "BAR"));
			Assert.AreEqual("QUUX.BARNAME", DatasetNameUtils.ChangeDatasetPrefix("QUUX.FOONAME", "FOO", "BAR"));
			Assert.Catch<InvalidOperationException>(() => DatasetNameUtils.ChangeDatasetPrefix(" QUUX . NAME ", "FOO", "BAR"));
			Assert.AreEqual("QUUX.BARNAME", DatasetNameUtils.ChangeDatasetPrefix(" QUUX . FOONAME ", "FOO", "BAR"));

			// the empty string is a valid prefix:
			Assert.AreEqual("PRE_NAME", DatasetNameUtils.ChangeDatasetPrefix("NAME", "", "PRE_"));
			Assert.AreEqual("NAME", DatasetNameUtils.ChangeDatasetPrefix("PRE_NAME", "PRE_", ""));

			// In K2 the prefix ends in an underscore, but that's nothing special here:
			Assert.Catch<InvalidOperationException>(() => DatasetNameUtils.ChangeDatasetPrefix("NAME", "FOO_", "BAR_"));
			Assert.AreEqual("BAR_NAME", DatasetNameUtils.ChangeDatasetPrefix("FOO_NAME", "FOO_", "BAR_"));
			Assert.Catch<InvalidOperationException>(() => DatasetNameUtils.ChangeDatasetPrefix("QUUX.NAME", "FOO_", "BAR_"));
			Assert.AreEqual("QUUX.BAR_NAME", DatasetNameUtils.ChangeDatasetPrefix("QUUX.FOO_NAME", "FOO_", "BAR_"));
		}

		[Test]
		public void CanParseDatasetName()
		{
			DatasetNameUtils.ParseDatasetName(null, out string tableName, out string qualifier);
			Assert.IsNull(tableName);
			Assert.IsNull(qualifier);

			DatasetNameUtils.ParseDatasetName("", out tableName, out qualifier);
			Assert.IsEmpty(tableName);
			Assert.IsNull(qualifier);

			DatasetNameUtils.ParseDatasetName("FOO_BAR", out tableName, out qualifier);
			Assert.AreEqual("FOO_BAR", tableName);
			Assert.IsNull(qualifier);

			DatasetNameUtils.ParseDatasetName(" FOO_BAR ", out tableName, out qualifier);
			Assert.AreEqual("FOO_BAR", tableName);
			Assert.IsNull(qualifier);

			DatasetNameUtils.ParseDatasetName(".FOO_BAR", out tableName, out qualifier);
			Assert.AreEqual("FOO_BAR", tableName);
			Assert.IsNull(qualifier);

			DatasetNameUtils.ParseDatasetName("FOO_BAR.", out tableName, out qualifier);
			Assert.IsEmpty(tableName);
			Assert.AreEqual("FOO_BAR", qualifier);

			DatasetNameUtils.ParseDatasetName("PRE.FOO_BAR", out tableName, out qualifier);
			Assert.AreEqual("FOO_BAR", tableName);
			Assert.AreEqual("PRE", qualifier);

			DatasetNameUtils.ParseDatasetName("PRE.FOO.BAR", out tableName, out qualifier);
			Assert.AreEqual("BAR", tableName);
			Assert.AreEqual("PRE.FOO", qualifier);

			DatasetNameUtils.ParseDatasetName(" PRE . FOO_BAR ", out tableName, out qualifier);
			Assert.AreEqual("FOO_BAR", tableName);
			Assert.AreEqual("PRE", qualifier);
		}

		[Test]
		public void CanQualifyDatasetName()
		{
			Assert.IsNull(DatasetNameUtils.QualifyDatasetName(null));
			Assert.IsNull(DatasetNameUtils.QualifyDatasetName(null, "qualifier"));

			Assert.IsEmpty(DatasetNameUtils.QualifyDatasetName(string.Empty));
			Assert.IsEmpty(DatasetNameUtils.QualifyDatasetName(string.Empty, "qualifier"));

			Assert.AreEqual("FOO", DatasetNameUtils.QualifyDatasetName("FOO"));
			Assert.AreEqual("FOO", DatasetNameUtils.QualifyDatasetName("FOO", string.Empty));
			Assert.AreEqual("BAR.FOO", DatasetNameUtils.QualifyDatasetName("FOO", "BAR"));

			Assert.AreEqual("FOO", DatasetNameUtils.QualifyDatasetName("QUUX.FOO"));
			Assert.AreEqual("FOO", DatasetNameUtils.QualifyDatasetName("QUUX.FOO", string.Empty));
			Assert.AreEqual("BAR.FOO", DatasetNameUtils.QualifyDatasetName("QUUX.FOO", "BAR"));

			Assert.AreEqual("FOO", DatasetNameUtils.QualifyDatasetName(" QUUX . FOO "));
			Assert.AreEqual("BAR.FOO", DatasetNameUtils.QualifyDatasetName(" QUUX . FOO ", " BAR "));

			// The qualifier argument may end in the separator:
			Assert.AreEqual("PRE.NAME", DatasetNameUtils.QualifyDatasetName("NAME", "PRE."));
			Assert.AreEqual("PRE.NAME", DatasetNameUtils.QualifyDatasetName("ANY.NAME", "PRE."));
			Assert.AreEqual("PRE.NAME", DatasetNameUtils.QualifyDatasetName(" NAME ", " PRE . "));
		}

		[Test]
		public void CanUnqualifyDatasetName()
		{
			Assert.IsNull(DatasetNameUtils.UnqualifyDatasetName(null));
			Assert.IsEmpty(DatasetNameUtils.UnqualifyDatasetName(string.Empty));
			Assert.AreEqual("NAME", DatasetNameUtils.UnqualifyDatasetName("NAME"));
			Assert.AreEqual("NAME", DatasetNameUtils.UnqualifyDatasetName("PRE.NAME"));
			Assert.AreEqual("NAME", DatasetNameUtils.UnqualifyDatasetName(" NAME "));
			Assert.AreEqual("NAME", DatasetNameUtils.UnqualifyDatasetName(" PRE . NAME "));
		}
	}
}
