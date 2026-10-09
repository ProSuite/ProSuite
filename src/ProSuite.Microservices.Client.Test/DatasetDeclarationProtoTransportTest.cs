using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Exceptions;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.Microservices.Client.Test
{
	/// <summary>
	/// The proto twin of <c>DatasetFieldRoleTransportTest</c>: the field roles and the declared
	/// dataset type that a condition list carries alongside a dataset parameter value, this time
	/// over the wire rather than through XML. They stand in for what a DDX dataset would know
	/// about itself and a harvested model cannot - most immediately the file-path field of a
	/// raster or point cloud catalog.
	/// </summary>
	[TestFixture]
	public class DatasetDeclarationProtoTransportTest
	{
		private const string _catalogName = "RAS_DTM_TILES";
		private const string _pathField = "CURRENT_FILE_PATH";
		private const string _workspaceId = "ws1";

		#region Writing the message

		[Test]
		public void Can_write_field_roles_and_dataset_type_to_the_message()
		{
			ParameterMsg parameterMsg = GetDatasetParameterMsg(
				CreateSpecification(SupportedDatasetType.RasterCatalog, FilePathRole()));

			Assert.AreEqual((int) SupportedDatasetType.RasterCatalog, parameterMsg.DatasetType);

			Assert.AreEqual(1, parameterMsg.FieldRoles.Count);
			Assert.AreEqual("FilePath", parameterMsg.FieldRoles[0].Role);
			Assert.AreEqual(_pathField, parameterMsg.FieldRoles[0].Name);
		}

		[Test]
		public void Value_without_declarations_writes_nothing()
		{
			// The common case: a DDX dataset knows its own type and carries its own roles, so
			// nothing is written and the message stays what it was before this feature.
			ParameterMsg parameterMsg =
				GetDatasetParameterMsg(CreateSpecification(SupportedDatasetType.Null, null));

			Assert.AreEqual(0, parameterMsg.DatasetType);
			Assert.AreEqual(0, parameterMsg.FieldRoles.Count);
		}

		[Test]
		public void Can_round_trip_a_point_cloud_catalog_declaration()
		{
			ParameterMsg parameterMsg = GetDatasetParameterMsg(
				CreateSpecification(SupportedDatasetType.PointCloudCatalog, FilePathRole()));

			DatasetDeclaration declaration = GetSingleDeclaration(
				parameterMsg.WorkspaceId, CreateCondition(parameterMsg));

			Assert.AreEqual(_catalogName, declaration.DatasetName);
			Assert.AreEqual(SupportedDatasetType.PointCloudCatalog, declaration.DatasetType);
			Assert.AreEqual(AttributeRole.FilePath, declaration.FieldRoles[0].Role);
			Assert.AreEqual(_pathField, declaration.FieldRoles[0].FieldName);
		}

		#endregion

		#region Collecting the declarations

		[Test]
		public void Can_collect_declarations_by_dataset_name()
		{
			// The same dataset declared by two conditions: one declaration, no complaint.
			QualityConditionMsg condition1 = CreateCondition(CreateCatalogParameterMsg());
			QualityConditionMsg condition2 = CreateCondition(CreateCatalogParameterMsg());

			IList<DatasetDeclaration> declarations =
				ProtoDataQualityUtils.GetDatasetDeclarations(
					_workspaceId, new[] { condition1, condition2 });

			Assert.AreEqual(1, declarations.Count);
			Assert.AreEqual(_catalogName, declarations[0].DatasetName);
			Assert.AreEqual(SupportedDatasetType.RasterCatalog, declarations[0].DatasetType);
		}

		[Test]
		public void Parameter_without_field_roles_declares_nothing()
		{
			var parameterMsg = new ParameterMsg
			                   {
				                   Name = "featureClass",
				                   Value = _catalogName,
				                   WorkspaceId = _workspaceId
			                   };

			Assert.IsEmpty(ProtoDataQualityUtils.GetDatasetDeclarations(
				               _workspaceId, new[] { CreateCondition(parameterMsg) }));
		}

		[Test]
		public void Declarations_of_another_workspace_are_not_collected()
		{
			// One model is built per workspace, and each must see only its own declarations.
			Assert.IsEmpty(ProtoDataQualityUtils.GetDatasetDeclarations(
				               "ws2", new[] { CreateCondition(CreateCatalogParameterMsg()) }));
		}

		[Test]
		public void Anonymous_workspace_is_not_a_missing_workspace()
		{
			// DataSource.AnonymousId is the empty string, DataSource documents its id as optional,
			// and proto3 defaults DataSourceMsg.id to it. A client that never names its data source
			// must not fail here - it would fail every verification, not only those using a catalog.
			var parameterMsg = new ParameterMsg
			                   {
				                   Name = "featureClass",
				                   Value = _catalogName
			                   };

			Assert.IsEmpty(ProtoDataQualityUtils.GetDatasetDeclarations(
				               string.Empty, new[] { CreateCondition(parameterMsg) }));
		}

		[Test]
		public void Can_declare_a_catalog_in_the_anonymous_workspace()
		{
			// The counterpart of the above: a declaration with no workspace id belongs to the
			// anonymous workspace and must be collected for it.
			var parameterMsg = new ParameterMsg
			                   {
				                   Name = "featureClass",
				                   Value = _catalogName,
				                   DatasetType = (int) SupportedDatasetType.RasterCatalog,
				                   FieldRoles =
				                   {
					                   new DatasetFieldRoleMsg
					                   { Role = "FilePath", Name = _pathField }
				                   }
			                   };

			DatasetDeclaration declaration =
				GetSingleDeclaration(string.Empty, CreateCondition(parameterMsg));

			Assert.AreEqual(_catalogName, declaration.DatasetName);
			Assert.AreEqual(SupportedDatasetType.RasterCatalog, declaration.DatasetType);
		}

		[Test]
		public void Declarations_inside_a_transformer_are_collected()
		{
			// A transformer has dataset parameters of its own, and one of them can reference a
			// catalog just as a condition parameter can.
			var transformerMsg = new InstanceConfigurationMsg
			                     {
				                     Name = "transformer",
				                     InstanceDescriptorName = "TrGeometryToPoints(0)",
				                     Parameters = { CreateCatalogParameterMsg() }
			                     };

			QualityConditionMsg conditionMsg = CreateCondition(
				new ParameterMsg { Name = "featureClass", Transformer = transformerMsg });

			DatasetDeclaration declaration = GetSingleDeclaration(_workspaceId, conditionMsg);

			Assert.AreEqual(_catalogName, declaration.DatasetName);
		}

		[Test]
		public void Declarations_inside_an_issue_filter_are_collected()
		{
			var issueFilterMsg = new InstanceConfigurationMsg
			                     {
				                     Name = "issueFilter",
				                     InstanceDescriptorName = "IfWithin(0)",
				                     Parameters = { CreateCatalogParameterMsg() }
			                     };

			var conditionMsg = new QualityConditionMsg
			                   {
				                   Name = "Condition",
				                   TestDescriptorName = "Descriptor(0)",
				                   ConditionIssueFilters = { issueFilterMsg }
			                   };

			Assert.AreEqual(_catalogName,
			                GetSingleDeclaration(_workspaceId, conditionMsg).DatasetName);
		}

		#endregion

		#region Errors

		[Test]
		public void Field_roles_without_a_declared_dataset_type_are_an_error()
		{
			// The roles say which field holds the path, not what to do with it.
			ParameterMsg parameterMsg = CreateCatalogParameterMsg();
			parameterMsg.DatasetType = 0;

			var exception = Assert.Throws<InvalidConfigurationException>(
				() => ProtoDataQualityUtils.GetDatasetDeclarations(
					_workspaceId, new[] { CreateCondition(parameterMsg) }));

			StringAssert.Contains("datasetType", exception.Message);
		}

		[Test]
		public void Conflicting_declarations_of_the_same_dataset_are_an_error()
		{
			// Only one of them can end up on the dataset, so this cannot be resolved silently.
			ParameterMsg asRasterCatalog = CreateCatalogParameterMsg();

			ParameterMsg asPointCloudCatalog = CreateCatalogParameterMsg();
			asPointCloudCatalog.DatasetType = (int) SupportedDatasetType.PointCloudCatalog;

			Assert.Throws<InvalidConfigurationException>(
				() => ProtoDataQualityUtils.GetDatasetDeclarations(
					_workspaceId,
					new[]
					{
						CreateCondition(asRasterCatalog),
						CreateCondition(asPointCloudCatalog)
					}));
		}

		[Test]
		public void Unknown_role_name_is_an_error()
		{
			// Dropping it silently would leave the dataset without the field it needs and fail
			// much later, far from the cause.
			ParameterMsg parameterMsg = CreateCatalogParameterMsg();
			parameterMsg.FieldRoles[0].Role = "NoSuchRole";

			var exception = Assert.Throws<InvalidConfigurationException>(
				() => ProtoDataQualityUtils.GetDatasetDeclarations(
					_workspaceId, new[] { CreateCondition(parameterMsg) }));

			StringAssert.Contains("NoSuchRole", exception.Message);
		}

		[Test]
		public void Role_without_a_field_name_is_an_error()
		{
			ParameterMsg parameterMsg = CreateCatalogParameterMsg();
			parameterMsg.FieldRoles[0].Name = string.Empty;

			Assert.Throws<InvalidConfigurationException>(
				() => ProtoDataQualityUtils.GetDatasetDeclarations(
					_workspaceId, new[] { CreateCondition(parameterMsg) }));
		}

		#endregion

		#region Test setup

		private static IList<DatasetFieldRole> FilePathRole()
		{
			return new List<DatasetFieldRole>
			       { new DatasetFieldRole(AttributeRole.FilePath, _pathField) };
		}

		private static ParameterMsg CreateCatalogParameterMsg()
		{
			return new ParameterMsg
			       {
				       Name = "featureClass",
				       Value = _catalogName,
				       WorkspaceId = _workspaceId,
				       DatasetType = (int) SupportedDatasetType.RasterCatalog,
				       FieldRoles =
				       {
					       new DatasetFieldRoleMsg { Role = "FilePath", Name = _pathField }
				       }
			       };
		}

		private static QualityConditionMsg CreateCondition(ParameterMsg parameterMsg)
		{
			return new QualityConditionMsg
			       {
				       Name = "Condition",
				       TestDescriptorName = "Descriptor(0)",
				       Parameters = { parameterMsg }
			       };
		}

		private static DatasetDeclaration GetSingleDeclaration(
			string workspaceId, QualityConditionMsg conditionMsg)
		{
			IList<DatasetDeclaration> declarations =
				ProtoDataQualityUtils.GetDatasetDeclarations(workspaceId, new[] { conditionMsg });

			Assert.AreEqual(1, declarations.Count, "Expected exactly one declaration");

			return declarations[0];
		}

		private static QualitySpecification CreateSpecification(
			SupportedDatasetType datasetType, IList<DatasetFieldRole> fieldRoles)
		{
			var testDescriptor = new TestDescriptor(
				"Descriptor1",
				new ClassDescriptor("ProSuite.QA.Tests.QaConstraint", "ProSuite.QA.Tests"), 0);

			var condition = new QualityCondition("Condition", testDescriptor);

			Dataset catalog = new TestVectorDataset(_catalogName);
			var model = new TestModel("TEST_MODEL");
			model.AddDataset(catalog);

			condition.AddParameterValue(
				new DatasetTestParameterValue("table", typeof(IReadOnlyTable))
				{
					DatasetValue = catalog,
					DatasetType = datasetType,
					FieldRoles = fieldRoles
				});

			var specification = new QualitySpecification("TestSpecification");
			specification.AddElement(condition);

			// GetCustomizable() clones the parameter values: the declarations must survive that,
			// or a customized specification would lose them.
			return specification.GetCustomizable();
		}

		private static ParameterMsg GetDatasetParameterMsg(QualitySpecification specification)
		{
			ConditionListSpecificationMsg specificationMsg =
				ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
					specification, null, out _);

			return specificationMsg.Elements.Single().Condition.Parameters.Single();
		}

		private class TestModel : DdxModel
		{
			public TestModel(string name) : base(name)
			{
				SetCloneId(123);
			}

			public override string QualifyModelElementName(string modelElementName)
			{
				return modelElementName;
			}

			public override string TranslateToModelElementName(string masterDatabaseDatasetName)
			{
				return masterDatabaseDatasetName;
			}

			protected override void CheckAssignSpecialDatasetCore(Dataset dataset) { }
		}

		private class TestVectorDataset : VectorDataset
		{
			public TestVectorDataset(string name) : base(name) { }
		}

		#endregion
	}
}
