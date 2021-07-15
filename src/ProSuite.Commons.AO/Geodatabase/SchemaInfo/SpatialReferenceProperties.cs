using System.ComponentModel;
using System.Globalization;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase.SchemaInfo
{
	public class SpatialReferenceProperties
	{
		public SpatialReferenceProperties([NotNull] ISpatialReference spatialReference)
		{
			Name = spatialReference.Name;

			CoordinateSystem = GetCoordinateSystemProperties(spatialReference);

			var resolution = spatialReference as ISpatialReferenceResolution;
			var tolerance = spatialReference as ISpatialReferenceTolerance;

			if (spatialReference.HasXYPrecision())
			{
				double xmin;
				double ymin;
				double xmax;
				double ymax;
				spatialReference.GetDomain(out xmin, out xmax, out ymin, out ymax);

				XYDomain = new XyDomainProperties(xmin, ymin, xmax, ymax);

				if (resolution != null)
				{
					XYResolution = Format(resolution.XYResolution[true]);
				}

				if (tolerance != null)
				{
					XYTolerance = Format(tolerance.XYTolerance);
				}
			}

			if (spatialReference.HasZPrecision())
			{
				double min;
				double max;
				spatialReference.GetZDomain(out min, out max);

				ZDomain = new ZDomainProperties(min, max);

				if (resolution != null)
				{
					ZResolution = Format(resolution.ZResolution[true]);
				}

				if (tolerance != null)
				{
					ZTolerance = Format(tolerance.ZTolerance);
				}
			}

			if (spatialReference.HasMPrecision())
			{
				double min;
				double max;
				spatialReference.GetMDomain(out min, out max);

				MDomain = new MDomainProperties(min, max);

				if (resolution != null)
				{
					MResolution = Format(resolution.MResolution);
				}

				if (tolerance != null)
				{
					MTolerance = Format(tolerance.MTolerance);
				}
			}
		}

		[UsedImplicitly]
		public string Name { get; private set; }

		[DisplayName("XY Resolution")]
		[UsedImplicitly]
		public string XYResolution { get; private set; }

		[DisplayName("XY Tolerance")]
		[UsedImplicitly]
		public string XYTolerance { get; private set; }

		[TypeConverter(typeof(AllPropertiesConverter))]
		[DisplayName("XY Domain")]
		[UsedImplicitly]
		public XyDomainProperties XYDomain { get; private set; }

		[DisplayName("Z Resolution")]
		[UsedImplicitly]
		public string ZResolution { get; private set; }

		[DisplayName("Z Tolerance")]
		[UsedImplicitly]
		public string ZTolerance { get; private set; }

		[TypeConverter(typeof(AllPropertiesConverter))]
		[DisplayName("Z Domain")]
		[UsedImplicitly]
		public ZDomainProperties ZDomain { get; private set; }

		[DisplayName("M Resolution")]
		[UsedImplicitly]
		public string MResolution { get; private set; }

		[DisplayName("M Tolerance")]
		[UsedImplicitly]
		public string MTolerance { get; private set; }

		[TypeConverter(typeof(AllPropertiesConverter))]
		[DisplayName("M Domain")]
		[UsedImplicitly]
		public MDomainProperties MDomain { get; private set; }

		[TypeConverter(typeof(AllPropertiesConverter))]
		[DisplayName("Coordinate System")]
		[UsedImplicitly]
		public CoordinateSystemProperties CoordinateSystem { get; private set; }

		public override string ToString()
		{
			return Name;
		}

		[NotNull]
		private static string Format(double value)
		{
			return StringUtils.FormatPreservingDecimalPlaces(
				value, CultureInfo.CurrentCulture);
		}

		[CanBeNull]
		private static CoordinateSystemProperties GetCoordinateSystemProperties(
			ISpatialReference spatialReference)
		{
			var projCs = spatialReference as IProjectedCoordinateSystem;
			if (projCs != null)
			{
				return new ProjectedCoordinateSystemProperties(projCs);
			}

			var geoCs = spatialReference as IGeographicCoordinateSystem;
			return geoCs != null
				       ? new GeographicCoordinateSystemProperties(geoCs)
				       : null;
		}
	}
}
