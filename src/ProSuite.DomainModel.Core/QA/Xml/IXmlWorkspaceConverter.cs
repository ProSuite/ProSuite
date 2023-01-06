using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	/// <summary>
	/// Converter abstraction that allows for platform-specific creation of XML
	/// workspace and looking up/validating model from an XML workspace.
	/// </summary>
	public interface IXmlWorkspaceConverter
	{
		[NotNull]
		XmlWorkspace CreateXmlWorkspace([NotNull] DdxModel model,
		                                bool exportWorkspaceConnections,
		                                bool exportConnectionFilePaths);

		DdxModel SelectMatchingModel(XmlWorkspace forWorkspace, IList<DdxModel> fromModels);
	}
}
