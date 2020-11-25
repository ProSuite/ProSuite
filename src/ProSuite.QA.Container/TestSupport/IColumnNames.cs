using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestSupport
{
	public interface IColumnNames
	{
		[NotNull]
		List<string> GetInvolvedColumnNames(
			bool showWorkspaceName = false,
			bool showWorkspaceNameIfDiffers = false,
			bool showTableName = false,
			bool showTableNameIfDiffers = false,
			bool showTableAliasName = false,
			bool showTableAliasNameIfDiffers = false);
	}
}
