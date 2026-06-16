using System.Threading.Tasks;
using System.Windows.Forms;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Env;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.UI.Core.QA.VerificationResult;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class ShowLastVerificationCmdBase : ButtonCommandBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected abstract IVerificationSessionContext SessionContext { get; }

		protected override void OnUpdateCore()
		{
			IQualityVerificationEnvironment env = SessionContext?.VerificationEnvironment;

			Enabled = env?.LastVerificationResult != null;

			if (! Enabled)
			{
				DisabledTooltip = "No verification has been run in this session.";
			}
		}

		protected override async Task<bool> OnClickAsyncCore()
		{
			IQualityVerificationEnvironment env = SessionContext?.VerificationEnvironment;

			if (env == null)
			{
				return false;
			}

			var result = env.LastVerificationResult as BackgroundVerificationResult;

			if (result?.VerificationMsg == null)
			{
				return false;
			}

			int specId = result.VerificationMsg.SpecificationId;

			QualitySpecification spec = await env.GetQualitySpecification(specId);

			if (spec == null)
			{
				_msg.Warn($"Quality specification {specId} could not be loaded.");
				return false;
			}

			QualityVerification verification = result.GetQualityVerification(spec);

			var form = new QAVerificationForm(new NopDomainTransactionManager());
			form.SetVerification(verification, verification.ContextType, verification.ContextName);
			form.StartPosition = FormStartPosition.CenterScreen;
			UIEnvironment.ShowDialog(form);

			return true;
		}
	}
}
