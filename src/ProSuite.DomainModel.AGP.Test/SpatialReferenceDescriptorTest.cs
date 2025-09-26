using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.DomainModel.AGP.DataModel;

namespace ProSuite.DomainModel.AGP.Test;

[TestFixture]
public class SpatialReferenceDescriptorTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();
	}

	[Test]
	public void CanCreateFrom()
	{
		var sref = SpatialReferenceBuilder.CreateSpatialReference(2056);

		var descriptor = SpatialReferenceDescriptorExtensions.CreateFrom(sref);

		Assert.NotNull(descriptor);
		Assert.AreEqual(sref.Name, descriptor.Name);
		Assert.NotNull(descriptor.XmlString);
	}

	[Test]
	public void CanGetSpatialReference()
	{
		var lv95 = SpatialReferenceBuilder.CreateSpatialReference(2056);
		var descriptor = SpatialReferenceDescriptorExtensions.CreateFrom(lv95);

		var sref1 = descriptor.GetSpatialReference();

		Assert.NotNull(sref1);
		Assert.AreEqual(2056, sref1.Wkid);
		SpatialReference.AreEqual(lv95, sref1, ignoreUnknown: false, checkResolution: true);

		var sref2 = descriptor.GetSpatialReference();

		// 2nd time it should return the cached instance:

		Assert.AreSame(sref1, sref2);
	}
}
