using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <inheritdoc cref="GdbRow" />
	public class GdbFeature : GdbRow, IFeature, IFeatureBuffer, IFeatureChanges
	{
		private readonly int _shapeFieldIndex;

		[NotNull] private readonly IFeatureClass _featureClass;

		private IGeometry _originalShape;

		#region Constructors

		public GdbFeature(int oid, [NotNull] GdbFeatureClass featureClass)
			: base(oid, featureClass)
		{
			_featureClass = featureClass;
			_shapeFieldIndex =
				_featureClass.FindField(_featureClass.ShapeFieldName);
		}

		#endregion

		#region IFeature implementation

		public override IGeometry ShapeCopy => Shape != null ? GeometryFactory.Clone(Shape) : null;
		ITable IFeature.Table => Table;

		public override IGeometry Shape
		{
			get
			{
				// Make sure that the marshal-reference-count is increased by exactyl 1 (i.e. do not call PropertySetUtils.HasProperty)
				string name = Convert.ToString(_shapeFieldIndex);

				try
				{
					object shapeProperty = ValueSet.GetProperty(name);

					if (shapeProperty == DBNull.Value)
					{
						return null;
					}

					return (IGeometry) shapeProperty;
				}
				catch (COMException)
				{
					// E_Fail occurs if the value has never been set and does not exist in the property set.
					return null;
				}
			}
			set
			{
				esriGeometryType? shapeType = value?.GeometryType;

				if (shapeType != null)
				{
					// Allow null Shape
					Assert.AreEqual(_featureClass.ShapeType, shapeType.Value,
					                "Invalid geometry type: {0}", shapeType);
				}

				_originalShape = Shape;
				set_Value(_shapeFieldIndex, value);
			}
		}


		public override esriFeatureType FeatureType => _featureClass.FeatureType;

		#endregion

		public bool ShapeChanged { get; private set; }

		IGeometry IFeatureChanges.OriginalShape => _originalShape;

		protected override void StoreCore()
		{
			base.StoreCore();

			// TODO: consider snapping Shape to dataset's spatial reference

			ShapeChanged = ! GeometryUtils.AreEqual(_originalShape, Shape);
		}
	}
}
