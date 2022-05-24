using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.DomainServices.AO.QA;
using ProSuite.QA.Tests;

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
		public void GetParallelableTests()
		{
			List<Assembly> assemblies = new List<Assembly> { typeof(QaMinArea).Assembly };

			List<Type> parallelable = new List<Type>();
			List<Type> nonParallelable = new List<Type>();
			foreach (Assembly assembly in assemblies)
			{
				foreach (Type type in assembly.GetTypes())
				{
					if (type.IsAbstract)
					{
						continue;
					}

					if (! typeof(ITest).IsAssignableFrom(type))
					{
						continue;
					}

					bool p = TestAssembler.CanBeExecutedWithTileThreads(type);

					if (p)
					{
						parallelable.Add(type);
					}
					else
					{
						nonParallelable.Add(type);
					}
				}
			}

			Assert.IsTrue(parallelable.Count > 0);
			Assert.IsTrue(nonParallelable.Count > 0);
		}

		[Test]
		public void CanRunParallel()
		{
			Trace.WriteLine($"Executing {nameof(CanRunParallel)}");
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("ParallelTest");
			string fcName = "Border";
			IFeatureClass linesFc = CreateFeatureClass(
				workspace, fcName, esriGeometryType.esriGeometryPoint,
				customFields: new[] { FieldUtils.CreateIntegerField(_waitFieldName) });

			int iWait = linesFc.FindField(_waitFieldName);
			//ReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(linesFc);
			int maxX = 5;
			int maxY = 5;

			List<TileParam> tileParsList = new List<TileParam>();

			double dx = 10000;
			for (int ix = 0; ix < maxX; ix++)
			{
				for (int iy = 0; iy < maxY; iy++)
				{
					IFeature f = linesFc.CreateFeature();
					f.Value[iWait] = (maxX - ix) * 1000 + (maxY - iy) * 100;
					double x = 2600000 + ix * dx;
					double y = 1200000 + iy * dx;
					f.Shape = GeometryFactory.CreatePoint(x, y);
					f.Store();

					TileParam t = new TileParam
					              {
						              Box = new WKSEnvelope
						                    {
							                    XMin = x - dx / 2,
							                    XMax = x + dx / 2,
							                    YMin = y - dx / 2,
							                    YMax = y + dx / 2
						                    }
					              };
					tileParsList.Add(t);
				}
			}

			_tileParams = tileParsList;
			string wsPath = WorkspaceUtils.TryGetCatalogPath((IWorkspace) workspace);
			Assert.NotNull(wsPath);

			RunParallel(
				tileParsList, dx,
				() =>
				{
					IFeatureWorkspace ws = WorkspaceUtils.OpenFeatureWorkspace(wsPath);
					IFeatureClass fc = ws.OpenFeatureClass(fcName);
					ReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
					WaitTest wt = new WaitTest(roFc);

					return new List<ContainerTest> { wt };
				});
		}

		[Test]
		public void CanFilterParallel()
		{
			IFeatureWorkspace workspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("CanFilterParallel");
			string fcName = "Border";
			IFeatureClass linesFc = CreateFeatureClass(
				workspace, fcName, esriGeometryType.esriGeometryPolyline);

			//ReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(linesFc);
			int maxX = 5;
			int maxY = 5;

			List<TileParam> tilePars = new List<TileParam>();

			double dx = 10000;
			IPolyline p = new PolylineClass();
			for (int ix = 0; ix < maxX; ix++)
			{
				for (int iy = 0; iy < maxY; iy++)
				{
					double x = 2600000 + ix * dx;
					double y = 1200000 + iy * dx;
					((IPointCollection) p).AddPoint(new PointClass { X = x, Y = y });
					TileParam t = new TileParam
					              {
						              Box = new WKSEnvelope
						                    {
							                    XMin = x - dx / 2,
							                    XMax = x + dx / 2,
							                    YMin = y - dx / 2,
							                    YMax = y + dx / 2
						                    }
					              };
					tilePars.Add(t);
				}
			}

			IFeature f = linesFc.CreateFeature();
			f.Shape = p;
			f.Store();

			_tileParams = tilePars;
			string wsPath = WorkspaceUtils.TryGetCatalogPath((IWorkspace) workspace);
			Assert.NotNull(wsPath);

			_errors.Clear();
			_unfilteredErrors.Clear();
			RunParallel(
				tilePars, dx,
				() =>
				{
					IFeatureWorkspace ws = WorkspaceUtils.OpenFeatureWorkspace(wsPath);
					IFeatureClass fc = ws.OpenFeatureClass(fcName);
					ReadOnlyFeatureClass roFc = ReadOnlyTableFactory.Create(fc);
					AllErrorsTest wt = new AllErrorsTest(roFc);

					QaMaxLength ml = new QaMaxLength(roFc, 10000);
					QaMinSegAngle ma = new QaMinSegAngle(roFc, 0.6);

					return new List<ContainerTest> { wt, ml, ma };
				});
			int expected = 1 + 1 + (2 * (maxY - 1));
			Assert.AreEqual(expected, _errors.Count);
			Assert.AreEqual(2 * tilePars.Count * expected, _unfilteredErrors.Count);
		}

		private void RunParallel(List<TileParam> tilePars, double dx,
		                         Func<List<ContainerTest>> getTests,
		                         bool runWithMaxTasks = true, bool runTilesParallel = true)
		{
			_tileParams = tilePars;

			if (runWithMaxTasks)
			{
				tilePars.ForEach(p => p.State = 0);
				int nTasks = 8;
				List<Task> tasks = new List<Task>();
				for (int iTask = 0; iTask < nTasks; iTask++)
				{
					Task t = Task.Run(() =>
					{
						int i = Task.CurrentId ?? -1;
						Trace.WriteLine($"Started Task {i}");

						List<ContainerTest> tests = getTests();

						while (true)
						{
							TileParam tp = GetUnhandledTileParam();
							if (tp == null)
							{
								break;
							}

							RunTile(tests, tp, dx);
						}

						Trace.WriteLine($"Ended Task {i}");
					});
					tasks.Add(t);
				}

				Task completed = Task.WhenAll(tasks);
				completed.Wait();

				foreach (TileParam tileParam in tilePars)
				{
					Assert.AreEqual(3, tileParam.State);
				}
			}

			if (runTilesParallel)
			{
				tilePars.ForEach(p => p.State = 0);
				int iTask = 0;
				List<Task> tasks = new List<Task>();
				foreach (TileParam tileParam in tilePars)
				{
					int i = iTask++;
					TileParam tp = tileParam;
					Task t = Task.Run(() =>
					{
						Trace.WriteLine($"Started Task {i}");

						List<ContainerTest> tests = getTests();
						RunTile(tests, tp, dx);

						Trace.WriteLine($"Ended Task {i}");
					});
					tasks.Add(t);
				}

				Task completed = Task.WhenAll(tasks);
				completed.Wait();

				foreach (TileParam tileParam in tilePars)
				{
					Assert.AreEqual(3, tileParam.State);
				}
			}
		}

		private void RunTile(IEnumerable<ContainerTest> tests, TileParam tp, double dx)
		{
			Trace.WriteLine(
				$"Handling tile {tp.Box.XMin}, {tp.Box.YMin} in task {Task.CurrentId}");

			tp.State = 2;
			// Test
			Dictionary<ContainerTest, int> testDict = new Dictionary<ContainerTest, int>();
			var container = new TestContainer.TestContainer { TileSize = dx };
			int idxTest = 0;
			foreach (var test in tests)
			{
				container.AddTest(test);
				testDict.Add(test, idxTest);
				idxTest++;
			}

			container.QaError += (s, e) =>
			{
				if (e.Cancel)
				{
					return;
				}

				ContainerTest test = (ContainerTest) e.QaError.Test;
				string serGeom = SerializeGeometry(e.QaError.Geometry);
				e.QaError.Geometry.Envelope.QueryWKSCoords(out WKSEnvelope errorExtent);
				IEnvelope involvedEnv = null;
				foreach (IReadOnlyRow row in e.TestedRows)
				{
					if (row is IReadOnlyFeature f)
					{
						if (involvedEnv == null)
						{
							involvedEnv = GeometryFactory.Clone(f.Shape.Envelope);
						}
						else
						{
							involvedEnv.Union(f.Shape.Envelope);
						}
					}
				}

				WKSEnvelope involvedExtent = new WKSEnvelope();
				if (involvedEnv != null)
				{
					involvedEnv.QueryWKSCoords(out involvedExtent);
				}
				else
				{
					involvedExtent = errorExtent;
				}

				QaSer qaSer = new QaSer
				              {
					              TestIndex = testDict[test],
					              Error = serGeom,
					              ErrorExtent = errorExtent,
					              InvolvedExtent = involvedExtent
				              };
				AddError(qaSer, tp);
			};
			container.Execute(GeometryFactory.CreateEnvelope(tp.Box));

			tp.State = 3;
		}

		private string SerializeGeometry(IGeometry geom)
		{
			string sGeom = GetJson(geom); // GeometryUtils.ToString(geom);
			return sGeom;
		}

		public static string GetJson(IGeometry geometry, int precision = 3)
		{
			IJSONWriter jsonWriter = new JSONWriterClass();
			jsonWriter.WriteToString();

			IPropertySet propertySet = new PropertySetClass();

			//IGeometryResultOptions geometryResultOptions = new GeometryResultOptionsClass();
			//geometryResultOptions.GeneralizeGeometries = true;
			//geometryResultOptions.DensifyGeometries = true;
			//propertySet.SetProperty("GeometryResultOptions", geometryResultOptions);

			if (precision >= 0)
			{
				propertySet.SetProperty("GeometryPrecision", precision);
			}

			IJSONSerializer jsonSerializer = new JSONSerializerGdbClass();
			jsonSerializer.InitSerializer(jsonWriter, propertySet);
			((IExternalSerializerGdb) jsonSerializer).WriteGeometry(null, geometry);

			string json = Encoding.UTF8.GetString(jsonWriter.GetStringBuffer());
			return json;
		}

		public static string GetJson(IFeature feature)
		{
			IJSONWriter jsonWriter = new JSONWriterClass();
			jsonWriter.WriteToString();

			IJSONSerializer jsonSerializer = new JSONSerializerGdbClass();
			jsonSerializer.InitSerializer(jsonWriter, null); // <--
			IGeometryResultOptions opts = new GeometryResultOptionsClass();

			int nFields = feature.Fields.FieldCount;
			List<int> fields = new List<int>(nFields);
			for (int iField = 0; iField < nFields; iField++)
			{
				fields.Add(iField);
			}

			((IExternalSerializerGdb) jsonSerializer).WriteRow(
				null, feature, feature.Fields, fields.ToArray(), opts);
			string json = Encoding.UTF8.GetString(jsonWriter.GetStringBuffer());
			return json;
		}

		private List<TileParam> _tileParams;
		private readonly object _tileParamsLock = new object();

		private readonly HashSet<QaSer> _errors = new HashSet<QaSer>(new QaSerEqual());
		private readonly List<QaSer> _unfilteredErrors = new List<QaSer>();
		private readonly object _errorsLock = new object();

		private void AddError(QaSer serGeom, TileParam tp)
		{
			AccessErrors(() =>
			{
				_errors.Add(serGeom);
				_unfilteredErrors.Add(serGeom);
			});
		}

		private void AccessErrors(Action accessAction)
		{
			lock (_errorsLock)
			{
				accessAction();
			}
		}

		private void RemoveHandledErrors()
		{
			WKSPoint handled = new WKSPoint { X = double.MaxValue, Y = double.MaxValue };
			foreach (TileParam tileParam in _tileParams)
			{
				if (tileParam.State != 3)
				{
					handled.Y = Math.Min(handled.Y, tileParam.Box.YMax);
				}
			}

			foreach (TileParam tileParam in _tileParams)
			{
				if (tileParam.Box.YMax <= handled.Y && tileParam.State != 3)
				{
					handled.X = Math.Min(handled.X, tileParam.Box.XMax);
				}
			}

			List<QaSer> toRemove = new List<QaSer>();
			AccessErrors(() =>
			{
				//foreach (string error in _errors)
				//{
				//	toRemove.Add(error);
				//}
			});
		}

		private TileParam GetUnhandledTileParam()
		{
			Task.Run(() => RemoveHandledErrors());
			lock (_tileParamsLock)
			{
				foreach (TileParam tileParam in _tileParams)
				{
					if (tileParam.State == 0)
					{
						tileParam.State = 1;
						return tileParam;
					}
				}
			}

			return null;
		}

		private class QaSerEqual : IEqualityComparer<QaSer>
		{
			public bool Equals(QaSer x, QaSer y)
			{
				if (x == y) return true;
				if (x == null || y == null) return false;
				return x.TestIndex == y.TestIndex &&
				       x.Error == y.Error;
			}

			public int GetHashCode(QaSer obj)
			{
				return obj.TestIndex.GetHashCode() + 7 * obj.Error.GetHashCode();
			}
		}

		private class QaSer
		{
			public int TestIndex { get; set; }
			public string Error { get; set; }
			public WKSEnvelope ErrorExtent { get; set; }
			public WKSEnvelope InvolvedExtent { get; set; }
		}

		private class TileParam
		{
			public WKSEnvelope Box { get; set; }
			public int State { get; set; }
		}

		private static IFeatureClass CreateFeatureClass(
			IFeatureWorkspace workspace, string name, esriGeometryType type,
			int spatialReferenceId = (int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
			IEnumerable<IField> customFields = null)
		{
			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				spatialReferenceId, setDefaultXyDomain: true);

			List<IField> fields = new List<IField>();
			fields.Add(FieldUtils.CreateOIDField());
			if (customFields != null)
			{
				fields.AddRange(customFields);
			}

			fields.Add(FieldUtils.CreateShapeField("Shape", type, sr, 1000));
			return DatasetUtils.CreateSimpleFeatureClass(
				workspace, name, FieldUtils.CreateFields(fields));
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
				int taskNr = Task.CurrentId ?? 0;
				Trace.WriteLine($"Executing oid {row.OID} in task {taskNr}");
				if (row.get_Value(_iWait) is int w)
				{
					Trace.WriteLine($"Waiting for {w} milliseconds in task {taskNr}");
					Thread.Sleep(w);
				}

				Trace.WriteLine($"Tested oid {row.OID} in task {taskNr}");
				return 0;
			}
		}

		private class AllErrorsTest : ContainerTest
		{
			public AllErrorsTest(IReadOnlyTable table)
				: base(table) { }

			protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
			{
				if (! (row is IReadOnlyFeature f))
				{
					return 0;
				}

				ReportError("error", GeometryFactory.Clone(f.Shape), null, null, values: null, row);
				return 1;
			}
		}
	}
}
