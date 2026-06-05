using System.Collections.Generic;
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
		string shapeField = tableDefinition.GetShapeField();

		string statusFieldName = "STATUS";

		int fieldIndex = tableDefinition.FindField(statusFieldName);
		var errorTypeFieldName = "FEHLERART";
		var schema = new DbSourceClassSchema(tableDefinition.GetObjectIDField(), shapeField,
		                                     statusFieldName, fieldIndex,
		                                     (int) IssueCorrectionStatus.NotCorrected,
		                                     (int) IssueCorrectionStatus.Corrected,
		                                     errorTypeFieldName);

		string defaultDefinitionQuery = $"{errorTypeFieldName} < 300";

		List<ISourceClass> sourceClasses =
			new List<ISourceClass>
			{
				new ProductionModelIssueItemClass(
					new GdbTableIdentity(errorsFeatureClass), schema,
					null, defaultDefinitionQuery,
					FilterHelper.Create(errorsFeatureClass, defaultDefinitionQuery),
					WorkspaceUtils.GetWorkspaceDbType(geodatabase))
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
