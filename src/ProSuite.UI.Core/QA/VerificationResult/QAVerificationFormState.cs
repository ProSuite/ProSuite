using System.ComponentModel;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.Commons.UI.WinForms.Controls;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	public class QAVerificationFormState : FormState
	{
		[XmlAttribute("listHeight")]
		[DefaultValue(-1)]
		public int ListHeight { get; set; }

		[XmlAttribute("listWidth")]
		[DefaultValue(-1)]
		public int ListWidth { get; set; }

		[XmlAttribute("activeMode")]
		[DefaultValue(QAVerificationDisplayMode.Plain)]
		public QAVerificationDisplayMode ActiveMode { get; set; } =
			QAVerificationDisplayMode.Plain;

		[XmlAttribute("showFulfilled")]
		public bool ShowFulfilled { get; set; }

		[XmlAttribute("showWarning")]
		public bool ShowWarning { get; set; }

		[XmlAttribute("showError")]
		public bool ShowFailed { get; set; }

		[XmlAttribute("showChildTimes")]
		public bool ShowChildTimes { get; set; }

		[XmlAttribute("showLoadTimes")]
		public bool ShowLoadTimes { get; set; }

		[XmlAttribute("matchCase")]
		public bool MatchCase { get; set; }

		[XmlAttribute("filterRows")]
		public bool FilterRows { get; set; }

		[CanBeNull]
		public DataGridViewSortState VerifiedConditionsSortState { get; set; }

		[CanBeNull]
		public DataGridViewSortState VerifiedDatasetsSortState { get; set; }
	}
}
