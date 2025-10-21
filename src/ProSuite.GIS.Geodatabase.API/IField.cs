using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IField
	{
		string Name { get; }

		/// <summary>
		/// The field's alias name. This can be null (e.g. for SHAPE.AREA etc.)
		/// </summary>
		[CanBeNull]
		string AliasName { get; }

		esriFieldType Type { get; }

		[CanBeNull]
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
