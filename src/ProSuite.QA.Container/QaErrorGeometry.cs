using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class QaErrorGeometry
	{
		[CanBeNull] [ThreadStatic] private static IEnvelope _envelopeTemplate;

		private readonly bool _hasGeometry;

		private bool _envelopeKnown;
		private IGeometry _geometry;
		private double _xMax;
		private double _xMin;
		private double _yMax;
		private double _yMin;
		private double _xyTolerance;
		private IGeometry _geometryInModelSpatialRef;

		public QaErrorGeometry([CanBeNull] IGeometry geometry)
		{
			_geometry = geometry;

			_envelopeKnown = false;
			_hasGeometry = _geometry != null;
		}

		/// <summary>
		/// unreduced geometry,
		/// will throw InvalidOperation when the error is completely processed by the TestContainer and TestContainer.KeepErrorGeometry == false
		/// </summary>
		[CanBeNull]
		public IGeometry Geometry
		{
			get
			{
				if (_hasGeometry && _geometry == null)
				{
					throw new InvalidOperationException("Geometry has been reduced");
				}

				return _geometry;
			}
		}

		public IGeometry GetGeometryInModelSpatialRef()
		{
			return _geometryInModelSpatialRef ?? _geometry;
		}

		public WKSEnvelope? GetEnvelope()
		{
			if (! VerifyEnvelope())
			{
				return null;
			}

			return new WKSEnvelope { XMin = _xMin, YMin = _yMin, XMax = _xMax, YMax = _yMax };
		}

		public int CompareEnvelope(QaErrorGeometry other)
		{
			if (! _hasGeometry)
			{
				return other._hasGeometry
					       ? -1
					       : 0;
			}

			if (! other._hasGeometry)
			{
				return 1;
			}

			bool thisHasEnv = VerifyEnvelope();
			bool otherHasEnv = other.VerifyEnvelope();

			if (thisHasEnv == false)
			{
				return otherHasEnv
					       ? -1
					       : 0;
			}

			if (! otherHasEnv)
			{
				return 1;
			}

			if (! IsWithinTolerance(_xMin, other._xMin))
			{
				return Math.Sign(_xMin - other._xMin);
			}

			if (! IsWithinTolerance(_yMin, other._yMin))
			{
				return Math.Sign(_yMin - other._yMin);
			}

			if (! IsWithinTolerance(_xMax, other._xMax))
			{
				return Math.Sign(_xMax - other._xMax);
			}

			if (! IsWithinTolerance(_yMax, other._yMax))
			{
				return Math.Sign(_yMax - other._yMax);
			}

			return 0;
		}

		public void ReduceGeometry()
		{
			VerifyEnvelope();
			_geometry = null;
		}

		public bool IsProcessed(double xMax, double yMax)
		{
			VerifyEnvelope();
			if (! _envelopeKnown)
			{
				return true;
			}

			return _xMax < xMax && _yMax < yMax;
		}

		private bool VerifyEnvelope()
		{
			if (_envelopeKnown)
			{
				return true;
			}

			if (_geometry == null || _geometry.IsEmpty)
			{
				return false;
			}

			if (_envelopeTemplate == null)
			{
				_envelopeTemplate = new EnvelopeClass();
			}

			_geometry.QueryEnvelope(_envelopeTemplate);
			if (! _envelopeTemplate.IsEmpty)
			{
				_envelopeTemplate.QueryCoords(out _xMin, out _yMin,
				                              out _xMax, out _yMax);

				_xyTolerance = GetXyTolerance(_geometry.SpatialReference,
				                              _xMin, _yMin,
				                              _xMax, _yMax);
			}

			_envelopeKnown = true;
			return true;
		}

		private static double GetXyTolerance([CanBeNull] ISpatialReference spatialReference,
		                                     double xMin, double yMin,
		                                     double xMax, double yMax)
		{
			var tolerance = spatialReference as ISpatialReferenceTolerance;
			if (tolerance != null)
			{
				double xyTolerance = tolerance.XYTolerance;
				if (xyTolerance > 0)
				{
					return xyTolerance;
				}
			}

			// hack: come up with tolerance for lat/long or (assuming!) meters

			const double toleranceM = 0.02; // 2 cm
			const double circumferenceM = 40000000;
			const double circumferenceDeg = 360;

			return AssumeLatLong(xMin, yMin, xMax, yMax)
				       ? toleranceM / circumferenceM * circumferenceDeg
				       : toleranceM;
		}

		private static bool AssumeLatLong(double xMin, double yMin, double xMax,
		                                  double yMax)
		{
			return xMin > -200 && yMin > -100 && xMax < 200 && yMax < 100;
		}

		private bool IsWithinTolerance(double v0, double v1)
		{
			double epsilon = MathUtils.GetDoubleSignificanceEpsilon(v0, v1);
			double difference = Math.Abs(v0 - v1);

			return MathUtils.IsWithinTolerance(difference, _xyTolerance, epsilon);
		}

		public void SetGeometryInModelSpatialReference(IGeometry projectedGeometry)
		{
			_geometryInModelSpatialRef = projectedGeometry;
		}
	}
}
