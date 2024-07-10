using System;
using System.Linq;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class VectorDataset : ObjectDataset, IVectorDataset, IFeatureClassSchemaDef
	{
		[UsedImplicitly] private LayerFile _defaultSymbology;

		[UsedImplicitly] private double? _minimumSegmentLength; // name should be ..Override

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VectorDataset"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected VectorDataset() { }

		protected VectorDataset([NotNull] string name) : base(name) { }

		protected VectorDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation)
			: base(name, abbreviation) { }

		protected VectorDataset([NotNull] string name,
		                        [CanBeNull] string abbreviation,
		                        [CanBeNull] string aliasName)
			: base(name, abbreviation, aliasName) { }

		#endregion

		[UsedImplicitly]
		private LayerFile DefaultSymbology => _defaultSymbology;

		#region IVectorDataset

		public double MinimumSegmentLength
		{
			get { return _minimumSegmentLength ?? Model.DefaultMinimumSegmentLength; }
			set { _minimumSegmentLength = value; }
		}

		public double? MinimumSegmentLengthOverride
		{
			get { return _minimumSegmentLength; }
			set { _minimumSegmentLength = value; }
		}

		public override bool HasGeometry => true;

		public LayerFile DefaultLayerFile
		{
			get { return _defaultSymbology; }
			set { _defaultSymbology = value; }
		}

		#endregion

		public override DatasetType DatasetType => DatasetType.FeatureClass;

		#region Implementation of IFeatureClassSchema

		public string ShapeFieldName =>
			GetAttributes().FirstOrDefault(a => a.FieldType == FieldType.Geometry)?.Name;

		public ProSuiteGeometryType ShapeType
		{
			get
			{
				GeometryTypeShape geometryTypeShape = ((GeometryTypeShape) GeometryType);

				return geometryTypeShape?.ShapeType ?? ProSuiteGeometryType.Null;
			}
		}

		public ITableField AreaField =>
			GetAttributes().FirstOrDefault(a => AttributeRole.ShapeArea.Equals(a.Role));

		public ITableField LengthField => throw new NotImplementedException();

		#endregion
	}
}
