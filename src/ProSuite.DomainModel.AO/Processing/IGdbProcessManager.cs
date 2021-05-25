using System.Collections.Generic;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Processing;

namespace ProSuite.DomainModel.AO.Processing
{
	public interface IGdbProcessManager
	{
		void LoadProcesses([CanBeNull] IProgressFeedback pf);

		[CanBeNull]
		IGdbProcess GetProcess([NotNull] string processName);

		IEnumerable<T> GetProcesses<T>(IProcessingContext context, string group)
			where T : IGdbProcess;

		[NotNull]
		ICollection<string> GetGroupNames();

		[CanBeNull]
		Image GetProcessIcon([NotNull] string processName);

		string AllProcessesGroupName { get; }
		string GroupProcessGroupName { get; }
		IEnumerable<ProcessSelectionType> ProcessSelectionTypes { get; }
		bool AllowParameterModification { get; }

		[NotNull]
		DdxModel Model { get; }

		[NotNull]
		IDatasetContext DatasetContext { get; }

		// TODO Separate interface IProcessingContextFactory? Overload w/ReferenceScale?
		[NotNull]
		IProcessingContext CreateContext(ProcessSelectionType selectionType,
		                                 ProcessExecutionType executionType);
	}
}
