using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public interface IXmlDataQualityExporter
	{
		void Export([NotNull] IEnumerable<QualitySpecification> qualitySpecifications,
		            [NotNull] string xmlFilePath,
		            bool exportMetadata,
		            bool? exportWorkspaceConnections,
		            bool exportConnectionFilePaths,
		            bool exportAllTestDescriptors,
		            bool exportAllCategories,
		            bool exportNotes);

		void Export([NotNull] QualitySpecification specification,
		            [NotNull] string xmlFilePath,
		            bool exportMetadata,
		            bool? exportWorkspaceConnections,
		            bool exportConnectionFilePaths,
		            bool exportAllTestDescriptors,
		            bool exportAllCategories,
		            bool exportNotes);

		/// <summary>
		/// Gets or sets a value indicating whether connection strings should be exported for 
		/// referenced workspaces.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if connection strings (which may include encrypted passwords) should be
		/// exported for referenced workspaces; if <c>false</c>, workspaces are identified only by
		/// the name of the corresponding model.
		/// </value>
		[UsedImplicitly]
		bool ExportWorkspaceConnections { get; set; }
	}
}
