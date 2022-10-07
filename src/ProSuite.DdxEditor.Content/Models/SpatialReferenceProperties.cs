using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.Models
{
	public class SpatialReferenceProperties
	{
		private readonly string _wellKnownText;
		private readonly bool _isHighPrecision;
		private readonly double? _xyTolerance;
		private readonly bool? _xyToleranceValid;
		private readonly double? _xyResolution;
		private readonly double? _domainXMin;
		private readonly double? _domainYMin;
		private readonly double? _domainXMax;
		private readonly double? _domainYMax;
		private readonly double? _zResolution;
		private readonly double? _zTolerance;
		private readonly bool? _zToleranceValid;
		private readonly double? _mResolution;
		private readonly double? _mTolerance;
		private readonly bool? _mToleranceValid;
		private readonly double? _domainZMin;
		private readonly double? _domainZMax;
		private readonly double? _domainMMin;
		private readonly double? _domainMMax;
		private readonly int _factoryCode;
		private readonly string _csName;
		private readonly string _vcsName;

		public SpatialReferenceProperties([NotNull] ISpatialReference sref)
		{
			Assert.ArgumentNotNull(sref, nameof(sref));

			var controlPrecision = (IControlPrecision3) sref;
			var tolerance = (ISpatialReferenceTolerance) sref;
			var resolution = (ISpatialReferenceResolution) sref;

			_factoryCode = sref.FactoryCode;
			_csName = sref.Name;
			_wellKnownText = SpatialReferenceUtils.ExportToESRISpatialReference(sref);
			_isHighPrecision = controlPrecision.IsHighPrecision;

			IVerticalCoordinateSystem vcs =
				SpatialReferenceUtils.GetVerticalCoordinateSystem(sref);

			if (vcs != null)
			{
				_vcsName = vcs.Name;
			}

			if (sref.HasXYPrecision())
			{
				_xyResolution = resolution.XYResolution[true];
				_xyTolerance = tolerance.XYTolerance;
				_xyToleranceValid = tolerance.XYToleranceValid ==
				                    esriSRToleranceEnum.esriSRToleranceOK;

				sref.GetDomain(out double xMin, out double xMax, out double yMin, out double yMax);
				_domainXMin = xMin;
				_domainYMin = yMin;
				_domainXMax = xMax;
				_domainYMax = yMax;
			}

			if (sref.HasZPrecision())
			{
				_zResolution = resolution.ZResolution[true];
				_zTolerance = tolerance.ZTolerance;
				_zToleranceValid = tolerance.ZToleranceValid ==
				                   esriSRToleranceEnum.esriSRToleranceOK;

				sref.GetZDomain(out double zMin, out double zMax);
				_domainZMin = zMin;
				_domainZMax = zMax;
			}

			if (sref.HasMPrecision())
			{
				_mResolution = resolution.MResolution;
				_mTolerance = tolerance.MTolerance;
				_mToleranceValid = tolerance.MToleranceValid ==
				                   esriSRToleranceEnum.esriSRToleranceOK;

				sref.GetMDomain(out double mMin, out double mMax);
				_domainMMin = mMin;
				_domainMMax = mMax;
			}
		}

		public int FactoryCode => _factoryCode;

		public string CSName => _csName;

		public string WellKnownText => _wellKnownText;

		public bool IsHighPrecision => _isHighPrecision;

		public string VcsName => _vcsName;

		public double? XyTolerance => _xyTolerance;

		public bool? XyToleranceValid => _xyToleranceValid;

		public double? XyResolution => _xyResolution;

		public double? DomainXMin => _domainXMin;

		public double? DomainYMin => _domainYMin;

		public double? DomainXMax => _domainXMax;

		public double? DomainYMax => _domainYMax;

		public double? ZResolution => _zResolution;

		public double? ZTolerance => _zTolerance;

		public bool? ZToleranceValid => _zToleranceValid;

		public double? DomainZMin => _domainZMin;

		public double? DomainZMax => _domainZMax;

		public double? MResolution => _mResolution;

		public double? MTolerance => _mTolerance;

		public bool? MToleranceValid => _mToleranceValid;

		public double? DomainMMin => _domainMMin;

		public double? DomainMMax => _domainMMax;
	}
}
