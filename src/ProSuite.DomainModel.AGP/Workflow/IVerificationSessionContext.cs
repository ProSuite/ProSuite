using System;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.Core.Workflow;

namespace ProSuite.DomainModel.AGP.Workflow;

public interface IVerificationSessionContext : ISessionContext
{
	IQualityVerificationEnvironment VerificationEnvironment { get; }

	event EventHandler QualitySpecificationsRefreshed;

	/// <summary>
	/// Whether the current quality specification can be verified or not and, if it cannot
	/// be verified the reason why.
	/// </summary>
	/// <param name="reason"></param>
	/// <returns></returns>
	bool CanVerifyQuality(out string reason);
}
