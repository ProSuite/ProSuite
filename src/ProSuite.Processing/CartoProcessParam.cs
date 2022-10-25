using System;

namespace ProSuite.Processing
{
	public class CartoProcessParam
	{
		public string Name { get; }
		public Type Type { get; }
		public bool Required { get; }
		public string Description { get; }

		public CartoProcessParam(
			string name, Type type, bool required = false, string description = null)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Required = required;
			Description = description ?? string.Empty;
		}
	}
}
