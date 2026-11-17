using System;
using System.Collections.Generic;
using System.IO;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.Test.DataModel
{
	[TestFixture]
	public class ModelHarvestingTest
	{
		[OneTimeSetUp]
		public void SetupFixture()
		{
			TestUtils.InitializeLicense();
		}

		[OneTimeTearDown]
		public void TeardownFixture()
		{
			TestUtils.ReleaseLicense();
		}

		/// <summary>
		/// A model is harvested with qualified names, then the "harvest qualified element names"
		/// option is switched off and the model is harvested again. The existing datasets must be
		/// renamed in place (and not be replaced by new datasets), otherwise the quality
		/// conditions that reference them would lose their datasets.
		/// </summary>
		[Test]
		public void CanSwitchFromQualifiedToUnqualifiedNames()
		{
			IFeatureWorkspace workspace = OpenMobileGdbCopy();

			var model = new TestModel("harvest test") { HarvestQualifiedElementNames = true };

			// Not part of the geodatabase: it will be registered as deleted
			Dataset deletedDataset = model.AddDataset(new TestVectorDataset("main.gone"));

			using (var context = new MasterDatabaseWorkspaceContext(workspace, model, false))
			{
				model.WorkspaceContext = context;

				Harvest(model, context);

				Assert.IsTrue(model.ElementNamesAreQualified);

				// For a mobile geodatabase, ArcObjects reports "main" as the database name, not
				// as the schema owner. An SQL Server geodatabase reports both.
				Assert.AreEqual("main", model.DefaultDatabaseName);

				Dataset lines = model.GetDatasetByModelName("main.lines");
				Dataset points = model.GetDatasetByModelName("main.points");
				Dataset polygons = model.GetDatasetByModelName("main.polygons");
				Assert.NotNull(lines);
				Assert.NotNull(points);
				Assert.NotNull(polygons);
				Assert.IsTrue(deletedDataset.Deleted);

				int datasetCountBefore = model.Datasets.Count;

				model.HarvestQualifiedElementNames = false;

				Harvest(model, context);

				Assert.IsFalse(model.ElementNamesAreQualified);
				Assert.AreEqual("main", model.DefaultDatabaseName);

				// The same dataset instances, renamed, and still existing
				Assert.AreEqual(datasetCountBefore, model.Datasets.Count);
				Assert.AreSame(lines, model.GetDatasetByModelName("lines"));
				Assert.AreSame(points, model.GetDatasetByModelName("points"));
				Assert.AreSame(polygons, model.GetDatasetByModelName("polygons"));
				Assert.IsFalse(lines.Deleted);
				Assert.IsFalse(points.Deleted);
				Assert.IsFalse(polygons.Deleted);
				Assert.IsNull(model.GetDatasetByModelName("main.lines"));

				// The renamed dataset can still be opened in the master database
				IObjectClass objectClass = context.OpenObjectClass((IObjectDataset) lines);
				Assert.AreEqual("main.lines", DatasetUtils.GetName(objectClass));

				// A dataset that was registered as deleted keeps its qualified name
				Assert.AreEqual("main.gone", deletedDataset.Name);
				Assert.IsTrue(deletedDataset.Deleted);

				foreach (Dataset dataset in model.Datasets)
				{
					Console.WriteLine(@"{0} (deleted: {1})", dataset.Name, dataset.Deleted);
				}
			}
		}

		private static void Harvest([NotNull] DdxModel model,
		                            [NotNull] IWorkspaceContext context)
		{
			var factory = new TestDatasetListBuilderFactory
			              {
				              GeometryTypes = new List<GeometryType>
				                              {
					                              new GeometryTypeShape(
						                              "Point", ProSuiteGeometryType.Point),
					                              new GeometryTypeShape(
						                              "Polyline", ProSuiteGeometryType.Polyline),
					                              new GeometryTypeShape(
						                              "Polygon", ProSuiteGeometryType.Polygon)
				                              }
			              };

			ModelHarvesting.Harvest(model, context, factory, (IAttributeConfigurator) null);
		}

		[NotNull]
		private static IFeatureWorkspace OpenMobileGdbCopy()
		{
			var locator = new TestDataLocator(typeof(TestUtils).Assembly,
			                                  @"..\..\ProSuite\src", "TestData");
			string sourcePath = locator.GetPath("mobile.geodatabase");

			string directory = Path.Combine(Path.GetTempPath(),
			                                $"{nameof(ModelHarvestingTest)}_{Guid.NewGuid():N}");
			Directory.CreateDirectory(directory);

			string copyPath = Path.Combine(directory, "mobile.geodatabase");
			File.Copy(sourcePath, copyPath);

			return WorkspaceUtils.OpenMobileGdbFeatureWorkspace(copyPath);
		}

		private class TestModel : ProductionModel, IModelMasterDatabase
		{
			public TestModel([NotNull] string name) : base(name) { }

			public IWorkspaceContext WorkspaceContext { private get; set; }

			public override string QualifyModelElementName(string modelElementName)
			{
				return ModelUtils.QualifyModelElementName(this, modelElementName);
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return ModelUtils.TranslateToModelElementName(this, masterDatabaseDatasetName);
			}

			IWorkspaceContext IModelMasterDatabase.CreateMasterDatabaseWorkspaceContext()
			{
				return WorkspaceContext;
			}
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset([NotNull] string name) : base(name) { }
		}

		private class TestDatasetListBuilderFactory : IDatasetListBuilderFactory
		{
			public IList<GeometryType> GeometryTypes { get; set; }

			public IDatasetListBuilder Create(string modelSchemaOwner,
			                                  string modelDatasetPrefix,
			                                  bool ignoreUnversionedDatasets,
			                                  bool ignoreUnregisteredTables,
			                                  bool unqualifyDatasetNames,
			                                  IDatasetFilter datasetFilter)
			{
				return new TestDatasetListBuilder(unqualifyDatasetNames);
			}
		}

		/// <summary>
		/// Uses every feature class of the workspace.
		/// </summary>
		private class TestDatasetListBuilder : IDatasetListBuilder
		{
			private readonly bool _unqualifyDatasetNames;
			private readonly List<Dataset> _datasets = new List<Dataset>();

			public TestDatasetListBuilder(bool unqualifyDatasetNames)
			{
				_unqualifyDatasetNames = unqualifyDatasetNames;
			}

			public bool IgnoreDataset(IDatasetName datasetName, out string reason)
			{
				bool ignore = datasetName.Type != esriDatasetType.esriDTFeatureClass;

				reason = ignore ? "not a feature class" : string.Empty;
				return ignore;
			}

			public void UseDataset(IDatasetName name)
			{
				string datasetName = _unqualifyDatasetNames
					                     ? ModelElementNameUtils.GetUnqualifiedName(name.Name)
					                     : name.Name;

				_datasets.Add(new TestVectorDataset(datasetName));
			}

			public void AddDatasets(DdxModel model)
			{
				foreach (Dataset dataset in _datasets)
				{
					model.AddDataset(dataset);
				}
			}
		}
	}
}
