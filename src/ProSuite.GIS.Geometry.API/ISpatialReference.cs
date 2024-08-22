namespace ESRI.ArcGIS.Geometry
{
	public interface ISpatialReference : ISpatialReferenceInfo
	{
		new string Name { get; }

		new string Alias { get; }

		new string Abbreviation { get; }

		new string Remarks { get; }

		new int FactoryCode { get; }

		long SpatialReferenceImpl { get; }

		long PrecisionImpl { get; }

		long PrecisionExImpl { get; }

		bool HasXYPrecision();

		bool HasZPrecision();

		bool HasMPrecision();

		void IsPrecisionEqual(ISpatialReference otherSR, out bool IsPrecisionEqual);

		void SetFalseOriginAndUnits(double falseX, double falseY, double xyUnits);

		void SetZFalseOriginAndUnits(double falseZ, double zUnits);

		void SetMFalseOriginAndUnits(double falseM, double mUnits);

		void GetFalseOriginAndUnits(out double falseX, out double falseY, out double xyUnits);

		void GetZFalseOriginAndUnits(out double falseZ, out double zUnits);

		void GetMFalseOriginAndUnits(out double falseM, out double mUnits);

		void GetDomain(out double XMin, out double XMax, out double YMin, out double YMax);

		void SetDomain(double XMin, double XMax, double YMin, double YMax);

		void GetZDomain(out double outZMin, out double outZMax);

		void SetZDomain(double inZMin, double inZMax);

		void GetMDomain(out double outMMin, out double outMMax);

		void SetMDomain(double inMMin, double inMMax);

		ILinearUnit ZCoordinateUnit { get; set; }

		void Changed();
	}

	public interface ILinearUnit : IUnit
	{
		double MetersPerUnit { get; }
	}

	public interface IUnit : ISpatialReferenceInfo
	{
		double ConversionFactor { get; }
	}

	public interface ISpatialReferenceInfo
	{
		string Name { get; }

		string Alias { get; }

		string Abbreviation { get; }

		string Remarks { get; }

		int FactoryCode { get; }
	}
}
