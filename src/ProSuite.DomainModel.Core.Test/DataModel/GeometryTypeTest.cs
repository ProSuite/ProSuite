using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Test.DataModel
{
	[TestFixture]
	public class GeometryTypeTest
	{
		[Test]
		public void CanCompareGeometryTypes()
		{
			var g1 = new GeometryTypeShape("Multipatch", ProSuiteGeometryType.MultiPatch);
			var g2 = new GeometryTypeShape("MultiPatch", ProSuiteGeometryType.MultiPatch);

			Assert.AreEqual(g1, g2);
		}

		[Test]
		public void CanCompareGeometries()
		{
			IList<GeometryType> geometryTypes =
				GeometryTypeFactory.CreateGeometryTypes();

			// Use the 'wrong' spelling:
			var multipatchType =
				new GeometryTypeShape("MultiPatch", ProSuiteGeometryType.MultiPatch);

			// The 'official' spelling
			// - used to be Multipatch in the legacy prosuite
			// - was MultiPatch until v. 1.2.1
			// - is now Multipatch again.
			Assert.IsTrue(geometryTypes.Contains(multipatchType));

			geometryTypes.Remove(multipatchType);

			List<GeometryType> missingTypes = GeometryTypeFactory
			                                  .GetMissingGeometryTypes(
				                                  geometryTypes, true)
			                                  .ToList();

			Assert.AreEqual(1, missingTypes.Count);

			Assert.IsTrue(missingTypes[0].Equals(multipatchType));
		}

		[Test]
		public void CanCloneGeometryTypes()
		{
			foreach (GeometryType geometryType in GeometryTypeFactory.CreateGeometryTypes())
			{
				GeometryType clone = geometryType.Clone();

				Assert.AreEqual(geometryType, clone);

				Assert.AreEqual(geometryType.Name, clone.Name);

				if (geometryType is GeometryTypeShape geometryTypeShape)
				{
					Assert.AreEqual(geometryTypeShape.ShapeType,
					                ((GeometryTypeShape) clone).ShapeType);
				}
			}
		}
	}
}
