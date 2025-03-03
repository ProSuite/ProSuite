using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.VerificationReports.Xml;

namespace ProSuite.DomainServices.AO.QA.HtmlReports
{
	public class HtmlVerifiedDataset
	{
		[NotNull]
		public string Name { get; }

		[CanBeNull]
		public string WorkspaceName { get; }

		public string DatasetType { get; set; }

		public int WarningCount { get; set; }

		public int ErrorCount { get; set; }

		public int StopErrorCount { get; set; }

		/// <summary>
		/// The number of conditions in which this dataset is involved.
		/// </summary>
		public int VerifiedConditionCount { get; set; }

		public double Resolution { get; set; }

		public double Tolerance { get; set; }

		public string CoordinateSystem { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="HtmlVerifiedDataset"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="workspaceName">Name of the workspace.</param>
		public HtmlVerifiedDataset([NotNull] string name,
		                           [CanBeNull] string workspaceName)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			WorkspaceName = workspaceName;
		}

		public HtmlVerifiedDataset(XmlVerifiedDataset xmlVerifiedDataset)
			: this(xmlVerifiedDataset.Name, xmlVerifiedDataset.WorkspaceName)
		{
			WarningCount = xmlVerifiedDataset.WarningCount;
			ErrorCount = xmlVerifiedDataset.ErrorCount;
			StopErrorCount = xmlVerifiedDataset.StopErrorCount;

			CoordinateSystem = xmlVerifiedDataset.CoordinateSystem;
			Tolerance = xmlVerifiedDataset.Tolerance;
			Resolution = xmlVerifiedDataset.Resolution;

			VerifiedConditionCount = xmlVerifiedDataset.VerifiedConditionCount;

			DatasetType = xmlVerifiedDataset.GeometryType;
		}
	}
}
