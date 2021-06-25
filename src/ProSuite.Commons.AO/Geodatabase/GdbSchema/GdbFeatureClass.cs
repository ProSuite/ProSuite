using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <inheritdoc cref="GdbTable" />
	public class GdbFeatureClass : GdbTable, IFeatureClass, IGeoDataset
	{
		public GdbFeatureClass(int objectClassId,
		                       [NotNull] string name,
		                       esriGeometryType shapeType,
		                       [CanBeNull] string aliasName = null,
		                       [CanBeNull] Func<GdbTable, BackingDataset> createBackingDataset = null,
		                       [CanBeNull] IWorkspace workspace = null)
			: base(objectClassId, name, aliasName, createBackingDataset, workspace)
		{
			ShapeType = shapeType;
		}

		protected override void FieldAddedCore(IField field)
		{
			base.FieldAddedCore(field);

			if (field.Type == esriFieldType.esriFieldTypeGeometry)
			{
				ShapeFieldName = field.Name;
			}
		}

		protected override IObject CreateObject(int oid)
		{
			return new GdbFeature(oid, this);
		}

		protected override esriDatasetType GetDatasetType()
		{
			return esriDatasetType.esriDTFeatureClass;
		}

		#region IGeoDataset Members

		public ISpatialReference SpatialReference { get; set; }

		IEnvelope IGeoDataset.Extent
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

		IFields IFeatureClass.Fields => Fields;

		IFeature IFeatureClass.CreateFeature() => CreateFeature();
		public GdbFeature CreateFeature()
		{
			return (GdbFeature) CreateObject(GetNextOid());
		}

		public IFeature GetFeature(int id)
		{
			return (IFeature) GetRow(id);
		}

		public IFeatureCursor GetFeatures(object ids, bool Recycling)
		{
			throw new NotImplementedException();
		}

		public IFeatureBuffer CreateFeatureBuffer()
		{
			return (IFeatureBuffer) CreateFeature();
		}

		public int FeatureCount(IQueryFilter queryFilter)
		{
			return RowCount(queryFilter);
		}

		public new IFeatureCursor Search(IQueryFilter filter, bool recycling)
		{
			if (BackingDataset == null)
			{
				throw new NotImplementedException("No backing dataset provided for Search().");
			}

			var rows = BackingDataset.Search(filter, recycling);

			return new FeatureCursorImpl(this, rows);
		}

		public IFeatureCursor Update(IQueryFilter filter, bool Recycling)
		{
			throw new NotImplementedException();
		}

		public IFeatureCursor Insert(bool useBuffering)
		{
			throw new NotImplementedException();
		}

		public ISelectionSet Select(IQueryFilter queryFilter,
		                            esriSelectionType selType,
		                            esriSelectionOption selOption,
		                            IWorkspace selectionContainer)
		{
			throw new NotImplementedException();
		}

		public esriGeometryType ShapeType { get; }

		public esriFeatureType FeatureType => esriFeatureType.esriFTSimple;

		public string ShapeFieldName { get; set; } = "SHAPE";

		public IField AreaField => null;

		public IField LengthField => null;

		public IFeatureDataset FeatureDataset => throw new NotImplementedException();

		public int FeatureClassID => ObjectClassID;

		#endregion

		#region Nested class FeatureCursorImpl

		protected class FeatureCursorImpl : CursorImpl, IFeatureCursor
		{
			public FeatureCursorImpl(GdbFeatureClass gdbFeatureClass, IEnumerable<IRow> rows)
				: base(gdbFeatureClass, rows) { }

			public IFeature NextFeature()
			{
				return (IFeature) NextRow();
			}

			public void UpdateFeature(IFeature Object)
			{
				throw new NotImplementedException();
			}

			public void DeleteFeature()
			{
				throw new NotImplementedException();
			}

			public object InsertFeature(IFeatureBuffer buffer)
			{
				throw new NotImplementedException();
			}
		}

		#endregion
	}
}
