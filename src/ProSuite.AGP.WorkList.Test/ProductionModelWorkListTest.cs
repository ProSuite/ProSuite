using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using NUnit.Framework;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain.Persistence.Xml;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Testing;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Shared.AGP.WorkLists;

namespace ProSuite.AGP.WorkList.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class ProductionModelWorkListTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		Commons.Test.Testing.TestUtils.ConfigureUnitTestLogging("k2.logging.test.xml");
	}

	[Test]
	public void Can_instantiate_ProductionModelWorkList()
	{
		string filePath = TestDataPreparer.FromDirectory().GetPath("Error Work List.iwl");
		XmlWorkItemStateRepository stateRepository = CreateXmlStateRepository(filePath);

		// todo
		string gdbPath = @"C:\K2\WorkUnitData\DKM25\WU1184_Payerne\WU1184_Payerne.gdb";
		using Geodatabase geodatabase = WorkspaceUtils.OpenFileGeodatabase(gdbPath);

		DbStatusWorkItemRepository itemRepository =
			CreateWorkItemRepository(geodatabase, stateRepository, gdbPath);

		Geometry areaOfInterest = GetAreaOfInterest(geodatabase);

		ProductionModelIssueWorkList workList = CreateWorkList(itemRepository, areaOfInterest);

		List<IWorkItem> items = workList.Search(new QueryFilter()).ToList();
		Assert.AreEqual(1453, items.Count);
	}

	[Test]
	public void Can_filter_ProductionModelWorkList_by_ErrorType()
	{
		string filePath = TestDataPreparer.FromDirectory().GetPath("Error Work List.iwl");
		XmlWorkItemStateRepository stateRepository = CreateXmlStateRepository(filePath);

		// todo
		string gdbPath = @"C:\K2\WorkUnitData\DKM25\WU1184_Payerne\WU1184_Payerne.gdb";
		using Geodatabase geodatabase = WorkspaceUtils.OpenFileGeodatabase(gdbPath);

		DbStatusWorkItemRepository itemRepository =
			CreateWorkItemRepository(geodatabase, stateRepository, gdbPath);

		Geometry areaOfInterest = GetAreaOfInterest(geodatabase);

		ProductionModelIssueWorkList workList = CreateWorkList(itemRepository, areaOfInterest);

		List<IWorkItem> items = workList.Search(new QueryFilter()).ToList();
		Assert.AreEqual(1453, items.Count);

		Geodatabase gdb = geodatabase;
		Assert.Throws<ArgumentException>(() => geodatabase.ApplyEdits(() =>
		{
			var random = new Random();
			long oid = random.NextInt64(maxValue: 1468);

			using var errorsFeatureClass =
				DatasetUtils.OpenDataset<FeatureClass>(gdb, "DKM25_ERRORS_POLYGON");
			using var definition = errorsFeatureClass.GetDefinition();
			int index = definition.FindField("FEHLERART");

			Feature randomFeature =
				GdbQueryUtils.GetFeatures(errorsFeatureClass, new List<long> { oid }, null, false)
				             .FirstOrDefault();
			Assert.NotNull(randomFeature);

			randomFeature[index] = ErrorType.Allowed;
			randomFeature.Store();

			workList.LoadItems();
			items = workList.Search(new QueryFilter()).ToList();

			Assert.AreEqual(1452, items.Count);

			throw new ArgumentException("roll back edits");
		}));
	}

	// NOTE: If FEHLERART returns Null or DBNull there is no work item created because of the
	//		 default definition query "FEHLERART < 300".
	[Test]
	public void LearningTest_ErrorType_is_null()
	{
		string filePath = TestDataPreparer.FromDirectory().GetPath("errors.iwl");
		XmlWorkItemStateRepository stateRepository = CreateXmlStateRepository(filePath);

		// todo
		string path = TestDataPreparer.ExtractZip("errors.gdb.zip").Overwrite().GetPath();
		using Geodatabase geodatabase = WorkspaceUtils.OpenFileGeodatabase(path);

		DbStatusWorkItemRepository itemRepository =
			CreateWorkItemRepository(geodatabase, stateRepository, path);

		Geometry areaOfInterest = GetAreaOfInterest(geodatabase);

		ProductionModelIssueWorkList workList = CreateWorkList(itemRepository, areaOfInterest);

		List<IWorkItem> items = workList.Search(new QueryFilter()).ToList();
		Assert.AreEqual(9, items.Count);

		using var errorsFeatureClass =
			DatasetUtils.OpenDataset<FeatureClass>(geodatabase, "DKM25_ERRORS_POLYGON");
		using var definition = errorsFeatureClass.GetDefinition();
		int index = definition.FindField("FEHLERART");

		var random = new Random();
		long oid = random.NextInt64(minValue: 1, maxValue: 10);

		Debug.WriteLine($"random OID {oid}");

		Feature randomFeature =
			GdbQueryUtils.GetFeatures(errorsFeatureClass, new List<long> { oid }, null, false)
			             .FirstOrDefault();
		Assert.NotNull(randomFeature);

		Assert.Throws<ArgumentException>(() => geodatabase.ApplyEdits(() =>
		{
			object value = randomFeature[index];
			Assert.NotNull(value);
			Assert.AreEqual(ErrorType.Hard, (ErrorType) value);

			randomFeature[index] = null;
			randomFeature.Store();

			Assert.Null(randomFeature[index]);

			workList.LoadItems();
			items = workList.Search(new QueryFilter()).ToList();
			Assert.AreEqual(8, items.Count);

			throw new ArgumentException("roll back edits");
		}));
	}

	private static ProductionModelIssueWorkList CreateWorkList(
		DbStatusWorkItemRepository itemRepository,
		Geometry areaOfInterest)
	{
		var workList =
			new ProductionModelIssueWorkList(itemRepository, areaOfInterest, "name", "displayName");
		workList.ExtentProvider = new ExtentProviderMock
		                          {
			                          Extent = areaOfInterest.Extent
		                          };
		workList.LoadItems();
		return workList;
	}

	private static Geometry GetAreaOfInterest(Geodatabase geodatabase)
	{
		using var fc = DatasetUtils.OpenDataset<FeatureClass>(geodatabase, "DKM25_WU_PERIMETER");

		return fc.GetFeature(1).GetShape();
	}

	private static DbStatusWorkItemRepository CreateWorkItemRepository(
		Geodatabase geodatabase, XmlWorkItemStateRepository stateRepository, string gdbPath)
	{
		using var errorsFeatureClass =
			DatasetUtils.OpenDataset<FeatureClass>(geodatabase, "DKM25_ERRORS_POLYGON");

		FeatureClassDefinition tableDefinition = errorsFeatureClass.GetDefinition();

		string statusField = "STATUS";
		string errorTypeFieldName = "FEHLERART";
		string shapeField = tableDefinition.GetShapeField();
		string oidField = tableDefinition.GetObjectIDField();

		var subFields = new Dictionary<string, int>
		                {
			                { oidField, tableDefinition.FindField(oidField) },
			                { statusField, tableDefinition.FindField(statusField) },
			                { shapeField, tableDefinition.FindField(shapeField) },
			                { errorTypeFieldName, tableDefinition.FindField(errorTypeFieldName) }
		                };

		DbSourceClassSchema schema =
			new DbSourceClassSchema(statusField,
			                        IssueCorrectionStatus.NotCorrected,
			                        IssueCorrectionStatus.Corrected,
			                        subFields);

		string defaultDefinitionQuery = $"{errorTypeFieldName} < 300";

		List<ISourceClass> sourceClasses =
			new List<ISourceClass>
			{
				new ProductionModelIssueItemClass(
					new GdbTableIdentity(errorsFeatureClass), schema,
					null, defaultDefinitionQuery, errorTypeFieldName,
					FilterHelper.Create(errorsFeatureClass, defaultDefinitionQuery),
					dbType: WorkspaceUtils.GetWorkspaceDbType(geodatabase))
			};

		return new DbStatusWorkItemRepository(sourceClasses, stateRepository, gdbPath);
	}

	private static XmlWorkItemStateRepository CreateXmlStateRepository(string filePath)
	{
		var stateRepository =
			new XmlWorkItemStateRepository(filePath, "name", "displayName",
			                               typeof(ProductionModelIssueWorkList));
		stateRepository.LoadAllStates();
		return stateRepository;
	}
}
