using System;
using System.Threading.Tasks;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Framework;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyLastCmdBase : ButtonCommandBase
	{
		//TODO: improve enabling: disable if qaEnvironment.LastVerificationPerimeter is null
		protected VerifyLastCmdBase()
		{
			// Instead of wiring each single button and tool and calling SessionContext.CanVerifyQuality
			// for each one, the singleton event aggregator updates all at once:
			Register();
		}

		private void Register()
		{
			VerificationPlugInController.GetInstance(SessionContext).Register(this);
		}

		protected abstract IVerificationSessionContext SessionContext { get; }

		protected abstract IWorkListOpener WorkListOpener { get; }

		protected virtual
			Func<IQualityVerificationResult, ErrorDeletionInPerimeter, bool, Task<int>>
			SaveAction => null;

		protected override Task<bool> OnClickAsyncCore()
		{
			return VerifyUtils.CreateCommandRunner(SessionContext, WorkListOpener, SaveAction)
			                  .VerifyLast();
		}
	}
}
