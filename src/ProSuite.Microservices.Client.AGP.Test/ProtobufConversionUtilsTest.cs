using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.Test
{
	[TestFixture]
	[Apartment(ApartmentState.STA)]
	public class ProtobufConversionUtilsTest
	{
		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			CoreHostProxy.Initialize();
		}

		[Test]
		public void CanConvertMultipatchToFromShapeMsg()
		{
			Multipatch multipatch = CreateMultipatch();

			ShapeMsg shapeMsg = ProtobufConversionUtils.ToShapeMsg(multipatch);

			Geometry rehydrated = ProtobufConversionUtils.FromShapeMsg(shapeMsg);

			Assert.IsTrue(GeometryEngine.Instance.Equals(multipatch, rehydrated));
			Assert.IsTrue(multipatch.IsEqual(rehydrated));
		}

		private static Multipatch CreateMultipatch()
		{
			MultipatchBuilderEx mpBuilder =
				new MultipatchBuilderEx(SpatialReferenceBuilder.CreateSpatialReference(2056));

			var patches = new List<Patch>();

			Patch patch1 = mpBuilder.MakePatch(esriPatchType.FirstRing);

			patch1.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600000, 1200000, 450),
					new Coordinate3D(2600000, 1200200, 450),
					new Coordinate3D(2600100, 1200200, 470),
					new Coordinate3D(2600100, 1200000, 470),
					new Coordinate3D(2600000, 1200000, 450)
				});

			patches.Add(patch1);

			Patch patch2 = mpBuilder.MakePatch(esriPatchType.Ring);

			patch2.Coords = new List<Coordinate3D>(
				new[]
				{
					new Coordinate3D(2600025, 1200050, 450),
					new Coordinate3D(2600050, 1200250, 450),
					new Coordinate3D(2600150, 1200275, 470),
					new Coordinate3D(2600125, 1200075, 470),
					new Coordinate3D(2600025, 1200050, 450)
				});

			patches.Add(patch2);

			mpBuilder.Patches = patches;

			Multipatch multipatch = mpBuilder.ToGeometry() as Multipatch;
			return multipatch;
		}
	}
}
