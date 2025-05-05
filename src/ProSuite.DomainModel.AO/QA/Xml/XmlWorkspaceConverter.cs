using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA.Xml;

namespace ProSuite.DomainModel.AO.QA.Xml
{
	public class XmlWorkspaceConverter : IXmlWorkspaceConverter
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		#region Implementation of IXmlWorkspaceConverter

		public XmlWorkspace CreateXmlWorkspace(DdxModel ddxModel,
		                                       bool exportWorkspaceConnections,
		                                       bool exportConnectionFilePaths)
		{
			var result = new XmlWorkspace
			             {
				             ID = XmlUtils.EscapeInvalidCharacters(ddxModel.Name),
				             ModelName = XmlUtils.EscapeInvalidCharacters(ddxModel.Name),
				             Database = ddxModel.DefaultDatabaseName,
				             SchemaOwner = ddxModel.DefaultDatabaseSchemaOwner
			             };

			if (exportWorkspaceConnections)
			{
				Model model = ddxModel as Model;
				Assert.NotNull(model, "The specified model is not of type Model.");

				IWorkspace workspace = model.GetMasterDatabaseWorkspace();

				Assert.NotNull(workspace,
				               "Unable to determine workspace connection string for model {0} " +
				               "(cannot open model master database workspace: {1})",
				               model.Name,
				               model.GetMasterDatabaseNoAccessReason() ?? "(no details)");

				string catalogPath = workspace.Type ==
				                     esriWorkspaceType.esriRemoteDatabaseWorkspace &&
				                     ! exportConnectionFilePaths
					                     ? null // don't use catalog path even if defined
					                     : WorkspaceUtils.TryGetCatalogPath(workspace);

				if (! string.IsNullOrEmpty(catalogPath))
				{
					result.CatalogPath = catalogPath;
				}
				else
				{
					result.ConnectionString = WorkspaceUtils.GetConnectionString(workspace);
					result.FactoryProgId = WorkspaceUtils.GetFactoryProgId(workspace);
				}
			}

			return result;
		}

		public DdxModel SelectMatchingModel(XmlWorkspace forXmlWorkspace,
		                                    IList<DdxModel> fromModels)
		{
			Assert.ArgumentNotNull(forXmlWorkspace, nameof(forXmlWorkspace));
			Assert.ArgumentNotNull(fromModels, nameof(fromModels));

			if (StringUtils.IsNotEmpty(forXmlWorkspace.ModelName))
			{
				return GetModel(forXmlWorkspace, fromModels);
			}

			// no model name specified

			IWorkspace workspace = OpenWorkspace(forXmlWorkspace);

			IList<Model> modelsForDatabase = GetModelsReferencingSameDatabase(
				forXmlWorkspace, workspace, fromModels);

			if (modelsForDatabase.Count == 1)
			{
				return modelsForDatabase[0];
			}

			if (modelsForDatabase.Count > 1)
			{
				foreach (Model model in modelsForDatabase)
				{
					IWorkspace modelWorkspace = model.GetMasterDatabaseWorkspace();

					if (modelWorkspace != null && ConnectedUserIsEqual(workspace, modelWorkspace))
					{
						return model;
					}
				}

				// No model uses the exact same user, just take the first.

				// If this decision is seriously wrong then we'll find out later when
				// a referenced dataset is not found in the model that is returned here.
				return modelsForDatabase[0];
			}

			throw new InvalidOperationException(
				string.Format("No matching model found for xml workspace with id '{0}'",
				              forXmlWorkspace.ID));
		}

		#endregion

		[NotNull]
		private static IList<Model> GetModelsReferencingSameDatabase(
			[NotNull] XmlWorkspace xmlWorkspace,
			[NotNull] IWorkspace workspace,
			[NotNull] IList<DdxModel> models)
		{
			var result = new List<Model>();

			foreach (DdxModel model in models)
			{
				Model aoModel = model as Model;
				Assert.NotNull(aoModel, "Unexpected type of model");

				IWorkspace modelWorkspace;
				try
				{
					modelWorkspace = aoModel.GetMasterDatabaseWorkspace();
				}
				catch (Exception e)
				{
					_msg.WarnFormat(
						"Unable to open workspace based on connection string for xml workspace with id '{0}': {1}",
						xmlWorkspace.ID, ExceptionUtils.FormatMessage(e));

					// TODO: xmlWorkspace.ModelName?
					result.Add(GetModel(xmlWorkspace.ID, models));
					continue;
				}

				if (! WorkspaceUtils.IsSameDatabase(workspace, modelWorkspace))
				{
					continue;
				}

				// model workspace references the same database as the xml workspace connection string
				result.Add(aoModel);
			}

			return result;
		}

