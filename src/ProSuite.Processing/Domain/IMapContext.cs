namespace ProSuite.Processing.Domain
{
	/// <summary>
	/// Essential map properties in a framework-neutral way
	/// </summary>
	public interface IMapContext
	{
		double ReferenceScale { get; }

		// TODO double CurrentScale { get; } in case no reference scale is set

		// TODO int SRID { get; } Needed? Don't want SpatialReference here (dependencies) // AlignToGrid needs SRef

		// TODO ColorModel: RGB, CMYK, WithSpotColors?

		double PointsPerMapUnit { get; }

		double MapUnitsPerPoint { get; }
	}
}
