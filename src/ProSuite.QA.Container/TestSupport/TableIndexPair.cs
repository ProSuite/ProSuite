using System;

namespace ProSuite.QA.Container.TestSupport
{
	internal class TableIndexPair : IEquatable<TableIndexPair>
	{
		private readonly int _tableIndex1;
		private readonly int _tableIndex2;

		/// <summary>
		/// Initializes a new instance of the <see cref="TableIndexPair"/> class.
		/// </summary>
		/// <param name="tableIndex1">The table index 1.</param>
		/// <param name="tableIndex2">The table index 2.</param>
		public TableIndexPair(int tableIndex1, int tableIndex2)
		{
			_tableIndex1 = tableIndex1;
			_tableIndex2 = tableIndex2;
		}

		#region IEquatable<TableIndexPair> Members

		public bool Equals(TableIndexPair other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other._tableIndex1 == _tableIndex1 && other._tableIndex2 == _tableIndex2;
		}

		#endregion

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

			if (obj.GetType() != typeof(TableIndexPair))
			{
				return false;
			}

			return Equals((TableIndexPair) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (_tableIndex1 * 397) ^ _tableIndex2;
			}
		}
	}
}