		[NotNull]
		private static DdxModel GetModel([NotNull] XmlWorkspace xmlWorkspace,
		                                 [NotNull] IEnumerable<DdxModel> fromModels)
		{
			Assert.ArgumentNotNull(xmlWorkspace, nameof(xmlWorkspace));
			Assert.ArgumentCondition(! string.IsNullOrEmpty(xmlWorkspace.ModelName),
			                         "model name not specified");

			Model model = GetModel(xmlWorkspace.ModelName, fromModels);

			Assert.NotNull(model, "Model '{0}' referenced in xml workspace not found",
			               xmlWorkspace.ModelName);

			if (StringUtils.IsNotEmpty(xmlWorkspace.CatalogPath) ||
			    StringUtils.IsNotEmpty(xmlWorkspace.ConnectionString))
			{
				IWorkspace workspace;
				try
				{
					workspace = OpenWorkspace(xmlWorkspace);
				}
				catch (Exception e)
				{
					_msg.WarnFormat(e.Message);
					// no further validation possible, just use the model
					return model;
				}

				IWorkspace modelWorkspace;
				try
				{
					modelWorkspace = model.GetMasterDatabaseWorkspace();
				}
				catch (Exception e)
				{
					_msg.WarnFormat("Unable to open workspace for model '{0}': {1}", model.Name,
					                e.Message);
					// no further validation possible, just use the model
					return model;
				}

				// both workspaces could be opened, they must reference the same database
				Assert.True(WorkspaceUtils.IsSameDatabase(workspace, modelWorkspace),
				            "connection string in xml workspace id '{0}' references a different database than the referenced model '{1}'",
				            xmlWorkspace.ID, model.Name);
			}

			return model;
		}

		[NotNull]
		private static Model GetModel([NotNull] string modelName,
		                              [NotNull] IEnumerable<DdxModel> fromModels)
		{
			DdxModel model = fromModels.FirstOrDefault(
				m => m.Name.Equals(modelName, StringComparison.InvariantCultureIgnoreCase));

			Assert.NotNull(model, "Model '{0}' corresponding to xml workspace id not found",
			               modelName);

			return (Model) model;
		}

		private static bool ConnectedUserIsEqual([NotNull] IWorkspace ws1,
		                                         [NotNull] IWorkspace ws2)
		{
			string user1 = WorkspaceUtils.GetConnectedUser(ws1);
			string user2 = WorkspaceUtils.GetConnectedUser(ws2);

			return user1.Equals(user2, StringComparison.InvariantCultureIgnoreCase);
		}

		[NotNull]
		private static IWorkspace OpenWorkspace([NotNull] XmlWorkspace xmlWorkspace)
		{
			Assert.ArgumentNotNull(xmlWorkspace, nameof(xmlWorkspace));
			Assert.ArgumentCondition(
				StringUtils.IsNotEmpty(xmlWorkspace.CatalogPath) ||
				StringUtils.IsNotEmpty(xmlWorkspace.ConnectionString),
				"neither catalog path nor connection string are specified for xml workspace id '{0}'",
				xmlWorkspace.ID);

			if (StringUtils.IsNotEmpty(xmlWorkspace.CatalogPath))
			{
				try
				{
					return WorkspaceUtils.OpenWorkspace(xmlWorkspace.CatalogPath);
				}
				catch (Exception e)
				{
					throw new InvalidConfigurationException(
						string.Format(
							"Unable to open workspace for catalog path of xml workspace with id '{0}': {1}",
							xmlWorkspace.ID, e.Message));
				}
			}

			Assert.NotNullOrEmpty(xmlWorkspace.FactoryProgId,
			                      "no factory progId is specified for xml workspace id '{0}'",
			                      xmlWorkspace.ID);

			try
			{
				return WorkspaceUtils.OpenWorkspace(xmlWorkspace.ConnectionString,
				                                    xmlWorkspace.FactoryProgId);
			}
			catch (Exception e)
			{
				throw new InvalidConfigurationException(
					string.Format(
						"Unable to open workspace for connection string of xml workspace with id '{0}': {1}",
						xmlWorkspace.ID, e.Message), e);
			}
		}
	}
}
