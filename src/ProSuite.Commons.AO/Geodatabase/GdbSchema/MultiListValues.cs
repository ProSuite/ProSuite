using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Value list that delegates to underlying actual features using the provided copy matrix.
	/// The values are only accessed when needed which improves performance.
	/// </summary>
	public class MultiListValues : IValueList
	{
		private readonly List<IValueList> _rows;
		private readonly List<IDictionary<int, int>> _copyMatrices;

		public MultiListValues(int rowCapacity = 2)
		{
			_rows = new List<IValueList>(rowCapacity);
			_copyMatrices = new List<IDictionary<int, int>>(rowCapacity);
		}

		/// <summary>
		/// Whether or not a missing field mapping is allowed and does not result in an exception
		/// but a null-value being returned instead.
		/// </summary>
		public bool AllowMissingFieldMapping { get; set; }

		/// <summary>
		/// Add a row with its associated copyMatrix.
		/// </summary>
		/// <param name="row">The source row from which the values shall be taken</param>
		/// <param name="copyMatrix">The copy matrix containing the {target-schema, source schema}
		/// key-value pairs of the associated field indexes. </param>
		public void AddRow(IReadOnlyRow row, IDictionary<int, int> copyMatrix)
		{
			_rows.Add(new ReadOnlyRowBasedValues(row));
			_copyMatrices.Add(copyMatrix);
		}

		/// <summary>
		/// Add a list its associated copyMatrix.
		/// </summary>
		/// <param name="list">The source list from which the values shall be taken</param>
		/// <param name="copyMatrix">The copy matrix containing the {target-index, source-index}
		/// key-value pairs of the associated list indexes. </param>
		public void AddList(IValueList list, IDictionary<int, int> copyMatrix)
		{
			_rows.Add(list);
			_copyMatrices.Add(copyMatrix);
		}

		public object GetValue(int index, bool increaseRcwRefCount = false)
		{
			if (TryGetSource(index, out IValueList sourceRow, out int fieldIndex))
			{
				return sourceRow?.GetValue(fieldIndex) ?? DBNull.Value;
			}

			if (AllowMissingFieldMapping)
			{
				return DBNull.Value;
			}

			throw new ArgumentOutOfRangeException(
				$"Field index {index} not found in a copy matrix.");
		}

		public void SetValue(int index, object value)
		{
			if (TryGetSource(index, out IValueList sourceRow, out int fieldIndex))
			{
				// No updating of actual features, just ensure the value is equal!

				// TODO: Check DBNull, reference types
				object sourceValue = sourceRow?.GetValue(fieldIndex) ?? DBNull.Value;

				if (! sourceValue.Equals(value))
				{
					// TODO: Add updateabiliy to the interface
					throw new InvalidOperationException("Cannot update read-only list");
				}
			}
			else
			{
				throw new ArgumentOutOfRangeException(
					$"Field index {index} not found in a copy matrix.");
			}
		}

		public bool HasValue(int index)
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				if (_copyMatrices[i].ContainsKey(index))
				{
					return true;
				}
			}

			return false;
		}

		private bool TryGetSource(int targetIndex, out IValueList sourceRow, out int fieldIndex)
		{
			for (int i = 0; i < _rows.Count; i++)
			{
				if (_copyMatrices[i].TryGetValue(targetIndex, out int sourceIndex))
				{
					sourceRow = _rows[i];
					fieldIndex = sourceIndex;

					return true;
				}
			}

			sourceRow = null;
			fieldIndex = -1;

			return false;
		}
	}
}
