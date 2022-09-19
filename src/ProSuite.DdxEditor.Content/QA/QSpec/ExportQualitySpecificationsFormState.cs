using System.ComponentModel;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	[UsedImplicitly]
	public class ExportQualitySpecificationsFormState : FormState
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
		public bool ExportWorkspaceConnectionStrings { get; set; }

		[DefaultValue(false)]
		public bool ExportSdeConnectionFilePaths { get; set; }

		[DefaultValue(false)]
		public bool ExportMetadata { get; set; }

		[DefaultValue(false)]
		public bool ExportAllTestDescriptors { get; set; }

		[DefaultValue(false)]
		public bool ExportAllCategories { get; set; }

		[DefaultValue(false)]
		public bool ExportNotes { get; set; }
	}
}
