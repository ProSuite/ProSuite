using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.QA.Core.IssueCodes;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Tests.Documentation;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.QA.Tests.Schema;

namespace ProSuite.QA.Tests
{
	[SchemaTest]
	public class QaGdbRelease : QaSchemaTestBase
	{
		private readonly IReadOnlyTable _table;
		private readonly VersionSpecification _minimumVersion;
		private readonly VersionSpecification _maximumVersion;
		private readonly bool _singleVersion;

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes => _codes ?? (_codes = new Code());

		private class Code : LocalTestIssueCodes
		{
			public const string UnableToDetermineRelease = "UnableToDetermineRelease";
			public const string IncorrectVersion_TooLow = "IncorrectVersion.TooLow";
			public const string IncorrectVersion_TooHigh = "IncorrectVersion.TooHigh";

			public Code() : base("GeodatabaseRelease") { }
		}

		#endregion

		[UsedImplicitly]
		[Doc(nameof(DocStrings.QaGdbRelease_0))]
		public QaGdbRelease(
			[Doc(nameof(DocStrings.QaGdbRelease_table))] [NotNull] IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaGdbRelease_expectedVersion))] [CanBeNull]
			string
				expectedVersion)
			: this(table, expectedVersion, expectedVersion) { }

		[UsedImplicitly]
		[Doc(nameof(DocStrings.QaGdbRelease_1))]
		public QaGdbRelease(
			[Doc(nameof(DocStrings.QaGdbRelease_table))] [NotNull] IReadOnlyTable table,
			[Doc(nameof(DocStrings.QaGdbRelease_minimumVersion))] [CanBeNull]
			string
				minimumVersion,
			[Doc(nameof(DocStrings.QaGdbRelease_maximumVersion))] [CanBeNull]
			string
				maximumVersion)
			: base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			_table = table;

			_minimumVersion = GetVersionSpecification(minimumVersion);
			_maximumVersion = GetVersionSpecification(maximumVersion);

			Assert.ArgumentCondition(_minimumVersion != null || _maximumVersion != null,
			                         "Version not specified");

			_singleVersion = _minimumVersion != null && _maximumVersion != null &&
			                 Equals(_minimumVersion, _maximumVersion);
		}

		[InternallyUsedTest]
		public QaGdbRelease([NotNull] QaGdbReleaseDefinition definition)
			: this((IReadOnlyTable)definition.Table,
			       definition.MinimumVersion,
			       definition.MaximumVersion)
		{ }

		public override int Execute()
		{
			IWorkspace workspace = _table.Workspace;

			IGeodatabaseRelease gdbRelease;
			if (! WorkspaceUtils.HasGeodatabaseReleaseInformation(workspace, out gdbRelease))
			{
				return ReportSchemaError(Codes[Code.UnableToDetermineRelease],
				                         "Unable to determine geodatabase release");
			}

			// gdb releases startet at 1 for arcgis 8 (2 -> 9, 3 -> 10)
			int major = gdbRelease.MajorVersion + 7; // --> offset by 7
			int minor = gdbRelease.MinorVersion;
			int bugfix = gdbRelease.BugfixVersion;

			bool belowMinimum = _minimumVersion != null &&
			                    _minimumVersion.IsGreaterThan(major, minor, bugfix);
			bool aboveMaximum = _maximumVersion != null &&
			                    _maximumVersion.IsLowerThan(major, minor, bugfix);

			string versionString = string.Format("{0}.{1}.{2}", major, minor, bugfix);

			if (belowMinimum)
			{
				return ReportSchemaError(
					Codes[Code.IncorrectVersion_TooLow],
					_singleVersion
						? "The geodatabase release version ({0}) is lower than the expected version ({1})"
						: "The geodatabase release version ({0}) is lower than the minimum version ({1})",
					versionString, _minimumVersion.VersionString);
			}

			if (aboveMaximum)
			{
				return ReportSchemaError(
					Codes[Code.IncorrectVersion_TooHigh],
					_singleVersion
						? "The geodatabase release version ({0}) is higher than the expected version ({1})"
						: "The geodatabase release version ({0}) is higher than the maximum version ({1})",
					versionString, _maximumVersion.VersionString);
			}

			return NoError;
		}

		[CanBeNull]
		private static VersionSpecification GetVersionSpecification(
			[CanBeNull] string versionString)
		{
			return versionString == null || StringUtils.IsNullOrEmptyOrBlank(versionString)
				       ? null
				       : VersionSpecification.Create(versionString);
		}
	}
}
