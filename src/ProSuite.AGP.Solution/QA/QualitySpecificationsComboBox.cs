using ProSuite.AGP.QA.ProPlugins;
using ProSuite.DomainModel.AGP.QA;

namespace ProSuite.AGP.Solution.QA
{
	sealed class QualitySpecificationsComboBox : QualitySpecificationsComboBoxBase
	{
		public QualitySpecificationsComboBox() : base() { }

		protected override IQualityVerificationEnvironment QualityVerificationEnvironment =>
			ProSuiteToolsModule.Current.QualityVerificationEnvironment;
	}
}
