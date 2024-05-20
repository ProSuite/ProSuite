using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using Grpc.Core;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA.Test;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO;
using ProSuite.Microservices.Server.AO.QualityTestService;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.External;
using ProSuite.QA.Tests.Test.Construction;
using ProSuite.QA.Tests.Test.TestRunners;
using TestUtils = ProSuite.Commons.AO.Test.TestUtils;

namespace ProSuite.QA.Tests.Test
{
	[TestFixture]
	public class QaExternalServiceTest
	{
		const string Localhost = "localhost";
		const int Port = 5181;

		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();

			// Start the server:
			StartServer(Localhost, Port);
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		[Test]
		[Category(TestCategory.Sde)]
		public void TestSdeWorkspace()
		{
			var workspace = TestUtils.OpenUserWorkspaceOracle();

			IFeatureClass fcStr =
				DatasetUtils.OpenFeatureClass(workspace, "TOPGIS_TLM.TLM_STRASSE");
			IFeatureClass fcBahn =
				DatasetUtils.OpenFeatureClass(workspace, "TOPGIS_TLM.TLM_EISENBAHN");

			ISpatialReference sr = DatasetUtils.GetSpatialReference(fcStr);

			IEnvelope envelope = GeometryFactory.CreateEnvelope(
				2600000, 1200000, 2601000, 1201000, sr);

			string connectionUrl = $"http://{Localhost}:{Port}";

			List<IReadOnlyTable> tables = new[]
			                              {
				                              ReadOnlyTableFactory.Create(fcBahn),
				                              ReadOnlyTableFactory.Create(fcStr)
			                              }.Cast<IReadOnlyTable>().ToList();

			var test = new QaExternalService(tables, connectionUrl, string.Empty);

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute(envelope);

				Assert.Greater(testRunner.Errors.Count, 0);
			}
		}

