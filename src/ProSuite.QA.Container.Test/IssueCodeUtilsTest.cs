using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class IssueCodeUtilsTest
	{
		[Test]
		public void CanGetIssueCodesFromField()
		{
			IList<IssueCode> codes =
				IssueCodeUtils.GetIssueCodes(typeof(TestWithIssueCodesInField));

			Assert.AreEqual(2, codes.Count);

			Assert.AreEqual("TESTFIELD.CODEA", codes[0].ID);
			Assert.AreEqual("TESTFIELD.CODEB", codes[1].ID);

			Assert.AreEqual("code A", codes[0].Description);
			Assert.AreEqual("code B", codes[1].Description);
		}

		[Test]
		public void CanGetIssueCodesFromProperty()
		{
			IList<IssueCode> codes =
				IssueCodeUtils.GetIssueCodes(typeof(TestWithIssueCodesInProperty));

			Assert.AreEqual(2, codes.Count);

			Assert.AreEqual("TESTPROP.CODE1", codes[0].ID);
			Assert.AreEqual("TESTPROP.CODE2", codes[1].ID);

			Assert.AreEqual("code 1", codes[0].Description);
			Assert.AreEqual("code 2", codes[1].Description);
		}

		[Test]
		public void CanGetIssueCodeByID()
		{
			IssueCode issueCode = IssueCodeUtils.GetIssueCode(
				"testprop.code1", typeof(TestWithIssueCodesInProperty));

			Assert.IsNotNull(issueCode);
			Assert.AreEqual("TESTPROP.CODE1", issueCode.ID);
			Assert.AreEqual("code 1", issueCode.Description);
		}

		[Test]
		public void CanGetIssueCodeByLocalCode()
		{
			IssueCode issueCode = IssueCodeUtils.GetIssueCode(
				"code1", typeof(TestWithIssueCodesInProperty));

			Assert.IsNotNull(issueCode);
			Assert.AreEqual("TESTPROP.CODE1", issueCode.ID);
			Assert.AreEqual("code 1", issueCode.Description);
		}

		private class TestWithIssueCodesInProperty : ContainerTest
		{
			private static MyIssueCodes _issueCodes;

			[UsedImplicitly]
			public static TestIssueCodes IssueCodes
			{
				get { return _issueCodes ?? (_issueCodes = new MyIssueCodes()); }
			}

			public TestWithIssueCodesInProperty(IReadOnlyTable table) : base(table) { }

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				return 0;
			}

			private class MyIssueCodes : TestIssueCodes
			{
				[UsedImplicitly] public const string Code1 = "CODE1";

				[UsedImplicitly] public const string Code2 = "CODE2";

				public MyIssueCodes() : base("TESTPROP", IssueCodeDescriptions.ResourceManager,
				                             true) { }
			}
		}

		private class TestWithIssueCodesInField : ContainerTest
		{
			[UsedImplicitly] private static MyIssueCodes _issueCodes = new MyIssueCodes();

			public TestWithIssueCodesInField(IReadOnlyTable table) : base(table) { }

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				return 0;
			}

			private class MyIssueCodes : TestIssueCodes
			{
				[UsedImplicitly] public const string CodeA = "CODEA";

				[UsedImplicitly] public const string CodeB = "CODEB";

				public MyIssueCodes() : base("TESTFIELD", IssueCodeDescriptions.ResourceManager,
				                             true) { }
			}
		}
	}
}
