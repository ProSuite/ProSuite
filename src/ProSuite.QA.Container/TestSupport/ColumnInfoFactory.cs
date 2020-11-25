using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public class ColumnInfoFactory
	{
		private readonly ITable _table;
		private readonly IFields _fields;

		private const string _shapeAreaFieldAlias = "$ShapeArea";
		private const string _shapeLengthFieldAlias = "$ShapeLength";
		private const string _shapeVertexCountFieldAlias = "$ShapeVertexCount";
		private const string _shapeXMinFieldAlias = "$ShapeXMin";
		private const string _shapeXMaxFieldAlias = "$ShapeXMax";
		private const string _shapeYMinFieldAlias = "$ShapeYMin";
		private const string _shapeYMaxFieldAlias = "$ShapeYMax";
		private const string _shapeMMinFieldAlias = "$ShapeMMin";
		private const string _shapeMMaxFieldAlias = "$ShapeMMax";
		private const string _shapeZMinFieldAlias = "$ShapeZMin";
		private const string _shapeZMaxFieldAlias = "$ShapeZMax";
		private const string _shapePartCountFieldAlias = "$ShapePartCount";
		private const string _shapeInteriorRingCountFieldAlias = "$ShapeInteriorRingCount";
		private const string _shapeExteriorRingCountFieldAlias = "$ShapeExteriorRingCount";
		private const string _objectIdFieldAlias = "$ObjectID";
		private const string _tableNameFieldAlias = "$TableName";
		private const string _qualifiedTableNameFieldAlias = "$QualifiedTableName";
		private const string _aliasPrefix = "$";

		[NotNull] private static readonly
			IDictionary<string, Func<ITable, string, ColumnInfo>> _propertyAliases
				= GetPropertyAliases();

		[NotNull]
		private static IDictionary<string, Func<ITable, string, ColumnInfo>>
			GetPropertyAliases()
		{
			return new Dictionary<string, Func<ITable, string, ColumnInfo>>(
				       StringComparer.OrdinalIgnoreCase)
			       {
				       {
					       _shapeAreaFieldAlias, (t, a) => new ShapeAreaAliasColumnInfo(t, a)
				       },
				       {
					       _shapeLengthFieldAlias, (t, a) => new ShapeLengthAliasColumnInfo(t, a)
				       },
				       {
					       _shapeVertexCountFieldAlias,
					       (t, a) => new ShapeVertexCountAliasColumnInfo(t, a)
				       },
				       {
					       _shapeXMinFieldAlias, (t, a) => new ShapeXMinAliasColumnInfo(t, a)
				       },
				       {
					       _shapeXMaxFieldAlias, (t, a) => new ShapeXMaxAliasColumnInfo(t, a)
				       },
				       {
					       _shapeYMinFieldAlias, (t, a) => new ShapeYMinAliasColumnInfo(t, a)
				       },
				       {
					       _shapeYMaxFieldAlias, (t, a) => new ShapeYMaxAliasColumnInfo(t, a)
				       },
				       {
					       _shapeMMinFieldAlias, (t, a) => new ShapeMMinAliasColumnInfo(t, a)
				       },
				       {
					       _shapeMMaxFieldAlias, (t, a) => new ShapeMMaxAliasColumnInfo(t, a)
				       },
				       {
					       _shapeZMinFieldAlias, (t, a) => new ShapeZMinAliasColumnInfo(t, a)
				       },
				       {
					       _shapeZMaxFieldAlias, (t, a) => new ShapeZMaxAliasColumnInfo(t, a)
				       },
				       {
					       _shapePartCountFieldAlias,
					       (t, a) => new ShapePartCountAliasColumnInfo(t, a)
				       },
				       {
					       _shapeInteriorRingCountFieldAlias,
					       (t, a) => new ShapeInteriorRingCountAliasColumnInfo(t, a)
				       },
				       {
					       _shapeExteriorRingCountFieldAlias,
					       (t, a) => new ShapeExteriorRingCountAliasColumnInfo(t, a)
				       },
				       {
					       _objectIdFieldAlias, (t, a) => new ObjectIdAliasColumnInfo(t, a)
				       },
				       {
					       _tableNameFieldAlias, (t, a) => new TableNameColumnInfo(t, a)
				       },
				       {
					       _qualifiedTableNameFieldAlias,
					       (t, a) => new TableNameColumnInfo(t, a, qualified: true)
				       }
			       };
		}

		[CLSCompliant(false)]
		public ColumnInfoFactory([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;
			_fields = table.Fields;
		}

		[CanBeNull]
		[CLSCompliant(false)]
		public ColumnInfo GetColumnInfo([NotNull] string expressionToken)
		{
			int fieldIndex = _table.FindField(expressionToken);
			if (fieldIndex >= 0)
			{
				IField field = _fields.Field[fieldIndex];

				return new FieldColumnInfo(_table, field, fieldIndex);
			}

			// the field is not found - check if it is an alias

			Func<ITable, string, ColumnInfo> constructor;
			if (expressionToken.StartsWith(_aliasPrefix, StringComparison.OrdinalIgnoreCase) &&
			    _propertyAliases.TryGetValue(expressionToken, out constructor))
			{
				return constructor(_table, expressionToken);
			}

			return null;
		}
	}
}
