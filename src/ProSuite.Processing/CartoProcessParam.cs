using System;

namespace ProSuite.Processing
{
	public class CartoProcessParam
	{
		public string Name { get; }
		public Type Type { get; }
		public bool Required { get; }
		public string Description { get; }
		public string Group { get; }

		public CartoProcessParam(
			string name, Type type, bool required = true, string description = null, string group = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Required = required;
			Description = description ?? string.Empty;
			Group = group?.Trim() ?? string.Empty;
		}
	}
}
