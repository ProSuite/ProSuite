extern alias EsriGeometry;
using System;
using ESRI.ArcGIS.Geometry;
using ISpatialReference = ESRI.ArcGIS.Geometry.ISpatialReference;

namespace ProSuite.ArcGIS.Geometry.AO
{
	public class ArcSpatialReference : ISpatialReference
	{
		private readonly EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReference _aoSpatialReference;

		public ArcSpatialReference(EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReference aoSpatialReference)
		{
			_aoSpatialReference = aoSpatialReference;
		}

		public EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReference AoSpatialReference => _aoSpatialReference;

		#region Implementation of ISpatialReferenceInfo

		public string Name => ((EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReferenceInfo)_aoSpatialReference).Name;

		public string Alias => ((EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReferenceInfo)_aoSpatialReference).Alias;

		public string Abbreviation => ((EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReferenceInfo)_aoSpatialReference).Abbreviation;

		public string Remarks => ((EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReferenceInfo)_aoSpatialReference).Remarks;

		public int FactoryCode => ((EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReferenceInfo)_aoSpatialReference).FactoryCode;

		public long SpatialReferenceImpl => _aoSpatialReference.SpatialReferenceImpl;

		public long PrecisionImpl => _aoSpatialReference.PrecisionImpl;

		public long PrecisionExImpl => _aoSpatialReference.PrecisionExImpl;

		public bool HasXYPrecision()
		{
			return _aoSpatialReference.HasXYPrecision();
		}

		public bool HasZPrecision()
		{
			return _aoSpatialReference.HasZPrecision();
		}

		public bool HasMPrecision()
		{
			return _aoSpatialReference.HasMPrecision();
		}

		public void IsPrecisionEqual(ISpatialReference otherSr, out bool isPrecisionEqual)
		{
			EsriGeometry::ESRI.ArcGIS.Geometry.ISpatialReference otherAoSr;
			if (otherSr is ArcSpatialReference otherArcSpatialReference)
			{
				otherAoSr = otherArcSpatialReference.AoSpatialReference;
			}
			else
			{
				throw new NotImplementedException();
			}

			_aoSpatialReference.IsPrecisionEqual(otherAoSr, out isPrecisionEqual);
		}

		public void SetFalseOriginAndUnits(double falseX, double falseY, double xyUnits)
		{
			_aoSpatialReference.SetFalseOriginAndUnits(falseX, falseY, xyUnits);
		}

		public void SetZFalseOriginAndUnits(double falseZ, double zUnits)
		{
			_aoSpatialReference.SetZFalseOriginAndUnits(falseZ, zUnits);
		}

		public void SetMFalseOriginAndUnits(double falseM, double mUnits)
		{
			_aoSpatialReference.SetMFalseOriginAndUnits(falseM, mUnits);
		}

		public void GetFalseOriginAndUnits(out double falseX, out double falseY, out double xyUnits)
		{
			_aoSpatialReference.GetFalseOriginAndUnits(out falseX, out falseY, out xyUnits);
		}

		public void GetZFalseOriginAndUnits(out double falseZ, out double zUnits)
		{
			_aoSpatialReference.GetZFalseOriginAndUnits(out falseZ, out zUnits);
		}

		public void GetMFalseOriginAndUnits(out double falseM, out double mUnits)
		{
			_aoSpatialReference.GetMFalseOriginAndUnits(out falseM, out mUnits);
		}

		public void GetDomain(out double xMin, out double xMax, out double yMin, out double yMax)
		{
			_aoSpatialReference.GetDomain(out xMin, out xMax, out yMin, out yMax);
		}

		public void SetDomain(double xMin, double xMax, double yMin, double yMax)
		{
			_aoSpatialReference.SetDomain(xMin, xMax, yMin, yMax);
		}

		public void GetZDomain(out double outZMin, out double outZMax)
		{
			_aoSpatialReference.GetZDomain(out outZMin, out outZMax);
		}

		public void SetZDomain(double inZMin, double inZMax)
		{
			_aoSpatialReference.SetZDomain(inZMin, inZMax);
		}

		public void GetMDomain(out double outMMin, out double outMMax)
		{
			_aoSpatialReference.GetMDomain(out outMMin, out outMMax);
		}

		public void SetMDomain(double inMMin, double inMMax)
		{
			_aoSpatialReference.SetMDomain(inMMin, inMMax);
		}

		public ILinearUnit ZCoordinateUnit
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void Changed()
		{
			_aoSpatialReference.Changed();
		}

		#endregion
	}
}
