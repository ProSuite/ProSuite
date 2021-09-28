using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.Xml;

namespace ProSuite.DomainServices.AO.QA.DatasetReports.Xml
{
	public static class XmlSpatialReferenceDescriptorUtils
	{
		[NotNull]
		public static XmlSpatialReferenceDescriptor CreateXmlSpatialReferenceDescriptor(
			[NotNull] IFeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			ISpatialReference spatialReference = ((IGeoDataset) featureClass).SpatialReference;

			return CreateXmlSpatialReferenceDescriptor(spatialReference);
		}

		[NotNull]
		public static XmlSpatialReferenceDescriptor CreateXmlSpatialReferenceDescriptor(
			[NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var controlPrecision = (IControlPrecision3) spatialReference;
			var tolerance = (ISpatialReferenceTolerance) spatialReference;
			var resolution = (ISpatialReferenceResolution) spatialReference;

			var result = new XmlSpatialReferenceDescriptor
			             {
				             WellKnownText =
					             SpatialReferenceUtils
						             .ExportToESRISpatialReference(spatialReference),
				             XyCoordinateSystem = spatialReference.Name,
				             IsHighPrecision = controlPrecision.IsHighPrecision
			             };

			if (spatialReference.HasXYPrecision())
			{
				result.XyResolution = resolution.XYResolution[true];
				result.XyTolerance = tolerance.XYTolerance;
				result.XyDomain = GetXyDomain(spatialReference);
			}

			if (spatialReference.HasZPrecision())
			{
				result.ZResolution = resolution.ZResolution[true];
				result.ZTolerance = tolerance.ZTolerance;
				result.ZDomain = GetZDomain(spatialReference);
			}

			if (spatialReference.HasMPrecision())
			{
				result.MResolution = resolution.MResolution;
				result.MTolerance = tolerance.MTolerance;
				result.MDomain = GetMDomain(spatialReference);
			}

			return result;
		}

		[NotNull]
		private static XmlRange GetZDomain([NotNull] ISpatialReference spatialReference)
		{
			double zMin;
			double zMax;
			spatialReference.GetZDomain(out zMin, out zMax);

			return new XmlRange {Min = zMin, Max = zMax};
		}

		[NotNull]
		private static XmlRange GetMDomain([NotNull] ISpatialReference spatialReference)
		{
			double mMin;
			double mMax;
			spatialReference.GetMDomain(out mMin, out mMax);

			return new XmlRange {Min = mMin, Max = mMax};
		}

		[NotNull]
		private static Xml2DEnvelope GetXyDomain(
			[NotNull] ISpatialReference spatialReference)
		{
			double xmin;
			double ymin;
			double xmax;
			double ymax;
			spatialReference.GetDomain(out xmin, out ymin, out xmax, out ymax);

			return new Xml2DEnvelope
			       {
				       XMin = xmin,
				       YMin = ymin,
				       XMax = xmax,
				       YMax = ymax
			       };
		}
	}
}
