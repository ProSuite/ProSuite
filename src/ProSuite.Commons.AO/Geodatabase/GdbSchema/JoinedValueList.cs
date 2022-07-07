using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Value list that delegates to underlying actual features using the provided copy matrix.
	/// The values are only accessed when needed which improves performance.
	/// </summary>
	public class JoinedValueList : IValueList
	{
		private readonly List<IRow> _rows;
		private readonly List<IDictionary<int, int>> _copyMatrices;

		public JoinedValueList(int rowCapacity = 2)
		{
			_rows = new List<IRow>(rowCapacity);
			_copyMatrices = new List<IDictionary<int, int>>(rowCapacity);
		}

		public bool Readonly { get; set; }

		/// <summary>
		/// Add a row to the list with its associated copyMatrix.
		/// </summary>
		/// <param name="row">The source row from which the values shall be taken</param>
		/// <param name="copyMatrix">The copy matrix containing the {target-schema, source schema}
		/// key-value pairs of the associated field indexes. </param>
		public void AddRow(IRow row, IDictionary<int, int> copyMatrix)
		{
			_rows.Add(row);
			_copyMatrices.Add(copyMatrix);
		}

		public object GetValue(int index, bool ensureRcwRefCountIncrease = false)
		{
			if (TryGetSource(index, out IRow sourceRow, out int fieldIndex))
			{
				return sourceRow?.Value[fieldIndex] ?? DBNull.Value;
			}

			throw new ArgumentOutOfRangeException(
				$"Field index {index} not found in a copy matrix.");
		}

		public void SetValue(int index, object value)
		{
			if (TryGetSource(index, out IRow sourceRow, out int fieldIndex))
			{
				// Be careful not to update actual features, just ensure the value is equal
				if (Readonly)
				{
					// TODO: Check DBNull, reference types
					object sourceValue = sourceRow?.Value[fieldIndex] ?? DBNull.Value;

					if (! sourceValue.Equals(value))
					{
						throw new InvalidOperationException("Cannot update read-only row");
					}
				}
				else
				{
					sourceRow.Value[fieldIndex] = value;
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

		private bool TryGetSource(int targetIndex, out IRow sourceRow, out int fieldIndex)
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