		[Test]
		public void TestWithShapefiles()
		{
			IFeatureWorkspace featureWorkspace =
				TestWorkspaceUtils.CreateTestShapefileWorkspace("TestExternalServiceFiles");

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			SpatialReferenceUtils.SetZDomain(sr, 0, 1000, 0.001, 0.002);

			IFeatureClass fcBahn = CreateFeatureClassBahn(featureWorkspace, sr);

			IFeatureClass fcStr = CreateFeatureClassStrasse(featureWorkspace, sr);

			string connectionUrl = $"http://{Localhost}:{Port}";

			List<IReadOnlyTable> tables = new[]
			                              {
				                              ReadOnlyTableFactory.Create(fcBahn),
				                              ReadOnlyTableFactory.Create(fcStr)
			                              }.Cast<IReadOnlyTable>().ToList();

			var test = new QaExternalService(tables, connectionUrl, string.Empty);

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute();
				Assert.AreEqual(3, testRunner.Errors.Count);
			}
		}

		[Test]
		public void TestWithFgdb()
		{
			IFeatureWorkspace featureWorkspace =
				TestWorkspaceUtils.CreateTestFgdbWorkspace("TestExternalServiceGdb");

			ISpatialReference sr = SpatialReferenceUtils.CreateSpatialReference(
				(int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95, true);

			SpatialReferenceUtils.SetZDomain(sr, 0, 1000, 0.001, 0.002);

			IFeatureClass fcBahn = CreateFeatureClassBahn(featureWorkspace, sr);

			IFeatureClass fcStr = CreateFeatureClassStrasse(featureWorkspace, sr);

			string connectionUrl = $"http://{Localhost}:{Port}";

			List<IReadOnlyTable> tables = new[]
			                              {
				                              ReadOnlyTableFactory.Create(fcBahn),
				                              ReadOnlyTableFactory.Create(fcStr)
			                              }.Cast<IReadOnlyTable>().ToList();

			var test = new QaExternalService(tables, connectionUrl, string.Empty);

			using (var testRunner = new QaTestRunner(test))
			{
				testRunner.Execute();
				Assert.AreEqual(3, testRunner.Errors.Count);

				QaError qaError = testRunner.Errors.First();

				Assert.Greater(qaError.InvolvedRows.Count, 0);

				Assert.AreEqual("Strasse", qaError.InvolvedRows[0].TableName);
			}
		}

		private static IFeatureClass CreateFeatureClassStrasse(IFeatureWorkspace featureWorkspace,
		                                                       ISpatialReference sr)
		{
			IFieldsEdit fieldsStr = new FieldsClass();
			fieldsStr.AddField(FieldUtils.CreateOIDField());
			fieldsStr.AddField(FieldUtils.CreateIntegerField("Dummy"));
			fieldsStr.AddField(FieldUtils.CreateIntegerField("Stufe"));
			fieldsStr.AddField(FieldUtils.CreateShapeField(
				                   "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                   1000, true, false));

			IFeatureClass fcStr =
				DatasetUtils.CreateSimpleFeatureClass(featureWorkspace, "Strasse", fieldsStr,
				                                      null);

			// make sure the table is known by the workspace
			((IWorkspaceEdit) featureWorkspace).StartEditing(false);
			((IWorkspaceEdit) featureWorkspace).StopEditing(true);

			int fieldIndexStrStufe = fcStr.FindField("Stufe");

			IFeature str1 = fcStr.CreateFeature();
			str1.set_Value(fieldIndexStrStufe, 0);
			str1.Shape = CurveConstruction.StartLine(150, 90, 101)
			                              .LineTo(150, 110, 101)
			                              .Curve;
			str1.Store();

			IFeature str2 = fcStr.CreateFeature();
			str2.set_Value(fieldIndexStrStufe, 2);
			str2.Shape = CurveConstruction.StartLine(180, 90, 99)
			                              .LineTo(180, 110, 99)
			                              .Curve;
			str2.Store();

			IFeature str3 = fcStr.CreateFeature();
			str3.set_Value(fieldIndexStrStufe, 0);
			str3.Shape = CurveConstruction.StartLine(120, 90, 101)
			                              .LineTo(120, 110, 101)
			                              .Curve;
			str3.Store();

			return fcStr;
		}

		private static IFeatureClass CreateFeatureClassBahn(IFeatureWorkspace featureWorkspace,
		                                                    ISpatialReference sr)
		{
			IFieldsEdit fieldsBahn = new FieldsClass();
			fieldsBahn.AddField(FieldUtils.CreateOIDField());
			fieldsBahn.AddField(FieldUtils.CreateIntegerField("Stufe"));
			fieldsBahn.AddField(FieldUtils.CreateShapeField(
				                    "Shape", esriGeometryType.esriGeometryPolyline, sr,
				                    1000, true, false));

			IFeatureClass fcBahn =
				DatasetUtils.CreateSimpleFeatureClass(featureWorkspace, "Bahn", fieldsBahn,
				                                      null);

			int fieldIndexBahnStufe = fcBahn.FindField("Stufe");

			IFeature bahn1 = fcBahn.CreateFeature();
			bahn1.set_Value(fieldIndexBahnStufe, 1);
			bahn1.Shape = CurveConstruction.StartLine(100, 100, 100)
			                               .LineTo(200, 100, 100)
			                               .Curve;
			bahn1.Store();
			return fcBahn;
		}

		private static void StartServer(string hostname, int port)
		{
			var testGrpc = new QualityTestIntersectImpl();

			var oneGb = (int) Math.Pow(1024, 3);

			IList<ChannelOption> channelOptions = GrpcServerUtils.CreateChannelOptions(oneGb);

			var server =
				new Server(channelOptions)
				{
					Services =
					{
						QualityTestGrpc.BindService(testGrpc)
					},
					Ports =
					{
						new ServerPort(hostname, port, ServerCredentials.Insecure)
					}
				};

			server.Start();
		}

		private class QualityTestIntersectImpl : QualityTestGrpcImpl
		{
			// This would normally come from a configuration:
			private readonly string _dbPassword = "unittest";

			protected override IEnumerable<ExecuteTestResponse> ExecuteTestCore(
				ExecuteTestRequest request,
				ConcurrentBag<DetectedIssueMsg> issueCollection,
				ITrackCancel trackCancel)
			{
				IGeometry aoi = ProtobufGeometryUtils.FromShapeMsg(request.Perimeter);

				var involvedDatasets = FromTestDatasetMsgs(request);

				List<string> filterExpressions =
					request.InvolvedTables.Select(it => it.FilterExpression).ToList();

				IList<IFeatureClass> featureClasses =
					involvedDatasets.Keys.Cast<IFeatureClass>().ToList();

				IQueryFilter filter0 = GetQueryFilter(aoi, featureClasses, filterExpressions, 0);

				var fc0Features = GdbQueryUtils.GetFeatures(featureClasses[0], filter0, false)
				                               .ToList();

				IGeometry fc0Geometry = GeometryUtils.UnionFeatures(fc0Features);

				IQueryFilter filter1 = GetQueryFilter(aoi, featureClasses, filterExpressions, 1);

				foreach (var feature1 in GdbQueryUtils.GetFeatures(featureClasses[1], filter1, true)
				        )
				{
					if (trackCancel != null && ! trackCancel.Continue())
					{
						yield break;
					}

					IGeometry shape1 = feature1.Shape;

					if (GeometryUtils.Disjoint(shape1, fc0Geometry))
					{
						continue;
					}

					IGeometry errorGeo = IntersectionUtils.Intersect(
						fc0Geometry, feature1.Shape, esriGeometryDimension.esriGeometry0Dimension);

					if (errorGeo.IsEmpty)
					{
						continue;
					}

					ShapeMsg errorShapeMsg =
						ProtobufGeometryUtils.ToShapeMsg(
							errorGeo, ShapeMsg.FormatOneofCase.EsriShape);

					DetectedIssueMsg issue = new DetectedIssueMsg()
					                         {
						                         Description = "Things intersect",
						                         IssueGeometry = errorShapeMsg,
						                         AffectedComponent = "GeometryComponent",
						                         IssueCodeDescription = "IssueCodeDesc",
						                         IssueCodeId = "23"
					                         };

					TestDatasetMsg involvedDataset = involvedDatasets[feature1.Table];

					var involvedObjectsMsg = new InvolvedObjectsMsg()
					                         {
						                         Dataset = involvedDataset.ClassDefinition
					                         };

					involvedObjectsMsg.ObjectIds.Add(feature1.OID);

					issue.InvolvedObjects.Add(involvedObjectsMsg);

					issueCollection.Add(issue);

					yield return new ExecuteTestResponse()
					             {
						             ServiceCallStatus = (int) ServiceCallStatus.Running
					             };
				}
			}

			private static IQueryFilter GetQueryFilter(
				[CanBeNull] IGeometry aoi,
				[NotNull] IList<IFeatureClass> featureClasses,
				IReadOnlyList<string> filterExpressions, int classIndex)
			{
				IQueryFilter filter;
				if (aoi != null)
				{
					filter = GdbQueryUtils.CreateSpatialFilter(featureClasses[classIndex], aoi);
				}
				else
				{
					filter = new QueryFilterClass();
				}

				filter.WhereClause = filterExpressions[classIndex];
				return filter;
			}

			protected override string GetPassword(string instance, string user)
			{
				return _dbPassword;
			}
		}
	}
}
