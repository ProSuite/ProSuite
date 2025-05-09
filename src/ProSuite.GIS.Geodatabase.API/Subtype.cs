using System;
using System.Collections.Generic;

namespace ProSuite.GIS.Geodatabase.API
{
	public class Subtype
	{
		public int Code { get; }
		public string Name { get; }

		private readonly Dictionary<string, object> _defaultValues =
			new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

		private readonly Dictionary<string, IDomain> _domains =
			new Dictionary<string, IDomain>(StringComparer.OrdinalIgnoreCase);

		public Subtype(int code, string name)
		{
			Code = code;
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		public void SetDefaultValue(string fieldName, object value)
		{
			_defaultValues[fieldName] = value;
		}

		public object GetDefaultValue(string fieldName)
		{
			return _defaultValues.TryGetValue(fieldName, out var value) ? value : null;
		}

		public void SetAttributeDomain(string fieldName, IDomain domain)
		{
			_domains[fieldName] = domain;
		}

		public IDomain GetAttributeDomain(string fieldName)
		{
			return _domains.TryGetValue(fieldName, out var domain) ? domain : null;
		}
	}
}
