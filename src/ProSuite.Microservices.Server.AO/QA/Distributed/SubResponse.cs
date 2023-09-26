using System;
using System.Collections.Concurrent;
using ProSuite.Commons.Progress;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	/// <summary>
	/// Thread safe container for the results of a sub-verification run of a parallel verification.
	/// </summary>
	public class SubResponse
	{
		public QualityVerificationMsg VerificationMsg { get; set; }

		public ServiceCallStatus Status { get; set; }

		public ConcurrentBag<IssueMsg> Issues { get; } = new ConcurrentBag<IssueMsg>();

		public int ProgressTotal { get; set; }
		public int ProgressCurrent { get; set; }

		public double ProgressRatio => (double) ProgressCurrent / ProgressTotal;

		public EnvelopeMsg CurrentBox { get; set; }

		public string CancellationMessage { get; set; }

		public DateTime LastProgressLog { get; set; }
	}
}
