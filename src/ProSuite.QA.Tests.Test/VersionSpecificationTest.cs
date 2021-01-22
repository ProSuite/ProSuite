using System;
using NUnit.Framework;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class VersionSpecificationTest
	{
		[Test]
		public void CanParseMajorOnly()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10");
			Assert.AreEqual("10.*.*", versionSpecification.VersionString);
		}

		[Test]
		public void CanParseMajorMinor()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10.2");
			Assert.AreEqual("10.2.*", versionSpecification.VersionString);
		}

		[Test]
		public void CanParseMajorMinorBugfix()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10.2.1");
			Assert.AreEqual("10.2.1", versionSpecification.VersionString);
		}

		[Test]
		public void CantParseMajorBugfix()
		{
			try
			{
				VersionSpecification.Create("10..1");
				Assert.Fail("Exception expected");
			}
			catch (ArgumentException e)
			{
				// ok
				Console.WriteLine(@"Expected exception: {0}", e.Message);
			}
		}

		[Test]
		public void CanDetermineLowerThanMajorMinorBugfix()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10.2.1");

			Assert.IsTrue(versionSpecification.IsLowerThan(11, 1, 0));
			Assert.IsTrue(versionSpecification.IsLowerThan(10, 3, 0));
			Assert.IsTrue(versionSpecification.IsLowerThan(10, 2, 2));

			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 1));

			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 0));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 1, 2));
			Assert.IsFalse(versionSpecification.IsLowerThan(9, 3, 2));
		}

		[Test]
		public void CanDetermineLowerThanMajorMinor()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10.2");

			Assert.IsTrue(versionSpecification.IsLowerThan(11, 1, 0));
			Assert.IsTrue(versionSpecification.IsLowerThan(10, 3, 0));

			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 2));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 1));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 0));

			Assert.IsFalse(versionSpecification.IsLowerThan(10, 1, 2));
			Assert.IsFalse(versionSpecification.IsLowerThan(9, 3, 2));
		}

		[Test]
		public void CanDetermineLowerThanMajor()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10");

			Assert.IsTrue(versionSpecification.IsLowerThan(11, 1, 0));

			Assert.IsFalse(versionSpecification.IsLowerThan(10, 3, 0));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 2));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 1));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 2, 0));
			Assert.IsFalse(versionSpecification.IsLowerThan(10, 1, 2));

			Assert.IsFalse(versionSpecification.IsLowerThan(9, 3, 2));
		}

		[Test]
		public void CanDetermineGreaterThanMajorMinorBugfix()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10.2.1");

			Assert.IsFalse(versionSpecification.IsGreaterThan(11, 1, 0));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 3, 0));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 2));

			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 1));

			Assert.IsTrue(versionSpecification.IsGreaterThan(10, 2, 0));
			Assert.IsTrue(versionSpecification.IsGreaterThan(10, 1, 2));
			Assert.IsTrue(versionSpecification.IsGreaterThan(9, 3, 2));
		}

		[Test]
		public void CanDetermineGreaterThanMajorMinor()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10.2");

			Assert.IsFalse(versionSpecification.IsGreaterThan(11, 1, 0));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 3, 0));

			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 2));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 1));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 0));

			Assert.IsTrue(versionSpecification.IsGreaterThan(10, 1, 2));
			Assert.IsTrue(versionSpecification.IsGreaterThan(9, 3, 2));
		}

		[Test]
		public void CanDetermineGreaterThanMajor()
		{
			VersionSpecification versionSpecification = VersionSpecification.Create("10");

			Assert.IsFalse(versionSpecification.IsGreaterThan(11, 1, 0));

			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 3, 0));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 2));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 1));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 2, 0));
			Assert.IsFalse(versionSpecification.IsGreaterThan(10, 1, 2));

			Assert.IsTrue(versionSpecification.IsGreaterThan(9, 3, 2));
		}
	}
}
