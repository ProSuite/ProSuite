namespace ProSuite.GIS.Geometry.API
{
	public interface IEnvelope : IGeometry
	{
		double Width { get; }

		double Height { get; }

		double Depth { get; }

		IPoint LowerLeft { get; }

		IPoint UpperLeft { get; }

		IPoint UpperRight { get; }

		IPoint LowerRight { get; }

		double XMin { get; set; }

		double YMin { get; set; }

		double XMax { get; set; }

		double YMax { get; set; }

		double MMin { get; set; }

		double MMax { get; set; }

		double ZMin { get; set; }

		double ZMax { get; set; }

		void Union(IEnvelope other);

		void Intersect(IEnvelope inEnvelope);

		//void Offset(double x, double y);

		//void OffsetZ(double z);

		//void OffsetM(double m);

		void Expand(double dx, double dy, bool asRatio);

		//void ExpandZ(double dz, bool asRatio);

		//void ExpandM(double dm, bool asRatio);

		//void DefineFromWKSPoints(int Count, ref WKSPoint Points);

		//void DefineFromPoints(int Count, ref IPoint Points);

		//void QueryWKSCoords(out WKSEnvelope e);

		//void PutWKSCoords(ref WKSEnvelope e);

		void PutCoords(double xMin, double yMin, double xMax, double yMax);

		void QueryCoords(out double xMin, out double yMin, out double xMax, out double yMax);

		//void CenterAt(IPoint p);
	}
}
