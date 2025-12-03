using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.UI.Core.QA.VerificationResult
{
	public partial class QualityConditionVerificationControl : UserControl
	{
		private QualityConditionVerification _verification;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionVerificationControl"/> class.
		/// </summary>
		public QualityConditionVerificationControl()
		{
			InitializeComponent();
		}

		public void SetCondition(
			[NotNull] QualityConditionVerification conditionVerification)
		{
			Assert.ArgumentNotNull(conditionVerification, nameof(conditionVerification));

			_verification = conditionVerification;

			QualityCondition displayableCondition = conditionVerification.DisplayableCondition;

			_qualityConditionControl.QualityCondition = displayableCondition;
			_qualityConditionTableViewControl.SetQualityCondition(displayableCondition);
			_testDescriptorControl.TestDescriptor = displayableCondition.TestDescriptor;

			_textBoxIssueCount.Text = _verification.ErrorCount.ToString("N0");

			_textBoxIssueCount.BackColor = GetErrorBackColor(conditionVerification);

			_textBoxIssueType.Text = _verification.AllowErrors
				                         ? "Warning"
				                         : "Error";

			// TODO: Handle StopOnError enumeration
			_textBoxStopCondition.Text = _verification.StopCondition == null
				                             ? "None"
				                             : string.Format("Stopped after error in {0}",
				                                             _verification.StopCondition.Name);
			//else if (_verification.StopCondition == _verification.QualityCondition)
			//{
			//    _textBoxStopCondition.Text = "Stopped for this and following tests";
			//}
		}

		private static Color GetErrorBackColor(
			[NotNull] QualityConditionVerification conditionVerification)
		{
			// TODO remove redundancy with QAVerificationForm (same colors used there)

			if (conditionVerification.ErrorCount == 0)
			{
				return Color.LightGreen;
			}

			return conditionVerification.AllowErrors
				       ? Color.Yellow
				       : Color.FromArgb(255, 100, 100);
		}

		public void Clear()
		{
			_textBoxIssueCount.Text = string.Empty;
			_textBoxIssueType.Text = string.Empty;
			_textBoxStopCondition.Text = string.Empty;

			_qualityConditionControl.QualityCondition = null;
			_qualityConditionTableViewControl.SetQualityCondition(null);

			_textBoxIssueCount.BackColor = SystemColors.Control;
		}
	}
}
