using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ArcGIS.Core.Data;
using NUnit.Framework;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Hosting;
using ProSuite.Commons.Test.Testing;
using ProSuite.Commons.Testing;

namespace ProSuite.Commons.AGP.Core.Test;

[TestFixture]
[Apartment(ApartmentState.STA)]
public class GdbQueryUtilsTest
{
	[OneTimeSetUp]
	public void OneTimeSetUp()
	{
		CoreHostProxy.Initialize();

		TestUtils.ConfigureUnitTestLogging("k2.logging.test.xml");
	}

	[Test, Ignore("Requires oracle connection")]
	public void Can_get_rows_in_list()
	{
		string catalogPath = TestDataPreparer.FromDirectory().GetPath("dkm25k2_as_osa.sde");
		ArcGIS.Core.Data.Geodatabase geodatabase = WorkspaceUtils.OpenGeodatabase(catalogPath);

		var featureClass =
			DatasetUtils.OpenDataset<FeatureClass>(geodatabase, "DKM25K2_MANAGER.DKM25_EINZELBAUM");

		var valueList = new List<string>(GetUuids(featureClass));

		Debug.WriteLine(valueList.Count);

		List<Row> rowsFromList = GdbQueryUtils.GetRowsInList(featureClass, "UUID", valueList, false)
		                                      .ToList();
		Debug.WriteLine(rowsFromList.Count);

		Assert.AreEqual(valueList.Count, rowsFromList.Count);
	}

	private static IEnumerable<string> GetUuids(FeatureClass featureClass)
	{
		using FeatureClassDefinition definition = featureClass.GetDefinition();
		int index = definition.FindField("UUID");

		var queryFilter = new QueryFilter
		                  {
			                  SubFields = "UUID"
		                  };

		foreach (Row row in GdbQueryUtils.GetRows<Row>(featureClass, queryFilter))
		{
			//string readRowValue = GdbObjectUtils.ReadRowValue<string>(row, index);
			yield return (string) row[index];
		}
	}
}
