using System;
using NUnit.Framework;
using ProSuite.Commons.Collections;

namespace ProSuite.Commons.Test.Collections
{
	[TestFixture]
	public class SimpleSetTest
	{
		[SetUp]
		public void SetUp()
		{
			// Invoked before each test
		}

		[TearDown]
		public void TearDown()
		{
			// invoked after each test
		}

		[Test]
		public void AddingIntsTest()
		{
			var set = new SimpleSet<int> {1, 2, 3};

			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.Contains(1));
			Assert.IsTrue(set.Contains(2));
			Assert.IsTrue(set.Contains(3));
			Assert.IsFalse(set.Contains(4));
		}

		[Test]
		public void AddingContainedElementThrowsException()
		{
			var set = new SimpleSet<object>();

			try
			{
				var member = new TestMember("hello");
				set.Add(member);
				set.Add(member); // throw
			}
			catch (Exception ex)
			{
				Console.WriteLine(
					@"Adding duplicate throwed exception: {0}", ex.Message);

				return; // ok
			}

			Assert.Fail("No exception when adding duplicate");
		}

		[Test]
		public void NullNotAllowedInSet()
		{
			var set = new SimpleSet<object>();

			try
			{
				set.Add("dont");
				set.Add("like");
				set.Add(null);
			}
			catch (ArgumentNullException)
			{
				Assert.AreEqual(2, set.Count);
				return; // ok
			}

			Assert.Fail("No ArgumentNullException on Add(null)");
		}

		[Test]
		public void CannotTestForNull()
		{
			var set = new SimpleSet<object>();

			try
			{
				set.Contains(null);
			}
			catch (ArgumentNullException)
			{
				return; // ok
			}

			Assert.Fail("No ArgumentNullException on Contains(null)");
		}

		[Test]
		public void ReferenceEqualityTest()
		{
			var set = new SimpleSet<TestMember>();

			var theObject = new TestMember("hello");
			var equalObject = new TestMember("HELLO");

			Assert.IsTrue(theObject.Equals(equalObject));
			Assert.IsFalse(theObject == equalObject);

			set.Add(theObject);

			TestMember containedObject;
			bool isContained = set.Contains(equalObject, out containedObject);

			Assert.IsTrue(isContained);
			Assert.IsTrue(containedObject.Equals(equalObject));
			Assert.IsTrue(containedObject == theObject);
			Assert.IsTrue(containedObject != equalObject);

			bool isChanged = set.TryAdd(equalObject, out containedObject);
			Assert.IsFalse(isChanged);
			Assert.IsTrue(containedObject.Equals(equalObject));
			Assert.IsTrue(containedObject == theObject);
			Assert.IsTrue(containedObject != equalObject);

			TestMember removedObject;
			bool wasContained = set.Remove(equalObject, out removedObject);

			Assert.IsTrue(wasContained);
			Assert.IsTrue(removedObject.Equals(theObject));
			Assert.IsTrue(removedObject == theObject);
			Assert.IsTrue(removedObject != equalObject);
		}

		private class TestMember : IEquatable<TestMember>
		{
			private readonly string _string;

			public TestMember(string s)
			{
				_string = s;
			}

			public bool Equals(TestMember other)
			{
				if (other == null)
				{
					return false;
				}

				return _string.Equals(other._string, StringComparison.OrdinalIgnoreCase);
			}

			public override bool Equals(object obj)
			{
				if (! (obj is TestMember))
				{
					return false;
				}

				return Equals(obj as TestMember);
			}

			public override int GetHashCode()
			{
				return _string.ToLower().GetHashCode();
			}
		}
	}
}