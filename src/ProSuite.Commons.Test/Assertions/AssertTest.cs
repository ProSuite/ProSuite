using NUnit.Framework;
using Assert = ProSuite.Commons.Essentials.Assertions.Assert;
using AssertionException = ProSuite.Commons.Essentials.Assertions.AssertionException;

namespace ProSuite.Commons.Test.Assertions
{
	[TestFixture]
	public class AssertTest
	{
		[Test]
		public void CanAssertNotNull()
		{
			Assert.NotNull("not null");
		}

		[Test]
		public void CanThrowNotNull()
		{
			const object dummy = null;
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.NotNull(dummy));
		}

		[Test]
		public void CanThrowNotNullForNullable()
		{
			int? dummy = null;
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.NotNull(dummy));
		}

		[Test]
		public void CanAssertNotNullMsg()
		{
			Assert.NotNull("not null", "{0} {1}", "arg1", "arg");
		}

		[Test]
		public void CanThrowNotNullMsg()
		{
			const object dummy = null;
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.NotNull(dummy, "{0} {1}", "arg1", "arg"));
		}

		[Test]
		public void CanAssertAreEqual()
		{
			Assert.AreEqual(111, 111, "equal");
		}

		[Test]
		public void CanThrowAreEqual()
		{
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.AreEqual(111, 112, "equal"));
		}

		[Test]
		public void CanAssertAssignable()
		{
			Assert.IsAssignable(new SubClass(), typeof(SuperClass));
		}

		[Test]
		public void CantAssertNotAssignable1()
		{
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.IsAssignable(new OtherClass(), typeof(SuperClass)));
		}

		[Test]
		public void CantAssertNotAssignable2()
		{
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.IsAssignable(new SuperClass(), typeof(SubClass)));
		}

		[Test]
		public void CanAssertAssignableFrom()
		{
			Assert.IsAssignableFrom(typeof(SuperClass), typeof(SubClass));
		}

		[Test]
		public void CantAssertNotAssignableFrom1()
		{
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.IsAssignableFrom(typeof(SuperClass), typeof(OtherClass)));
		}

		[Test]
		public void CantAssertNotAssignableFrom2()
		{
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.IsAssignableFrom(typeof(SubClass), typeof(SuperClass)));
		}

		[Test]
		public void CanAssertIs()
		{
			Assert.IsExactType(new SuperClass(), typeof(SuperClass));
		}

		[Test]
		public void CantAssertNotEqualType()
		{
			NUnit.Framework.Assert.Throws<AssertionException>(
				() => Assert.IsExactType(new SuperClass(), typeof(SubClass)));
		}
	}

	internal class SuperClass { }

	internal class SubClass : SuperClass { }

	internal class OtherClass { }
}