namespace ESRI.ArcGIS.Geodatabase
{
	public interface IField
	{
		string Name { get; }

		string AliasName { get; }

		esriFieldType Type { get; }

		IDomain Domain { get; }

		object DefaultValue { get; }

		int Length { get; }

		int Precision { get; }

		int Scale { get; }

		bool IsNullable { get; }

		IGeometryDef GeometryDef { get; }

		int VarType { get; }

		bool DomainFixed { get; }

		bool Required { get; }

		bool Editable { get; }

		bool CheckValue(object value);
	}
}
