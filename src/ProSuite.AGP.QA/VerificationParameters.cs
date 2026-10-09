using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.QA
{
	/// <summary>
	/// Transport of the parameters that shape a single verification request: where results are
	/// written, which artifacts to produce and whether to persist statistics. Built by the
	/// verification environment (typically from the application-specific options plus per-run
	/// caller input) and consumed by <see cref="VerificationServiceBase"/>.
	/// </summary>
	public class VerificationParameters
	{
		/// <param name="resultsPath">The base folder for results (reports and issue.gdb), or
		/// <c>null</c> to let the service use its default location.</param>
		public VerificationParameters([CanBeNull] string resultsPath)
		{
			ResultsPath = resultsPath;
		}

		/// <summary>
		/// The base folder for verification results (reports and issue.gdb), or <c>null</c> to use
		/// the service default.
		/// </summary>
		[CanBeNull]
		public string ResultsPath { get; }

		/// <summary>
		/// Whether the verification reports (HTML / XML) are written.
		/// </summary>
		public bool CreateReports { get; set; } = true;

		/// <summary>
		/// Whether a local Issue File Geodatabase is created.
		/// </summary>
		public bool CreateLocalIssueFileGdb { get; set; } = true;

		/// <summary>
		/// Whether the verification statistics are saved in the data dictionary (DDX). This is a
		/// per-run flag supplied by the caller (e.g. work unit / release cycle verification).
		/// </summary>
		public bool SaveVerificationStatisticsInDdx { get; set; }
	}
}
