using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.NamedValuesExpressions
{
	public class NamedValues
	{
		[NotNull] private readonly List<string> _values;

		public NamedValues([NotNull] string name) : this(name, new string[] { }) { }

		public NamedValues([NotNull] string name, [NotNull] IEnumerable<string> values)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(values, nameof(values));

			Name = name;
			_values = new List<string>(values);
		}

		[NotNull]
		public string Name { get; }

		[NotNull]
		public IEnumerable<string> Values => _values;

		public int ValueCount => _values.Count;

		[NotNull]
		public string GetValue(int index)
		{
			return _values[index];
		}
	}
}