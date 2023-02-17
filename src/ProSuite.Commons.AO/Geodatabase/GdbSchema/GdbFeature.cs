using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <inheritdoc cref="GdbRow" />
	public abstract class GdbFeature : GdbRow, IFeature, IFeatureBuffer, IFeatureChanges, IReadOnlyFeature
	{
		private class PolycurveFeature : GdbFeature, IIndexedPolycurveFeature
		{
			private IndexedPolycurve _indexedPolycurve;

			public PolycurveFeature(long oid, GdbFeatureClass featureClass, IValueList valueList)
				: base(oid, featureClass, valueList) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => _indexedPolycurve == null;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments
				=> _indexedPolycurve ??
				   (_indexedPolycurve = new IndexedPolycurve((IPointCollection4)Shape));
		}

		private class MultiPatchFeature : GdbFeature, IIndexedMultiPatchFeature
		{
			private Geometry.Proxy.IndexedMultiPatch _indexedMultiPatch;

			public MultiPatchFeature(long oid, GdbFeatureClass featureClass, IValueList valueList)
				: base(oid, featureClass, valueList) { }

			bool IIndexedSegmentsFeature.AreIndexedSegmentsLoaded => true;

			IIndexedSegments IIndexedSegmentsFeature.IndexedSegments => IndexedMultiPatch;

			public Geometry.Proxy.IIndexedMultiPatch IndexedMultiPatch
				=> _indexedMultiPatch ??
				   (_indexedMultiPatch = new Geometry.Proxy.IndexedMultiPatch((IMultiPatch)Shape));
		}

		private class AnyFeature : GdbFeature
		{
			public AnyFeature(long oid, GdbFeatureClass featureClass, IValueList valueList)
				: base(oid, featureClass, valueList) { }
		}


		private readonly int _shapeFieldIndex;

		[NotNull] private readonly IFeatureClass _featureClass;

		private IGeometry _originalShape;

		#region Constructors

		public static GdbFeature Create(long oid, [NotNull] GdbFeatureClass featureClass,
		                                [CanBeNull] IValueList valueList = null)
		{
			if (featureClass.ShapeType == esriGeometryType.esriGeometryPolyline
			    || featureClass.ShapeType == esriGeometryType.esriGeometryPolygon)
			{
				return new PolycurveFeature(oid, featureClass, valueList);
			}

			throw new NotImplementedException();
		}

		protected GdbFeature(long oid, [NotNull] GdbFeatureClass featureClass,
		                     [CanBeNull] IValueList valueList = null)
			: base(oid, featureClass, valueList)
		{
			_featureClass = featureClass;
			_shapeFieldIndex = featureClass.ShapeFieldIndex;
		}

		#endregion

		#region IFeature implementation

#if Server11
		long IFeature.OID => OID;
#else
		int IFeature.OID => (int)OID;
#endif

		public override IGeometry ShapeCopy => Shape != null ? GeometryFactory.Clone(Shape) : null;
		ITable IFeature.Table => Table;
		IReadOnlyFeatureClass IReadOnlyFeature.FeatureClass => (IReadOnlyFeatureClass) ReadOnlyTable;

		public override IGeometry Shape
		{
			get
			{
				try
				{
					// TODO: The performance overhead of increaseRcwRefCount: true is very large,
					// but only in x64!
					object shapeProperty = ValueSet.GetValue(_shapeFieldIndex, true);

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

		protected override void RecycleCore()
		{
			_originalShape = null;
		}

		protected override void StoreCore()
		{
			base.StoreCore();

			// TODO: consider snapping Shape to dataset's spatial reference

			ShapeChanged = ! GeometryUtils.AreEqual(_originalShape, Shape);
		}
	}
}
