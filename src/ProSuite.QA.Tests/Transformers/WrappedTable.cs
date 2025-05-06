using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// Gdb table implementation that wraps an existing table. This instance is considered equal
	/// to the base table in terms of hash-code and equality implementation.
	/// </summary>
	public class WrappedTable : GdbTable
	{
		private readonly IReadOnlyTable _baseTable;

		public WrappedTable([NotNull] IReadOnlyTable baseTable,
		                    Func<GdbTable, BackingDataset> createBackingDataset)
			: base(TransformedTableUtils.GetClassId(baseTable),
			       baseTable.Name, TransformedTableUtils.GetAliasName(baseTable),
			       createBackingDataset, baseTable.Workspace)
		{
			_baseTable = baseTable;

			for (int i = 0; i < _baseTable.Fields.FieldCount; i++)
			{
				IField field = _baseTable.Fields.Field[i];
				AddField(field);
			}
		}

		public WrappedTable(ITable template, bool useTemplateForQuerying = false)
			: base(template, useTemplateForQuerying)
		{
			_baseTable = ReadOnlyTableFactory.Create(template);
		}

		#region Overrides of GdbTable

		// We need to force the ObjectClassID to be the same as the base table for correct equality
		// comparison. This is the case even if the base table has an ObjectClassID of -1 in which
		// case the base class assigns a new (non-negative) ObjectClassID.
		// In the following situations the underlying AO-tables have an ObjectClassID of -1:
		// - FeatureClass from an in-memory workspace
		// - many-to-many association table of RelationshipClass
		public override int ObjectClassID => TransformedTableUtils.GetClassId(_baseTable);

		#endregion

		#region Equality members

		protected override bool EqualsCore(IReadOnlyTable otherTable)
		{
			return Equals(_baseTable, otherTable);
		}

		private bool Equals(WrappedTable other)
		{
			return Equals(_baseTable, other._baseTable);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj is IReadOnlyTable onlyTable)
			{
				return onlyTable.Equals(_baseTable);
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((WrappedTable) obj);
		}

		public override int GetHashCode()
		{
			return _baseTable.GetHashCode();
		}

		#endregion
	}
}
