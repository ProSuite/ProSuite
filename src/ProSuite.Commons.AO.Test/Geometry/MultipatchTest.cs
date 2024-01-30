using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;

namespace ProSuite.Commons.AO.Test.Geometry
{
	[TestFixture]
	public class MultipatchTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense(true);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		public void ValidateImportMpWithRandomPointIdsTest()
		{
			IFeatureWorkspace ws = TestWorkspaceUtils.CreateTestFgdbWorkspace("MultpatchTest");
			ISpatialReference lv95 =
				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95);

			IFeatureClass fc = DatasetUtils.CreateSimpleFeatureClass(
				ws, "MpFc",
				FieldUtils.CreateFields(
					FieldUtils.CreateOIDField(),
					FieldUtils.CreateShapeField(esriGeometryType.esriGeometryMultiPatch, lv95,
					                            hasZ: true)
				));

			IFeature f = fc.CreateFeature();
			f.Shape = GeometryUtils.FromXmlString(_mpErrorXml);

			// Stürzt ab in ArcObjects 10.8
			f.Store();
		}

		private const string _mpErrorXml = @"
<MultiPatchN xsi:type='typens:MultiPatchN'
             xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'
             xmlns:xs='http://www.w3.org/2001/XMLSchema'
             xmlns:typens='http://www.esri.com/schemas/ArcGIS/10.8'>
	<HasID>true</HasID>
	<HasZ>true</HasZ>
	<HasM>false</HasM>
	<Extent xsi:type='typens:EnvelopeN'>
		<XMin>2683950.6609999985</XMin>
		<YMin>1248138.5689999983</YMin>
		<XMax>2683953.5529999994</XMax>
		<YMax>1248146.5260000005</YMax>
		<ZMin>486.68499999999767</ZMin>
		<ZMax>486.68499999999767</ZMax>
	</Extent>
	<SurfacePatchArray xsi:type='typens:ArrayOfSurfacePatch'>
		<SurfacePatch xsi:type='typens:Ring'>
			<PointArray xsi:type='typens:ArrayOfPoint'>
				<Point xsi:type='typens:PointN'>
					<X>2683952.2419999987</X>
					<Y>1248146.2890000008</Y>
					<Z>486.68499999999767</Z>
					<ID>0</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683953.068</X>
					<Y>1248144.3779999986</Y>
					<Z>486.68499999999767</Z>
					<ID>0</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683953.5210000016</X>
					<Y>1248142.3850000016</Y>
					<Z>486.68499999999767</Z>
					<ID>0</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683953.5529999994</X>
					<Y>1248140.4400000013</Y>
					<Z>486.68499999999767</Z>
					<ID>-621</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683953.4389999993</X>
					<Y>1248138.881000001</Y>
					<Z>486.68499999999767</Z>
					<ID>1267346911</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683951.9730000012</X>
					<Y>1248138.5689999983</Y>
					<Z>486.68499999999767</Z>
					<ID>-1778353580</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683952.0430000015</X>
					<Y>1248140.6180000007</Y>
					<Z>486.68499999999767</Z>
					<ID>5570640</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683951.9479999989</X>
					<Y>1248142.449000001</Y>
					<Z>486.68499999999767</Z>
					<ID>4522066</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683951.5430000015</X>
					<Y>1248143.6970000006</Y>
					<Z>486.68499999999767</Z>
					<ID>4653151</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683950.6609999985</X>
					<Y>1248145.3029999994</Y>
					<Z>486.68499999999767</Z>
					<ID>4325445</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683952.0309999995</X>
					<Y>1248146.5260000005</Y>
					<Z>486.68499999999767</Z>
					<ID>5242975</ID>
				</Point>
				<Point xsi:type='typens:PointN'>
					<X>2683952.2419999987</X>
					<Y>1248146.2890000008</Y>
					<Z>486.68499999999767</Z>
					<ID>5505102</ID>
				</Point>
			</PointArray>
		</SurfacePatch>
	</SurfacePatchArray>
	<SpatialReference xsi:type='typens:ProjectedCoordinateSystem'>
		<WKT>PROJCS[&quot;CH1903+_LV95&quot;,GEOGCS[&quot;GCS_CH1903+&quot;,DATUM[&quot;D_CH1903+&quot;,SPHEROID[&quot;Bessel_1841&quot;,6377397.155,299.1528128]],PRIMEM[&quot;Greenwich&quot;,0.0],UNIT[&quot;Degree&quot;,0.0174532925199433]],PROJECTION[&quot;Hotine_Oblique_Mercator_Azimuth_Center&quot;],PARAMETER[&quot;False_Easting&quot;,2600000.0],PARAMETER[&quot;False_Northing&quot;,1200000.0],PARAMETER[&quot;Scale_Factor&quot;,1.0],PARAMETER[&quot;Azimuth&quot;,90.0],PARAMETER[&quot;Longitude_Of_Center&quot;,7.439583333333333],PARAMETER[&quot;Latitude_Of_Center&quot;,46.95240555555556],UNIT[&quot;Meter&quot;,1.0]],VERTCS[&quot;LHN95&quot;,VDATUM[&quot;Landeshohennetz_1995&quot;],PARAMETER[&quot;Vertical_Shift&quot;,0.0],PARAMETER[&quot;Direction&quot;,1.0],UNIT[&quot;Meter&quot;,1.0]]</WKT>
		<XOrigin>-27386400</XOrigin>
		<YOrigin>-32067900</YOrigin>
		<XYScale>1000</XYScale>
		<ZOrigin>-100000</ZOrigin>
		<ZScale>1000</ZScale>
		<MOrigin>-100000</MOrigin>
		<MScale>10000</MScale>
		<XYTolerance>0.01</XYTolerance>
		<ZTolerance>0.01</ZTolerance>
		<MTolerance>0.001</MTolerance>
		<HighPrecision>true</HighPrecision>
		<WKID>2056</WKID>
		<LatestWKID>2056</LatestWKID>
		<VCSWKID>5729</VCSWKID>
		<LatestVCSWKID>5729</LatestVCSWKID>
	</SpatialReference>
</MultiPatchN>";
	}
}
