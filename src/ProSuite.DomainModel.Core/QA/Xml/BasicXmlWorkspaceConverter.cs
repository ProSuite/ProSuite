using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Text;
using ProSuite.Commons.Xml;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public class BasicXmlWorkspaceConverter : IXmlWorkspaceConverter
	{
		#region Implementation of IXmlWorkspaceConverter

		public XmlWorkspace CreateXmlWorkspace(DdxModel ddxModel,
		                                       bool exportWorkspaceConnections,
		                                       bool exportConnectionFilePaths)
		{
			Assert.False(exportWorkspaceConnections,
			             "This implementation does not support exporting the workspace connections");

			return new XmlWorkspace
			       {
				       ID = XmlUtils.EscapeInvalidCharacters(ddxModel.Name),
				       ModelName = XmlUtils.EscapeInvalidCharacters(ddxModel.Name),
				       Database = ddxModel.DefaultDatabaseName,
				       SchemaOwner = ddxModel.DefaultDatabaseSchemaOwner
			       };
		}

		public DdxModel SelectMatchingModel(XmlWorkspace forWorkspace, IList<DdxModel> fromModels)
		{
			string modelName = forWorkspace.ModelName;

			if (StringUtils.IsNotEmpty(modelName))
			{
				DdxModel result = fromModels.FirstOrDefault(
					m => m.Name.Equals(modelName, StringComparison.InvariantCultureIgnoreCase));

				Assert.NotNull(result, "Model '{0}' corresponding to xml workspace id not found",
				               modelName);

				return result;
			}
			else
			{
				throw new ArgumentException("Provided xml workspace has no model defined.");
			}
		}

		#endregion
	}
}
