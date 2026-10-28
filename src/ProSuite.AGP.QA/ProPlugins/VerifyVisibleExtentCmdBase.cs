using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;

namespace ProSuite.AGP.QA.ProPlugins
{
	public abstract class VerifyVisibleExtentCmdBase : ButtonCommandBase
	{
		protected VerifyVisibleExtentCmdBase()
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

		/// <summary>
		/// Returns the perimeter to verify, derived from the visible extent, for example
		/// intersected with the current work unit.
		/// </summary>
		/// <param name="visibleExtent">The visible extent of the active map view.</param>
		/// <returns>The perimeter to verify, or null if nothing is to be verified. In that
		/// case the implementation has informed the user.</returns>
		[CanBeNull]
		protected virtual Geometry GetPerimeterToVerify([NotNull] Envelope visibleExtent)
		{
			return visibleExtent;
		}

		protected override Task<bool> OnClickAsyncCore()
		{
			return VerifyUtils.CreateCommandRunner(SessionContext, WorkListOpener, SaveAction)
			                  .VerifyVisibleExtent(GetPerimeterToVerify);
		}
	}
}
