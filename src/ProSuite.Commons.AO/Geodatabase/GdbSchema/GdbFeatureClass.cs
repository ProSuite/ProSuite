using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <inheritdoc cref="GdbTable" />
	public class GdbFeatureClass : GdbTable, IFeatureClass, IGeoDataset, IReadOnlyFeatureClass,
	                               IRowCreator<GdbFeature>
	{
		private int _shapeFieldIndex = -1;

		public GdbFeatureClass(int? objectClassId,
		                       [NotNull] string name,
		                       esriGeometryType shapeType,
		                       [CanBeNull] string aliasName = null,
		                       [CanBeNull] Func<GdbTable, BackingDataset> createBackingDataset =
			                       null,
		                       [CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, aliasName, createBackingDataset, workspace)
		{
			ShapeType = shapeType;
		}

		public GdbFeatureClass([NotNull] IFeatureClass template,
		                       bool useTemplateForQuerying = false)
			: base((ITable) template, useTemplateForQuerying)
		{
			ShapeType = template.ShapeType;
		}

		public GdbFields FieldsT => GdbFields;

		public int ShapeFieldIndex
		{
			get
			{
				if (_shapeFieldIndex < 0)
				{
					string shapeFieldName = ShapeFieldName;

					if (shapeFieldName != null)
					{
						_shapeFieldIndex = FindField(shapeFieldName);
					}
				}

				return _shapeFieldIndex;
			}
		}

		protected override void FieldAddedCore(IField field)
		{
			base.FieldAddedCore(field);

			if (field.Type == esriFieldType.esriFieldTypeGeometry)
			{
				_shapeFieldName = field.Name;
			}
		}

		GdbFeature IRowCreator<GdbFeature>.CreateRow() => (GdbFeature) CreateRow();

		public new GdbFeature CreateFeature() => (GdbFeature) CreateRow();

		public override GdbRow CreateObject(long oid,
		                                    IValueList valueList = null)
			=> CreateFeature(oid, valueList);

		public GdbFeature CreateFeature(long oid,
		                                [CanBeNull] IValueList valueList = null)
		{
			return new GdbFeature(oid, this, valueList);
		}

		public override esriDatasetType DatasetType => esriDatasetType.esriDTFeatureClass;

		#region IGeoDataset Members

		public override IEnvelope Extent
		{
			get
			{
				if (BackingDataset == null)
				{
					throw new NotImplementedException("No backing dataset provided for Extent.");
				}

				return BackingDataset.Extent;
			}
		}

		#endregion

		#region IFeatureClass Members

		public override IFeatureCursor Search(IQueryFilter filter, bool recycling)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for Search().");
			}

			var rows = BackingDataset.Search(filter, recycling);
			return new CursorImpl(this, rows);
		}

		public override esriGeometryType ShapeType { get; }

		public override esriFeatureType FeatureType => esriFeatureType.esriFTSimple;

		private string _shapeFieldName;
		public override string ShapeFieldName => _shapeFieldName ?? base.ShapeFieldName;

		public override IField AreaField => null;

		public override IField LengthField => null;

		public override int FeatureClassID => ObjectClassID;

		#endregion
	}
}
