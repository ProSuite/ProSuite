using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Gdb;

namespace ProSuite.Processing.Evaluation
{
	[DebuggerDisplay("FeatureCount = {FeatureCount}, EntryCount = {EntryCount}")]
	public class CommonValues : INamedValues
	{
		private readonly IDictionary<string, object> _commonValues;

		private CommonValues()
		{
			FeatureCount = 0;
			_commonValues = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
		}

		public CommonValues([NotNull] IRowValues row) : this()
		{
			if (row is null)
				throw new ArgumentNullException(nameof(row));

			Add(row);
		}

		public CommonValues([NotNull] IEnumerable<IRowValues> rows) : this()
		{
			if (rows is null)
				throw new ArgumentNullException(nameof(rows));

			foreach (var row in rows)
			{
				Add(row);
			}
		}

		public int FeatureCount { get; private set; }

		public int EntryCount => _commonValues.Count;

		public bool Exists(string name)
		{
			return name != null && _commonValues.ContainsKey(name);
		}

		public object GetValue(string name)
		{
			if (name == null) return null;
			return _commonValues.TryGetValue(name, out var value) ? value : null;
		}

		#region Private methods

		private void Add(IRowValues row)
		{
			if (FeatureCount == 0)
			{
				ProcessFields(row, CopyValue);
			}
			else
			{
				ProcessFields(row, CombineValues);
			}

			FeatureCount += 1;
		}

		private void ProcessFields(IRowValues row, Func<object, object, object> combine)
		{
			if (row is null)
				throw new ArgumentNullException(nameof(row));
			if (combine is null)
				throw new ArgumentNullException(nameof(combine));

			var fieldNames = row.FieldNames;
			int fieldCount = fieldNames.Count;

			for (int i = 0; i < fieldCount; i++)
			{
				var fieldName = fieldNames[i];
				if (string.IsNullOrEmpty(fieldName))
				{
					continue;
				}

				object value = row[i];
				if (value == DBNull.Value)
				{
					value = null;
				}

				object commonValue = GetValue(fieldName);

				_commonValues[fieldName] = combine(value, commonValue);
			}
		}

		private static object CopyValue(object value, object dummy)
		{
			return value;
		}

		private static object CombineValues(object value, object commonValue)
		{
			return Equals(value, commonValue) ? value : null;
		}

		#endregion
	}
}
