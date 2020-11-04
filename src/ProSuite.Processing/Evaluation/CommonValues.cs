using System;
using System.Collections.Generic;
using System.Diagnostics;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Processing.Utils;

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
			Assert.ArgumentNotNull(nameof(row));

			Add(row);
		}

		public CommonValues([NotNull] IEnumerable<IRowValues> rows) : this()
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

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
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(combine, nameof(combine));

			var fields = row.Fields;
			int fieldCount = fields.Count;

			for (int i = 0; i < fieldCount; i++)
			{
				Field field = fields[i];
				if (string.IsNullOrEmpty(field.Name))
				{
					continue;
				}

				object value = row[i];
				if (value == DBNull.Value)
				{
					value = null;
				}

				object commonValue = GetValue(field.Name);

				_commonValues[field.Name] = combine(value, commonValue);
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
