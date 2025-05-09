using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Gdb;

namespace ProSuite.Processing.Test.Mocks
{
	public class RowValuesMock : IRowValues
	{
		private readonly object[] _values;

		public RowValuesMock(params string[] fieldNames)
		{
			FieldNames = fieldNames;
			_values = new object[fieldNames.Length];
		}

		public IReadOnlyList<string> FieldNames { get; }

		public object this[int index]
		{
			get => _values[index];
			set => _values[index] = value;
		}

		public int FindField(string fieldName)
		{
			var names = FieldNames;
			var count = names.Count;

			for (int i = 0; i < count; i++)
			{
				if (string.Equals(fieldName, names[i], StringComparison.OrdinalIgnoreCase))
				{
					return i;
				}
			}

			return -1;
		}

		public bool Exists(string name)
		{
			if (name == null) return false;
			return FindField(name) >= 0;
		}

		public object GetValue(string name)
		{
			int index = FindField(name);
			return index < 0 ? null : _values[index];
		}

		public void SetValues(params object[] values)
		{
			if (values == null || values.Length != _values.Length)
				throw new ArgumentException(
					"Must pass exactly as many values as there are fields");

			for (int i = 0; i < values.Length; i++)
			{
				_values[i] = values[i];
			}
		}

		public void AssertValues(params object[] values)
		{
			if (values == null || values.Length != _values.Length)
				throw new ArgumentException(
					"Must pass exactly as many values as there are fields");

			for (int i = 0; i < values.Length; i++)
			{
				Assert.AreEqual(values[i], _values[i], "Values differ at index {0}", i);
			}
		}
	}
}
