using System;

namespace ProSuite.DomainModel.AO.QA
{
	[Flags]
	public enum ErrorDeletion
	{
		All = 0,
		VerifiedConditions = 1,
		KeepChangedAllowedErrors = 2,
		KeepUnusedAllowedErrors = 4
	}
}
