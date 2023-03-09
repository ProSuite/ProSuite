using System;

namespace ProSuite.Processing
{
	public class ParameterInfo
	{
		public string Name { get; }
		public Type Type { get; }
		public bool Required { get; }
		public bool Multivalued { get; }
		public object DefaultValue { get; }
		public string Group { get; }
		public int Order { get; }
		public string DocKey { get; }
		public Type Owner { get; }

		public ParameterInfo(Type owner, string name, Type type,
		                     bool required, bool multivalued, object defaultValue = null,
		                     string docKey = null, string group = null, int order = 0)
		{
			Owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Required = required;
			Multivalued = multivalued;
			DefaultValue = defaultValue;
			Group = group?.Trim() ?? string.Empty;
			Order = order;
			DocKey = docKey;
		}

		public override string ToString()
		{
			return $"{Name} (type {Type.Name}";
		}
	}
}
