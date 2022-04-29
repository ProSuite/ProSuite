using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.QA.Container.Test
{
	[TestFixture]
	public class TestParallelTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void TestFixtureSetUp()
		{
			_lic.Checkout();
		}

		[OneTimeTearDown]
		public void TestFixtureTearDown()
		{
			_lic.Release();
		}

		[Test]
		public void CanRunParallel()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateInMemoryWorkspace("ParallelTest");
			IFeatureClass linesFc = CreateFeatureClass(workspace, "Border",
													   esriGeometryType.esriGeometryPoint);

			int iWait = linesFc.FindField(_waitFieldName);
			ReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(linesFc);
			int maxX = 5;
			int maxY = 5;
			for (int ix = 0; ix < maxX; ix++)
			{
				for (int iy = 0; iy < maxY; iy++)
				{
					IFeature f = linesFc.CreateFeature();
					f.Value[iWait] = (maxX - ix) * 1000 + (maxY - iy) * 100;
					f.Shape = GeometryFactory.CreatePoint(2600000 + ix * 10000,
					                                      1200000 + iy * 10000);
					f.Store();
				}
			}


			TestParallel p = new TestParallel();
			p.Execute(new List<ContainerTest> { new WaitTest(roFc) });
		}

		private static IFeatureClass CreateFeatureClass(IFeatureWorkspace workspace,
														string name,
														esriGeometryType type)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				(int)esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				setDefaultXyDomain: true);
			IFieldsEdit fields = new FieldsClass();
			fields.AddField(FieldUtils.CreateOIDField());
			fields.AddField(FieldUtils.CreateShapeField(
								"Shape", type, sr, 1000));

			return DatasetUtils.CreateSimpleFeatureClass(
				workspace, name, null,
				FieldUtils.CreateOIDField(),
				FieldUtils.CreateIntegerField(_waitFieldName),
				FieldUtils.CreateShapeField("Shape", type, sr, 1000));
		}

		private const string _waitFieldName = "Wait";

		private class WaitTest : ContainerTest
		{
			private readonly int _iWait;

			public WaitTest(IReadOnlyTable table)
				: base(table)
			{
				_iWait = table.FindField(_waitFieldName);
			}

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				if (row.get_Value(_iWait) is int w)
				{
					System.Threading.Thread.Sleep(w);

				}
				return 0;
			}
		}
	}
}
