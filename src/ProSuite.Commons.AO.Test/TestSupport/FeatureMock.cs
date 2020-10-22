using System;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class FeatureMock : ObjectMock, IFeature, IFeatureBuffer, IFeatureChanges
	{
		private readonly int _shapeFieldIndex;
		[NotNull] private readonly FeatureClassMock _featureClassMock;

		#region Constructors

		internal FeatureMock(int oid, [NotNull] FeatureClassMock featureClassMock)
			: base(oid, featureClassMock)
		{
			_featureClassMock = featureClassMock;
			_shapeFieldIndex =
				_featureClassMock.FindField(_featureClassMock.ShapeFieldName);
		}

		#endregion

		#region IFeature implementation

		public IGeometry ShapeCopy => GeometryFactory.Clone(Shape);

		public IGeometry Shape
		{
			get
			{
				// Make sure that the marshal-reference-count is increased by exactyl 1 (i.e. do not call PropertySetUtils.HasProperty)
				string name = Convert.ToString(_shapeFieldIndex);

				try
				{
					return (IGeometry) _valueSet.GetProperty(name);
				}
				catch (COMException)
				{
					// E_Fail occurs if the value has never been set and does not exist in the property set.
					return null;
				}
			}
			set
			{
				Assert.AreEqual(_featureClassMock.ShapeType,
				                value?.GeometryType ??
				                _featureClassMock.ShapeType); // Allow null Shape

				OriginalShape = Shape;
				set_Value(_shapeFieldIndex, value);
			}
		}

		public IEnvelope Extent => Shape.Envelope;

		public esriFeatureType FeatureType => _featureClassMock.FeatureType;

		#endregion

		public bool ShapeChanged { get; private set; }

		public IGeometry OriginalShape { get; private set; }

		protected override void StoreCore()
		{
			base.StoreCore();

			ShapeChanged = ! GeometryUtils.AreEqual(OriginalShape, Shape);
		}
	}
}
