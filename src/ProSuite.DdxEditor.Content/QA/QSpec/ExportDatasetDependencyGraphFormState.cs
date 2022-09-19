using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	[UsedImplicitly]
	public class ExportDatasetDependencyGraphFormState : FormState
	{
		private const ExportTarget _defaultExportTarget = ExportTarget.SingleFile;
		private ExportTarget _exportTarget = _defaultExportTarget;

		[DefaultValue(_defaultExportTarget)]
		public ExportTarget ExportTarget
		{
			get { return _exportTarget; }
			set { _exportTarget = value; }
		}

		[DefaultValue(false)]
		public bool FilterRows { get; set; }

		[DefaultValue(false)]
		public bool MatchCase { get; set; }

		[DefaultValue(false)]
		public bool ExportModelsAsParentNodes { get; set; }

		[DefaultValue(false)]
		public bool ExportBidirectionalDependenciesAsUndirectedEdges { get; set; }

		[DefaultValue(false)]
		public bool IncludeSelfDependencies { get; set; }
	}
}
