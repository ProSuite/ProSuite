using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public static class SpatialReferenceDescriptorExtensions
	{
		[NotNull]
		public static SpatialReferenceDescriptor CreateFrom(
			[NotNull] ISpatialReference spatialReference)
		{
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			return new SpatialReferenceDescriptor(
				spatialReference.Name,
				SpatialReferenceUtils.ToXmlString(spatialReference));
		}

		[NotNull]
		public static ISpatialReference GetSpatialReference(
			this SpatialReferenceDescriptor spatialReferenceDescriptor)
		{
			if (! (spatialReferenceDescriptor.SpatialReferenceCache is ISpatialReference sref))
			{
				sref = CreateSpatialReference(spatialReferenceDescriptor.XmlString);
				spatialReferenceDescriptor.SpatialReferenceCache = sref;
			}

			return sref;
		}

		#region Non-public members

		[NotNull]
		private static ISpatialReference CreateSpatialReference(string xmlString)
		{
			if (string.IsNullOrEmpty(xmlString))
			{
				throw new InvalidConfigurationException(
					"Spatial reference xml string not defined");
			}

			return SpatialReferenceUtils.FromXmlString(xmlString);
		}

		#endregion
	}
}
