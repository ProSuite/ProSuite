using System;
using System.Collections.Generic;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;

namespace ProSuite.AGP.QA.ProPlugins
{
	/// <summary>
	/// An event aggregator that controls the Enabled reason of all registered tools and buttons using
	/// when it is notified on a change in the verification environment and especially sets the
	/// appropriate disabled reason. This prevents all individual tools to be forced to listen to
	/// the events and each call the CanVerify() method individually.
	/// </summary>
	public class VerificationPlugInController
	{
		private static VerificationPlugInController _instance;

		private readonly IMapBasedSessionContext _sessionContext;

		private readonly List<PlugIn> _verificationPlugins = new List<PlugIn>();

		public VerificationPlugInController([NotNull] IMapBasedSessionContext sessionContext)
		{
			_sessionContext = sessionContext;
			WireEvent();
		}

		public static VerificationPlugInController GetInstance(
			[NotNull] IMapBasedSessionContext sessionContext)
		{
			if (_instance == null)
			{
				_instance = new VerificationPlugInController(sessionContext);
			}

			return _instance;
		}

		public void Register([NotNull] PlugIn verificationCommandOrTool)
		{
			if (! _verificationPlugins.Contains(verificationCommandOrTool))
			{
				_verificationPlugins.Add(verificationCommandOrTool);

				SetInitialState(verificationCommandOrTool);
			}
		}

		private static void SetInitialState([NotNull] PlugIn verificationCommandOrTool)
		{
			verificationCommandOrTool.Enabled = false;
			verificationCommandOrTool.DisabledTooltip =
				"The verification environment is not yet fully initialized";
		}

		public bool UnRegister(PlugIn verificationCommandOrTool)
		{
			return _verificationPlugins.Remove(verificationCommandOrTool);
		}

		private void QualitySpecificationsRefreshed(object sender, EventArgs e)
		{
			bool enabled = _sessionContext.CanVerifyQuality(out string disabledReason);

			foreach (PlugIn plugin in _verificationPlugins)
			{
				plugin.Enabled = enabled;
				plugin.DisabledTooltip = disabledReason;
			}
		}

		private void WireEvent()
		{
			_sessionContext.QualitySpecificationsRefreshed += QualitySpecificationsRefreshed;
		}
	}
}