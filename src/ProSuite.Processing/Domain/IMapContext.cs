namespace ProSuite.Processing.Domain
{
	/// <summary>
	/// Essential map properties in a framework-neutral way
	/// </summary>
	public interface IMapContext
	{
		double ReferenceScale { get; }

		int SRID { get; }

		// TODO ColorModel: RGB, CMYK, WithSpotColors?

		double PointsToMapUnits(double distance);

		double MapUnitsToPoints(double distance);
	}
}
