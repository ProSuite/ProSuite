using System;
using System.Collections.Generic;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Value list that delegates to underlying actual features using the provided copy matrix.
	/// The values are only accessed when needed which improves performance.
	/// </summary>
	public class JoinedValueList : IValueList
	{
		private readonly List<IReadOnlyRow> _rows;
		private readonly List<IDictionary<int, int>> _copyMatrices;

		public JoinedValueList(int rowCapacity = 2)
		{
			_rows = new List<IReadOnlyRow>(rowCapacity);
			_copyMatrices = new List<IDictionary<int, int>>(rowCapacity);
		}

		/// <summary>
		/// Add a row to the list with its associated copyMatrix.
		/// </summary>
		/// <param name="row">The source row from which the values shall be taken</param>
		/// <param name="copyMatrix">The copy matrix containing the {target-schema, source schema}
		/// key-value pairs of the associated field indexes. </param>
		public void AddRow(IReadOnlyRow row, IDictionary<int, int> copyMatrix)
		{
			_rows.Add(row);
			_copyMatrices.Add(copyMatrix);
		}

		public object GetValue(int index, bool ensureRcwRefCountIncrease = false)
		{
			if (TryGetSource(index, out IReadOnlyRow sourceRow, out int fieldIndex))
			{
				return sourceRow?.get_Value(fieldIndex) ?? DBNull.Value;
			}

			throw new ArgumentOutOfRangeException(
				$"Field index {index} not found in a copy matrix.");
		}

		public void SetValue(int index, object value)
		{
			if (TryGetSource(index, out IReadOnlyRow sourceRow, out int fieldIndex))
			{
				// No updating of actual features, just ensure the value is equal!

				// TODO: Check DBNull, reference types
				object sourceValue = sourceRow?.get_Value(fieldIndex) ?? DBNull.Value;

				if (! sourceValue.Equals(value))
				{
					throw new InvalidOperationException("Cannot update read-only row");
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

		private bool TryGetSource(int targetIndex, out IReadOnlyRow sourceRow, out int fieldIndex)
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
