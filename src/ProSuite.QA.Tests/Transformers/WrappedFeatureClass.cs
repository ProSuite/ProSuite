using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	public class WrappedFeatureClass : GdbFeatureClass
	{
		private readonly IReadOnlyFeatureClass _baseClass;

		public WrappedFeatureClass([NotNull] IReadOnlyFeatureClass baseClass,
		                           Func<GdbTable, BackingDataset> createBackingDataset)
			: base(TransformedTableUtils.GetClassId(baseClass),
			       baseClass.Name, baseClass.ShapeType,
			       TransformedTableUtils.GetAliasName(baseClass),
			       createBackingDataset, baseClass.Workspace)
		{
			_baseClass = baseClass;

			for (int i = 0; i < _baseClass.Fields.FieldCount; i++)
			{
				IField field = _baseClass.Fields.Field[i];
				AddField(field);
			}
		}

		public WrappedFeatureClass([NotNull] IFeatureClass template,
		                           bool useTemplateForQuerying = false)
			: base(template, useTemplateForQuerying)
		{
			_baseClass = ReadOnlyTableFactory.Create(template);
		}

		#region Overrides of GdbTable

		// We need to force the ObjectClassID to be the same as the base table for correct equality
		// comparison. This is the case even if the base table has an ObjectClassID of -1 in which
		// case the base class assigns a new (non-negative) ObjectClassID.
		// In the following situations the underlying AO-tables have an ObjectClassID of -1:
		// - FeatureClass from an in-memory workspace
		// - many-to-many association table of RelationshipClass
		public override int ObjectClassID => TransformedTableUtils.GetClassId(_baseClass);

		#endregion

		/// <summary>
		/// Override the Equals(IReadOnlyTable) to ensure all equals comparisons are re-directed to this
		/// class.
		/// </summary>
		/// <param name="otherTable"></param>
		/// <returns></returns>
		public override bool Equals(IReadOnlyTable otherTable)
		{
			if (otherTable is WrappedFeatureClass otherWrappedFeatureClass)
			{
				return Equals(otherWrappedFeatureClass);
			}

			return base.Equals(otherTable);
		}

		#region Equality members

		protected override bool EqualsCore(IReadOnlyTable otherTable)
		{
			return Equals(_baseClass, otherTable);
		}

		protected bool Equals(WrappedFeatureClass other)
		{
			return Equals(_baseClass, other._baseClass);
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

			if (obj is IReadOnlyFeatureClass otherFeatureClass)
			{
				return otherFeatureClass.Equals(_baseClass);
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((WrappedFeatureClass) obj);
		}

		public override int GetHashCode()
		{
			return (_baseClass != null ? _baseClass.GetHashCode() : 0);
		}

		#endregion
	}
}
