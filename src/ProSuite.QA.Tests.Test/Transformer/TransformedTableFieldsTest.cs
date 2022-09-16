using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Licensing;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Transformers;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[TestFixture]
	public class TransformedTableFieldsTest
	{
		private readonly ArcGISLicenses _lic = new ArcGISLicenses();

		[OneTimeSetUp]
		public void SetupFixture()
		{
			_lic.Checkout(EsriProduct.ArcEditor);
		}

		[OneTimeTearDown]
		public void TearDownFixture()
		{
			_lic.Release();
		}

		[Test]
		public void CanValidateFieldNames()
		{
			GdbTable sourceTable = CreateSampleTable();

			var fields = new List<string>
			             {
				             "OBJEKTART AS OBJ_ART",
				             "TABLE_NAME.FIELD_NAME as NAME",
				             "MIN(field_value) AS minVal ",
				             "NOALIAS_field ",
				             "	TABLE_NAME.NO_ALIAS_EITHER"
			             };

			TransformedTableFields tableFields = new TransformedTableFields(sourceTable);

			Assert.IsTrue(tableFields.ValidateFieldNames(fields, true, out string message));
			Assert.IsFalse(tableFields.ValidateFieldNames(fields, false, out message));
		}

		[Test]
		public void CanAddAllFields()
		{
			GdbTable sourceTable = CreateSampleTable();

			GdbTable targetTable = new GdbTable(-1, "OUTPUT_TABLE");

			TransformedTableFields tableFields = new TransformedTableFields(sourceTable);

			tableFields.AddAllFields(targetTable);

			Assert.AreEqual("OBJECTID", targetTable.Fields.Field[0].Name);
			Assert.AreEqual("OBJEKTART", targetTable.Fields.Field[1].Name);
			Assert.AreEqual("FIELD_NAME", targetTable.Fields.Field[2].Name);
			Assert.AreEqual("FIELD_VALUE", targetTable.Fields.Field[3].Name);
			Assert.AreEqual("NOALIAS_FIELD", targetTable.Fields.Field[4].Name);
			Assert.AreEqual("NO_ALIAS_EITHER", targetTable.Fields.Field[5].Name);
			Assert.AreEqual(InvolvedRowUtils.BaseRowField, targetTable.Fields.Field[6].Name);

			WriteFieldNames(targetTable);
		}

		[Test]
		public void CanAddAllFieldsWithDuplicates()
		{
			GdbTable sourceTable1 = CreateSampleTable("TAB1");
			GdbTable sourceTable2 = CreateSampleTable("TAB2");

			GdbTable targetTable = new GdbTable(-1, "OUTPUT_TABLE");

			TransformedTableFields tableFields1 = new TransformedTableFields(sourceTable1);
			TransformedTableFields tableFields2 = new TransformedTableFields(sourceTable2);

			tableFields1.AddAllFields(targetTable);
			tableFields2.AddAllFields(targetTable);

			Assert.AreEqual("OBJECTID", targetTable.Fields.Field[0].Name);
			Assert.AreEqual("OBJEKTART", targetTable.Fields.Field[1].Name);
			Assert.AreEqual("FIELD_NAME", targetTable.Fields.Field[2].Name);
			Assert.AreEqual("FIELD_VALUE", targetTable.Fields.Field[3].Name);
			Assert.AreEqual("NOALIAS_FIELD", targetTable.Fields.Field[4].Name);
			Assert.AreEqual("NO_ALIAS_EITHER", targetTable.Fields.Field[5].Name);
			Assert.AreEqual(InvolvedRowUtils.BaseRowField, targetTable.Fields.Field[6].Name);

			Assert.AreEqual("TAB2_OBJECTID", targetTable.Fields.Field[7].Name);
			Assert.AreEqual("TAB2_OBJEKTART", targetTable.Fields.Field[8].Name);
			Assert.AreEqual("TAB2_FIELD_NAME", targetTable.Fields.Field[9].Name);
			Assert.AreEqual("TAB2_FIELD_VALUE", targetTable.Fields.Field[10].Name);
			Assert.AreEqual("TAB2_NOALIAS_FIELD", targetTable.Fields.Field[11].Name);
			Assert.AreEqual("TAB2_NO_ALIAS_EITHER", targetTable.Fields.Field[12].Name);

			WriteFieldNames(targetTable);
		}

		private static void WriteFieldNames(GdbTable targetTable)
		{
			for (int i = 0; i < targetTable.Fields.FieldCount; i++)
			{
				IField field = targetTable.Fields.Field[i];

				Console.WriteLine(field.Name);
			}
		}

		[Test]
		public void CanAddUserFields()
		{
			IField shapeField = FieldUtils.CreateShapeField(
				"Shape", esriGeometryType.esriGeometryPolygon,
				SpatialReferenceUtils.CreateSpatialReference
				((int) esriSRProjCS2Type.esriSRProjCS_CH1903Plus_LV95,
				 true), 1000);

			GdbTable sourceTable = CreateSampleTable();

			//IField shapeField = FieldUtils.CreateShapeField(
			//	esriGeometryType.esriGeometryPolygon,
			//	SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

			//IField shapeField = FieldUtils.CreateShapeField(
			//				esriGeometryType.esriGeometryPolygon,
			//				SpatialReferenceUtils.CreateSpatialReference(WellKnownHorizontalCS.LV95));

			GdbFeatureClass targetTable =
				new GdbFeatureClass(-1, "OUTPUT_TABLE", esriGeometryType.esriGeometryPoint);

			TransformedTableFields tableFields = new TransformedTableFields(sourceTable);

			int oidFieldIdx = tableFields.AddCustomOIDField(targetTable);
			Assert.False(oidFieldIdx < 0);
			tableFields.AddCustomShapeField(targetTable,
			                                esriGeometryType.esriGeometryPolygon,
			                                shapeField.GeometryDef);

			tableFields.AddUserDefinedFields(new List<string>
			                                 {
				                                 "TABLE_NAME.OBJEKTART",
				                                 "field_value as bla",
				                                 "NOALIAS_FIELD"
			                                 }, targetTable);

			Assert.AreEqual("OBJECTID", targetTable.Fields.Field[0].Name);
			Assert.AreEqual("SHAPE", targetTable.Fields.Field[1].Name);
			Assert.AreEqual("OBJEKTART", targetTable.Fields.Field[2].Name);
			Assert.AreEqual("bla", targetTable.Fields.Field[3].Name);
			Assert.AreEqual("NOALIAS_FIELD", targetTable.Fields.Field[4].Name);
			Assert.AreEqual(InvolvedRowUtils.BaseRowField, targetTable.Fields.Field[5].Name);

			for (int i = 0; i < targetTable.Fields.FieldCount; i++)
			{
				IField field = targetTable.Fields.Field[i];

				Console.WriteLine(field.Name);
			}
		}

		private static GdbTable CreateSampleTable(string tableName = "TABLE_NAME")
		{
			GdbTable table = new GdbTable(-1, tableName, "TableName");
			table.AddFieldT(FieldUtils.CreateOIDField());
			table.AddFieldT(
				FieldUtils.CreateIntegerField("OBJEKTART"));
			table.AddFieldT(FieldUtils.CreateTextField("FIELD_NAME", 12));
			table.AddFieldT(FieldUtils.CreateTextField("FIELD_VALUE", 12));
			table.AddFieldT(FieldUtils.CreateTextField("NOALIAS_FIELD", 12));
			table.AddFieldT(FieldUtils.CreateTextField("NO_ALIAS_EITHER", 12));
			return table;
		}
	}
}
