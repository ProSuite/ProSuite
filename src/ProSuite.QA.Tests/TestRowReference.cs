using System;

namespace ProSuite.QA.Tests
{
	public class TestRowReference : IEquatable<TestRowReference>
	{
		public TestRowReference(int objectId, int tableIndex)
		{
			ObjectId = objectId;
			TableIndex = tableIndex;
		}

		public int ObjectId { get; private set; }

		public int TableIndex { get; private set; }

		public override string ToString()
		{
			return string.Format("ObjectId: {0}, TableIndex: {1}", ObjectId, TableIndex);
		}

		public bool Equals(TestRowReference other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other.ObjectId == ObjectId && other.TableIndex == TableIndex;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(TestRowReference))
			{
				return false;
			}

			return Equals((TestRowReference) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (ObjectId * 397) ^ TableIndex;
			}
		}
	}
}
