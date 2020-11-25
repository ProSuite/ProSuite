using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container.TestSupport;

namespace ProSuite.QA.Container.Test.TestSupport
{
	[TestFixture]
	public class MultiTableViewTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();
		private IFeatureWorkspace _testWs;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout();
			_testWs = TestWorkspaceUtils.CreateInMemoryWorkspace("MultiTableViewTest");
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanMatchRows()
		{
			const string textField1 = "Text1";
			const string textField2 = "Text2";
			const string textFieldBoth = "Text";
			IFeatureClass fc1 = CreateFeatureClass("CanMatchRows_fc1",
			                                       esriGeometryType.esriGeometryPolygon,
			                                       FieldUtils.CreateTextField(textField1, 1),
			                                       FieldUtils.CreateTextField(textFieldBoth, 1));
			IFeatureClass fc2 = CreateFeatureClass("CanMatchRows_fc2",
			                                       esriGeometryType.esriGeometryPolygon,
			                                       FieldUtils.CreateTextField(textFieldBoth, 1),
			                                       FieldUtils.CreateTextField(textField2, 1));

			const bool caseSensitive = true;
			MultiTableView view = TableViewFactory.Create(new[] {(ITable) fc1, (ITable) fc2},
			                                              new[] {"G1", "G2"},
			                                              "G1.TEXT1 = G2.TEXT2 AND G1.Text = 'x' AND G2.Text = 'y'",
			                                              caseSensitive);

			IFeature f1A = fc1.CreateFeature();
			f1A.Value[fc1.FindField(textField1)] = "A";
			f1A.Value[fc1.FindField(textFieldBoth)] = "x";
			f1A.Store();

			IFeature f2A = fc2.CreateFeature();
			f2A.Value[fc2.FindField(textField2)] = "A";
			f2A.Value[fc2.FindField(textFieldBoth)] = "y";
			f2A.Store();

			Assert.IsTrue(view.MatchesConstraint(f1A, f2A));
			Assert.AreEqual("G1.TEXT1 = 'A'; G2.TEXT2 = 'A'; G1.TEXT = 'x'; G2.TEXT = 'y'",
			                view.ToString(f1A, f2A));

			IFeature f1B = fc1.CreateFeature();
			f1B.Value[fc1.FindField(textField1)] = "b";
			f1B.Value[fc1.FindField(textFieldBoth)] = "x";
			f1B.Store();

			IFeature f2B = fc2.CreateFeature();
			f2B.Value[fc2.FindField(textField2)] = "B"; // different case --> no match
			f2B.Value[fc2.FindField(textFieldBoth)] = "y";
			f2B.Store();

			Assert.IsFalse(view.MatchesConstraint(f1B, f2B));
			Assert.AreEqual("G1.TEXT1 = 'b'; G2.TEXT2 = 'B'; G1.TEXT = 'x'; G2.TEXT = 'y'",
			                view.ToString(f1B, f2B));
		}

		[Test]
		public void CanUseShapeAreaAlias()
		{
			IFeatureClass fc1 = CreateFeatureClass("CanUseShapeAreaAlias_fc1",
			                                       esriGeometryType.esriGeometryPolygon);
			IFeatureClass fc2 = CreateFeatureClass("CanUseShapeAreaAlias_fc2",
			                                       esriGeometryType.esriGeometryPolygon);

			const bool caseSensitive = true;
			MultiTableView view = TableViewFactory.Create(new[] {(ITable) fc1, (ITable) fc2},
			                                              new[] {"G1", "G2"},
			                                              "G1.$ShapeArea < G2.$ShapeArea",
			                                              caseSensitive);

			IFeature f1A = fc1.CreateFeature();
			f1A.Shape = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			f1A.Store();

			IFeature f1B = fc1.CreateFeature();
			f1B.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			f1B.Store();

			IFeature f2A = fc2.CreateFeature();
			f2A.Shape = GeometryFactory.CreatePolygon(0, 0, 100, 100);
			f2A.Store();

			IFeature f2B = fc2.CreateFeature();
			f2B.Shape = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			f2B.Store();

			Assert.IsTrue(view.MatchesConstraint(f1A, f2A));
			Assert.AreEqual("G1.$SHAPEAREA = 100; G2.$SHAPEAREA = 10000",
			                view.ToString(f1A, f2A));

			Assert.IsFalse(view.MatchesConstraint(f1B, f2B));
			Assert.AreEqual("G1.$SHAPEAREA = 10000; G2.$SHAPEAREA = 100",
			                view.ToString(f1B, f2B));
		}

		[Test]
		public void CanUseShapeLengthAlias()
		{
			IFeatureClass fc1 = CreateFeatureClass("CanUseShapeLengthAlias_fc1",
			                                       esriGeometryType.esriGeometryPolygon);
			IFeatureClass fc2 = CreateFeatureClass("CanUseShapeLengthAlias_fc2",
			                                       esriGeometryType.esriGeometryPolygon);

			const bool caseSensitive = true;
			MultiTableView view = TableViewFactory.Create(new[] {(ITable) fc1, (ITable) fc2},
			                                              new[] {"G1", "G2"},
			                                              "G1.$ShapeLength < G2.$ShapeLength",
			                                              caseSensitive);

			IFeature f1A = fc1.CreateFeature();
			f1A.Shape = GeometryFactory.CreatePolygon(0, 0, 1, 1);
			f1A.Store();

			IFeature f1B = fc1.CreateFeature();
			f1B.Shape = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			f1B.Store();

			IFeature f2A = fc2.CreateFeature();
			f2A.Shape = GeometryFactory.CreatePolygon(0, 0, 10, 10);
			f2A.Store();

			IFeature f2B = fc2.CreateFeature();
			f2B.Shape = GeometryFactory.CreatePolygon(0, 0, 1, 1);
			f2B.Store();

			Assert.IsTrue(view.MatchesConstraint(f1A, f2A));
			Assert.AreEqual("G1.$SHAPELENGTH = 4; G2.$SHAPELENGTH = 40",
			                view.ToString(f1A, f2A));

			Assert.IsFalse(view.MatchesConstraint(f1B, f2B));
			Assert.AreEqual("G1.$SHAPELENGTH = 40; G2.$SHAPELENGTH = 4",
			                view.ToString(f1B, f2B));
		}

		[NotNull]
		private IFeatureClass CreateFeatureClass([NotNull] string name,
		                                         esriGeometryType geometryType,
		                                         params IField[] attributeFields)
		{
			ISpatialReference sref = SpatialReferenceUtils.CreateSpatialReference
			((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			 true);

			SpatialReferenceUtils.SetXYDomain(sref, -10000, -10000, 10000, 10000, 0.0001,
			                                  0.001);

			var fields = new List<IField>
			             {
				             FieldUtils.CreateOIDField(),
				             FieldUtils.CreateShapeField("Shape", geometryType, sref, 1000)
			             };
			fields.AddRange(attributeFields);

			return DatasetUtils.CreateSimpleFeatureClass(_testWs, name,
			                                             FieldUtils.CreateFields(fields));
		}
	}
}
