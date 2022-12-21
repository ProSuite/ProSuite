using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Test;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Testing;
using ProSuite.DomainServices.AO.QA.DatasetReports;
using ProSuite.DomainServices.AO.QA.DatasetReports.Xml;

namespace ProSuite.DomainServices.AO.Test.QA.DatasetReports
{
	[TestFixture]
	public class ObjectClassReportBuilderTest
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

		[Test]
		public void CanGetFeatureClassReport()
		{
			IFeatureClass table = OpenTestFeatureClass("datasetreporting.gdb.zip",
			                                           "fclass_subtypes");

			var builder = new ObjectClassReportBuilder();
			FeatureClassReport result = builder.CreateReport(table, null);

			Console.WriteLine(@"Name:                    {0}", result.Name);
			Console.WriteLine(@"Alias name:              {0}", result.AliasName);
			Console.WriteLine(@"Is workspace versioned:  {0}", result.IsWorkspaceVersioned);
			Console.WriteLine(@"Registered as versioned: {0}", result.IsRegisteredAsVersioned);
			Console.WriteLine(@"Subtype field:           {0}", result.SubtypeField);
			Console.WriteLine(@"Version name:            {0}", result.VersionName);
			Console.WriteLine(@"Geodatabase release:     {0}", result.GeodatabaseRelease);
			Console.WriteLine(@"Is current Gdb release:  {0}",
			                  result.IsCurrentGeodatabaseRelease);
			Console.WriteLine(@"Row count:               {0}", result.RowCount);
			Console.WriteLine(@"Feature type:            {0}", result.FeatureType);
			Console.WriteLine(@"Shape type:              {0}", result.ShapeType);
			Console.WriteLine(@"Has Z:                   {0}", result.HasZ);
			Console.WriteLine(@"Has M:                   {0}", result.HasM);
			Console.WriteLine(@"Vertex count:            {0}", result.VertexCount);
			Console.WriteLine(@"Average vertex count:    {0}", result.AverageVertexCount);
			Console.WriteLine(@"Multipart feature count: {0}", result.MultipartFeatureCount);
			Console.WriteLine(@"Empty geometry count:    {0}", result.EmptyGeometryFeatureCount);
			Console.WriteLine(@"Nonlinear feature count: {0}",
			                  result.NonLinearSegmentFeatureCount);

			foreach (FieldDescriptor fieldDescriptor in result.Fields)
			{
				Console.WriteLine(@"Field name: {0}", fieldDescriptor.Name);
				Console.WriteLine(@"  Alias name:  {0}", fieldDescriptor.AliasName);
				Console.WriteLine(@"  Type:        {0}", fieldDescriptor.Type);
				Console.WriteLine(@"  Length:      {0}", fieldDescriptor.Length);
				Console.WriteLine(@"  Precision:   {0}", fieldDescriptor.Precision);
				Console.WriteLine(@"  Scale:       {0}", fieldDescriptor.Scale);
				Console.WriteLine(@"  Is Nullable: {0}", fieldDescriptor.IsNullable);
				Console.WriteLine(@"  Editable:    {0}", fieldDescriptor.Editable);
				Console.WriteLine(@"  Domain name: {0}", fieldDescriptor.DomainName);

				FieldStatistics statistics = fieldDescriptor.Statistics;
				if (statistics != null)
				{
					Console.WriteLine(@"  Minimum value:    {0}", statistics.MinimumValue);
					Console.WriteLine(@"  Maximum value:    {0}", statistics.MaximumValue);
					Console.WriteLine(@"  Null value count: {0}", statistics.NullValueCount);
					Console.WriteLine(@"  Value count:      {0}", statistics.ValueCount);

					FieldDistinctValues distinctValues = statistics.DistinctValues;
					if (distinctValues != null)
					{
						Console.WriteLine(@"  Distinct value count: {0}",
						                  distinctValues.DistinctValueCount);
						Console.WriteLine(@"  Unique value count: {0}",
						                  distinctValues.UniqueValuesCount);

						List<FieldDistinctValue> distinctValueList = distinctValues.DistinctValues;
						if (distinctValueList != null && distinctValueList.Count > 0)
						{
							Console.WriteLine(@"  Distinct values");
							foreach (FieldDistinctValue fieldDistinctValue in distinctValueList)
							{
								Console.WriteLine(@"    {0} - count: {1}",
								                  fieldDistinctValue.Value,
								                  fieldDistinctValue.Count);
							}

							if (distinctValues.UniqueValuesExcluded)
							{
								Console.WriteLine(@"  Unique values excluded");
							}

							if (distinctValues.MaximumReportedValueCountExceeded)
							{
								Console.WriteLine(@"  Maximum reported value count exceeded");
							}
						}
					}
				}
			}
		}

		[Test]
		public void CanGetIntFieldValueRange()
		{
			var fieldValueRange = new FieldValueRange();

			fieldValueRange.Add(100);
			fieldValueRange.Add(10);
			fieldValueRange.Add(20);
			fieldValueRange.Add(null);
			fieldValueRange.Add(DBNull.Value);

			Assert.AreEqual(3, fieldValueRange.ValueCount);
			Assert.AreEqual(2, fieldValueRange.NullCount);
			Assert.AreEqual(10, fieldValueRange.MinimumValue);
			Assert.AreEqual(100, fieldValueRange.MaximumValue);
		}

		[Test]
		public void CanGetTextFieldValueRange()
		{
			var fieldValueRange = new FieldValueRange();

			fieldValueRange.Add("x");
			fieldValueRange.Add("a");
			fieldValueRange.Add("b");
			fieldValueRange.Add(null);
			fieldValueRange.Add(DBNull.Value);

			Assert.AreEqual(3, fieldValueRange.ValueCount);
			Assert.AreEqual(2, fieldValueRange.NullCount);
			Assert.AreEqual("a", fieldValueRange.MinimumValue);
			Assert.AreEqual("x", fieldValueRange.MaximumValue);
		}

		[NotNull]
		private static IFeatureClass OpenTestFeatureClass([NotNull] string dbName,
		                                                  [NotNull] string featureClassName)
		{
			return (IFeatureClass) OpenTestTable(dbName, featureClassName);
		}

		[NotNull]
		private static ITable OpenTestTable([NotNull] string dbName,
		                                    [NotNull] string tableName)
		{
			string path = TestDataPreparer.ExtractZip(dbName, @"QA\TestData").GetPath();

			IFeatureWorkspace workspace = WorkspaceUtils.OpenFileGdbFeatureWorkspace(path);

			return workspace.OpenTable(tableName);
		}
	}
}
