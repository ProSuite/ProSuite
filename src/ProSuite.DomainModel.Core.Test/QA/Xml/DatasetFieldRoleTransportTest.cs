using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.Test.QA.Xml
{
	/// <summary>
	/// The field roles a standalone condition list carries alongside a dataset parameter value.
	/// They stand in for the attribute roles a DDX dataset would have, which a model harvested
	/// from the workspace cannot know - most immediately the file-path field of a raster catalog.
	/// </summary>
	[TestFixture]
	public class DatasetFieldRoleTransportTest
	{
		private const string _pathField = "CURRENT_FILE_PATH";

		[Test]
		public void Can_resolve_role_by_simple_and_qualified_name()
		{
			Assert.IsTrue(AttributeRole.TryResolve("FilePath", out AttributeRole simple));
			Assert.AreEqual(AttributeRole.FilePath, simple);

			Assert.IsTrue(
				AttributeRole.TryResolve("AttributeRole.FilePath", out AttributeRole qualified));
			Assert.AreEqual(AttributeRole.FilePath, qualified);

			Assert.IsTrue(AttributeRole.TryResolve("filepath", out AttributeRole lowerCase));
			Assert.AreEqual(AttributeRole.FilePath, lowerCase);

			Assert.AreEqual("FilePath", AttributeRole.GetSimpleName(AttributeRole.FilePath));
		}

		[Test]
		public void Cannot_resolve_unknown_role_name()
		{
			Assert.IsFalse(AttributeRole.TryResolve("NoSuchRole", out AttributeRole _));
			Assert.IsFalse(AttributeRole.TryResolve(null, out AttributeRole _));
			Assert.IsFalse(AttributeRole.TryResolve(" ", out AttributeRole _));
		}

		[Test]
		public void Can_read_field_roles_from_document()
		{
			XmlDataQualityDocument document = Deserialize(GetDocument(
				   $@"<Fields>
                     <Field role=""FilePath"" name=""{_pathField}"" />
                   </Fields>"));

			XmlDatasetTestParameterValue dtm = GetDtmParameterValue(document);

			Assert.NotNull(dtm.FieldRoles, "No field roles read");
			Assert.AreEqual(1, dtm.FieldRoles.Count);
			Assert.AreEqual("FilePath", dtm.FieldRoles[0].Role);
			Assert.AreEqual(_pathField, dtm.FieldRoles[0].Name);
		}

		[Test]
		public void Document_without_field_roles_still_reads()
		{
			// The element is optional: every existing document must keep working unchanged.
			XmlDataQualityDocument document = Deserialize(GetDocument(string.Empty));

			Assert.That(GetDtmParameterValue(document).FieldRoles, Is.Null.Or.Empty);
		}

		[Test]
		public void Can_round_trip_field_roles_through_the_parameter_value()
		{
			var testParameter = new TestParameter(
				"dtm", typeof(string), "the surface");

			var original = new DatasetTestParameterValue(testParameter)
			               {
				               FieldRoles = new List<DatasetFieldRole>
				                            {
					                            new DatasetFieldRole(
						                            AttributeRole.FilePath, _pathField)
				                            }
			               };

			XmlDatasetTestParameterValue xmlValue = Roundtrip(original);

			Assert.NotNull(xmlValue.FieldRoles);
			Assert.AreEqual(1, xmlValue.FieldRoles.Count);
			Assert.AreEqual("FilePath", xmlValue.FieldRoles[0].Role);
			Assert.AreEqual(_pathField, xmlValue.FieldRoles[0].Name);
		}

		[Test]
		public void Value_without_field_roles_writes_no_element()
		{
			var testParameter = new TestParameter(
				"dtm", typeof(string), "the surface");

			// The common case: nothing extra in the document.
			Assert.IsNull(Roundtrip(new DatasetTestParameterValue(testParameter)).FieldRoles);
		}

		[Test]
		public void Unknown_role_name_in_document_is_an_error()
		{
			// Skipping it silently would leave the dataset without the field it needs and fail
			// much later, far from the cause.
			XmlDataQualityDocument document = Deserialize(GetDocument(
				                                              $@"<Fields>
                     <Field role=""NoSuchRole"" name=""{_pathField}"" />
                   </Fields>"));

			var exception =
				Assert.Throws<InvalidConfigurationException>(() => GetDeclarations(
					                                             document, "ws1"));

			Assert.IsTrue(exception.Message.Contains("NoSuchRole"), exception.Message);
		}

		[Test]
		public void Can_collect_declarations_by_dataset_name()
		{
			XmlDataQualityDocument document = Deserialize(GetDocument(
				                                              $@"<Fields>
                     <Field role=""FilePath"" name=""{_pathField}"" />
                   </Fields>"));

			DatasetDeclaration declaration = GetSingleDeclaration(document, "ws1");

			Assert.AreEqual("RAS_DTM_TILES", declaration.DatasetName);
			Assert.AreEqual(1, declaration.FieldRoles.Count);
			Assert.AreEqual(AttributeRole.FilePath, declaration.FieldRoles[0].Role);
			Assert.AreEqual(_pathField, declaration.FieldRoles[0].FieldName);
		}

		[Test]
		public void Collected_declaration_carries_the_declared_dataset_type()
		{
			// The roles alone do not say what kind of catalog this is - every file catalog has a
			// file-path field. The declared type does.
			XmlDataQualityDocument document = Deserialize(GetDocument(
				                                              $@"<Fields>
                     <Field role=""FilePath"" name=""{_pathField}"" />
                   </Fields>"));

			Assert.AreEqual(SupportedDatasetType.RasterCatalog,
			                GetSingleDeclaration(document, "ws1").DatasetType);
		}

		[Test]
		public void Can_declare_a_point_cloud_catalog()
		{
			// The second file catalog kind travels the same way; the only difference is the
			// declared type, which is what decides whether the dataset ends up as a surface
			// source or as a point cloud.
			XmlDataQualityDocument document = Deserialize(GetDocument(
				                                              $@"<Fields>
                     <Field role=""FilePath"" name=""{_pathField}"" />
                   </Fields>", datasetType: "PointCloudCatalog"));

			DatasetDeclaration declaration = GetSingleDeclaration(document, "ws1");

			Assert.AreEqual(SupportedDatasetType.PointCloudCatalog, declaration.DatasetType);
			Assert.AreEqual(AttributeRole.FilePath, declaration.FieldRoles.Single().Role);
			Assert.AreEqual(_pathField, declaration.FieldRoles.Single().FieldName);
		}

		[Test]
		public void Can_round_trip_a_point_cloud_catalog_declaration()
		{
			var original = new DatasetTestParameterValue(
				               new TestParameter("dtm", typeof(string), "the point cloud"))
			               {
				               DatasetType = SupportedDatasetType.PointCloudCatalog,
				               FieldRoles = new List<DatasetFieldRole>
				                            {
					                            new DatasetFieldRole(
						                            AttributeRole.FilePath, _pathField)
				                            }
			               };

			XmlDatasetTestParameterValue xmlValue = Roundtrip(original);

			Assert.AreEqual(SupportedDatasetType.PointCloudCatalog, xmlValue.DatasetType);
			Assert.AreEqual(_pathField, xmlValue.FieldRoles.Single().Name);
		}

		[Test]
		public void Field_roles_without_a_declared_dataset_type_are_an_error()
		{
			// Guessing would pick the wrong catalog kind as soon as there is more than one.
			XmlDataQualityDocument document = Deserialize(GetDocument(
				                                              $@"<Fields>
                     <Field role=""FilePath"" name=""{_pathField}"" />
                   </Fields>", datasetType: null));

			var exception =
				Assert.Throws<InvalidConfigurationException>(() => GetDeclarations(
					                                             document, "ws1"));

			Assert.IsTrue(exception.Message.Contains("datasetType"), exception.Message);
		}

		[Test]
		public void Document_without_a_declared_dataset_type_still_reads()
		{
			// The attribute is optional: every existing document must keep working unchanged.
			XmlDataQualityDocument document =
				Deserialize(GetDocument(string.Empty, datasetType: null));

			Assert.AreEqual(SupportedDatasetType.Null,
			                GetDtmParameterValue(document).DatasetType);
		}

		[Test]
		public void Can_round_trip_the_declared_dataset_type()
		{
			var testParameter = new TestParameter(
				"dtm", typeof(string), "the surface");

			var original = new DatasetTestParameterValue(testParameter)
			               {
				               DatasetType = SupportedDatasetType.RasterCatalog
			               };

			Assert.AreEqual(SupportedDatasetType.RasterCatalog, Roundtrip(original).DatasetType);
		}

		[Test]
		public void Field_roles_of_another_workspace_are_not_collected()
		{
			XmlDataQualityDocument document = Deserialize(GetDocument(
				                                              $@"<Fields>
                     <Field role=""FilePath"" name=""{_pathField}"" />
                   </Fields>"));

			Assert.AreEqual(0, GetDeclarations(document, "other").Count);
		}

		[Test]
		public void Field_roles_survive_a_clone()
		{
			// Conditions get cloned, e.g. when a specification is customized. Losing the roles
			// there would leave the catalog without its file-path field.
			var original = new DatasetTestParameterValue("dtm", typeof(string))
			               {
				               FieldRoles = new List<DatasetFieldRole>
				                            {
					                            new DatasetFieldRole(
						                            AttributeRole.FilePath, _pathField)
				                            }
			               };

			var clone = (DatasetTestParameterValue) original.Clone();

			Assert.NotNull(clone.FieldRoles);
			Assert.AreEqual(1, clone.FieldRoles.Count);
			Assert.AreEqual(AttributeRole.FilePath, clone.FieldRoles[0].Role);
			Assert.AreEqual(_pathField, clone.FieldRoles[0].FieldName);

			// ... and the two must not share the list.
			clone.FieldRoles.Clear();
			Assert.AreEqual(1, original.FieldRoles.Count);
		}

		#region Test setup

		private class StubInstanceInfo : IInstanceInfo
		{
			public string TestDescription => null;

			public string[] TestCategories => new string[0];

			public IList<TestParameter> Parameters { get; } =
				new List<TestParameter>
				{
					new TestParameter("dtm", typeof(string))
				};

			public Type InstanceType => typeof(DatasetFieldRole);

			public TestParameter GetParameter(string parameterName)
			{
				return Parameters.FirstOrDefault(p => p.Name == parameterName);
			}

			public string GetParameterDescription(string parameterName) => null;
		}

		private static IList<DatasetDeclaration> GetDeclarations(
			XmlDataQualityDocument document, string workspaceId)
		{
			return XmlDataQualityUtils.GetDatasetDeclarations(
				workspaceId, GetConditions(document));
		}

		private static DatasetDeclaration GetSingleDeclaration(
			XmlDataQualityDocument document, string workspaceId)
		{
			return GetDeclarations(document, workspaceId).Single();
		}

		private static IList<XmlInstanceConfiguration> GetConditions(
			XmlDataQualityDocument document)
		{
			return document.GetAllQualityConditions()
			               .Select(pair => (XmlInstanceConfiguration) pair.Key)
			               .ToList();
		}

		private static XmlDatasetTestParameterValue GetDtmParameterValue(
			XmlDataQualityDocument document)
		{
			XmlQualityCondition condition = document.GetAllQualityConditions()
			                                        .Select(pair => pair.Key)
			                                        .Single();

			return condition.ParameterValues
			                .OfType<XmlDatasetTestParameterValue>()
			                .Single(p => p.TestParameterName == "dtm");
		}

		private static XmlDataQualityDocument Deserialize(string xml)
		{
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
			using (var reader = new StreamReader(stream))
			{
				// Validates against the schema, so this also covers the XSD.
				return XmlDataQualityUtils.Deserialize(reader);
			}
		}

		/// <summary>
		/// Exports a parameter value to XML and reads the result back, i.e. exercises both
		/// conversion directions of XmlDataQualityUtils.
		/// </summary>
		private static XmlDatasetTestParameterValue Roundtrip(
			DatasetTestParameterValue parameterValue)
		{
			var testDescriptor =
				new TestDescriptor("td", new ClassDescriptor(typeof(DatasetFieldRole)))
				{
					// Pre-set so that the export does not reflect over a real test class.
					InstanceInfo = new StubInstanceInfo()
				};

			var condition = new QualityCondition("qc", testDescriptor);

			condition.AddParameterValue(parameterValue);

			var specification = new QualitySpecification("spec");
			specification.AddElement(condition);

			var document = new XmlDataQualityDocument30();

			XmlDataQualityUtils.Populate(document,
			                             new Dictionary<DdxModel, string>(),
			                             new List<QualitySpecification> { specification },
			                             null, null,
			                             exportMetadata: false,
			                             exportAllDescriptors: false,
			                             exportAllCategories: false,
			                             exportNotes: false);

			return document.GetAllQualityConditions()
			               .Select(pair => pair.Key)
			               .Single()
			               .ParameterValues
			               .OfType<XmlDatasetTestParameterValue>()
			               .Single();
		}

		private static string GetDocument(string fieldsElement,
		                                  string datasetType = "RasterCatalog")
		{
			string datasetTypeAttribute =
				datasetType == null ? string.Empty : $@"datasetType=""{datasetType}""";

			return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<DataQuality xmlns=""urn:ProSuite.QA.QualitySpecifications-3.0"">
  <QualitySpecifications>
    <QualitySpecification name=""spec"">
      <Elements>
        <Element qualityCondition=""surface_vertex"" />
      </Elements>
    </QualitySpecification>
  </QualitySpecifications>
  <QualityConditions>
    <QualityCondition name=""surface_vertex"" testDescriptor=""QaSurfaceVertex"">
      <Parameters>
        <Dataset parameter=""featureClass"" value=""TLM_STRASSE"" workspace=""ws1"" />
        <Dataset parameter=""dtm"" value=""RAS_DTM_TILES"" workspace=""ws1""
                 {datasetTypeAttribute}>
          {fieldsElement}
        </Dataset>
        <Scalar parameter=""limit"" value=""1"" />
      </Parameters>
    </QualityCondition>
  </QualityConditions>
  <TestDescriptors>
    <TestDescriptor name=""QaSurfaceVertex"">
      <TestClass type=""ProSuite.QA.Tests.QaSurfaceVertex"" assembly=""ProSuite.QA.Tests""
                 constructorIndex=""4"" />
    </TestDescriptor>
  </TestDescriptors>
  <Workspaces>
    <Workspace id=""ws1"" catalogPath=""C:\temp\test.gdb"" />
  </Workspaces>
</DataQuality>";
		}

		#endregion
	}
}
