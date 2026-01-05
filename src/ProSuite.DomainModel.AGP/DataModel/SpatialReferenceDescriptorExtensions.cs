using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AGP.DataModel;

public static class SpatialReferenceDescriptorExtensions
{
	public static SpatialReferenceDescriptor CreateFrom(SpatialReference sref)
	{
		if (sref is null) return null;

		string xml = sref.ToXml();

		return new SpatialReferenceDescriptor(sref.Name, xml);
	}

	[NotNull]
	public static SpatialReference GetSpatialReference(this SpatialReferenceDescriptor descriptor)
	{
		if (! (descriptor.SpatialReferenceCache is SpatialReference sref))
		{
			sref = CreateSpatialReference(descriptor.XmlString);
			descriptor.SpatialReferenceCache = sref;
		}

		return sref;
	}

	#region Non-public members

	[NotNull]
	private static SpatialReference CreateSpatialReference(string xmlString)
	{
		if (string.IsNullOrEmpty(xmlString))
		{
			throw new InvalidConfigurationException(
				"Spatial reference xml string not defined");
		}

		return SpatialReferenceBuilder.FromXml(xmlString);
	}

	#endregion
}
