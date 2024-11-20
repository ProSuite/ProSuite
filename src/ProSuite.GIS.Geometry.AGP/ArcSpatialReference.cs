using System;
using ArcGIS.Core.Geometry;
using ProSuite.GIS.Geometry.API;

namespace ProSuite.GIS.Geometry.AGP
{
	public class ArcSpatialReference : ISpatialReference,
	                                   ISpatialReferenceResolution,
	                                   ISpatialReferenceTolerance
	{
		private readonly SpatialReference _proSpatialReference;

		public ArcSpatialReference(SpatialReference proSpatialReference)
		{
			_proSpatialReference = proSpatialReference;
		}

		public SpatialReference ProSpatialReference => _proSpatialReference;

		#region Implementation of ISpatialReferenceInfo

		public string Name => _proSpatialReference.Name;

		public string Alias => throw new NotImplementedException();

		public string Abbreviation => throw new NotImplementedException();

		public string Remarks => throw new NotImplementedException();

		public int FactoryCode => _proSpatialReference.Wkid;

		public long SpatialReferenceImpl => throw new NotImplementedException();

		public long PrecisionImpl => throw new NotImplementedException();

		public long PrecisionExImpl => throw new NotImplementedException();

		public bool HasXYPrecision()
		{
			throw new NotImplementedException();
		}

		public bool HasZPrecision()
		{
			throw new NotImplementedException();
		}

		public bool HasMPrecision()
		{
			throw new NotImplementedException();
		}

		public void IsPrecisionEqual(ISpatialReference otherSr, out bool isPrecisionEqual)
		{
			// TODO: Compare actual resulution values
			SpatialReference otherAoSr;
			if (otherSr is ArcSpatialReference otherArcSpatialReference)
			{
				otherAoSr = otherArcSpatialReference.ProSpatialReference;
			}
			else
			{
				throw new NotImplementedException();
			}

			isPrecisionEqual =
				SpatialReference.AreEqual(_proSpatialReference, otherAoSr, false, true);
		}

		public void SetFalseOriginAndUnits(double falseX, double falseY, double xyUnits)
		{
			throw new NotImplementedException();
		}

		public void SetZFalseOriginAndUnits(double falseZ, double zUnits)
		{
			throw new NotImplementedException();
		}

		public void SetMFalseOriginAndUnits(double falseM, double mUnits)
		{
			throw new NotImplementedException();
		}

		public void GetFalseOriginAndUnits(out double falseX, out double falseY, out double xyUnits)
		{
			falseX = _proSpatialReference.FalseX;
			falseY = _proSpatialReference.FalseY;
			xyUnits = _proSpatialReference.Unit.ConversionFactor;
		}

		public void GetZFalseOriginAndUnits(out double falseZ, out double zUnits)
		{
			falseZ = _proSpatialReference.FalseZ;
			zUnits = _proSpatialReference.ZUnit.ConversionFactor;
		}

		public void GetMFalseOriginAndUnits(out double falseM, out double mUnits)
		{
			falseM = _proSpatialReference.FalseM;
			mUnits = double.NaN;
		}

		public void GetDomain(out double xMin, out double xMax, out double yMin, out double yMax)
		{
			xMin = _proSpatialReference.Domain.XMin;
			xMax = _proSpatialReference.Domain.XMax;
			yMin = _proSpatialReference.Domain.YMin;
			yMax = _proSpatialReference.Domain.YMax;
		}

		public void SetDomain(double xMin, double xMax, double yMin, double yMax)
		{
			throw new NotImplementedException();
		}

		public void GetZDomain(out double outZMin, out double outZMax)
		{
			outZMin = _proSpatialReference.Domain.ZMin;
			outZMax = _proSpatialReference.Domain.ZMax;
		}

		public void SetZDomain(double inZMin, double inZMax)
		{
			throw new NotImplementedException();
		}

		public void GetMDomain(out double outMMin, out double outMMax)
		{
			outMMin = _proSpatialReference.Domain.MMin;
			outMMax = _proSpatialReference.Domain.MMax;
		}

		public void SetMDomain(double inMMin, double inMMax)
		{
			throw new NotImplementedException();
		}

		public ILinearUnit ZCoordinateUnit
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public void Changed()
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Implementation of ISpatialReferenceResolution

		public double get_XYResolution(bool bStandardUnits)
		{
			// TODO: Probably not the same any more!
			return ProSpatialReference.XYResolution;
		}

		public double get_ZResolution(bool bStandardUnits)
		{
			return ProSpatialReference.ZScale;
		}

		#endregion

		#region Implementation of ISpatialReferenceTolerance

		public double XYTolerance => ProSpatialReference.XYTolerance;
		public double ZTolerance => ProSpatialReference.ZTolerance;
		public double MTolerance => ProSpatialReference.MTolerance;

		#endregion

		#region Equality members

		protected bool Equals(ArcSpatialReference other)
		{
			return SpatialReference.AreEqual(_proSpatialReference, other._proSpatialReference,
			                                 false, true);
		}

		public override bool Equals(object obj)
		{
			if (obj is null)
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ArcSpatialReference) obj);
		}

		public override int GetHashCode()
		{
			if (_proSpatialReference == null)
			{
				return 0;
			}

			return HashCode.Combine(_proSpatialReference.LatestWkid,
			                        _proSpatialReference.VcsWkid);
		}

		#endregion
	}
}
