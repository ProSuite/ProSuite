using System;
using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	[CLSCompliant(false)]
	public interface IIssueRepository : IDisposable
	{
		[CanBeNull]
		IIssueGeometryTransformation IssueGeometryTransformation { get; set; }

		void AddIssue([NotNull] Issue issue, [CanBeNull] IGeometry issueGeometry);

		void CreateIndexes([CanBeNull] ITrackCancel trackCancel, bool ignoreErrors = false);

		[NotNull]
		IEnumerable<IIssueDataset> IssueDatasets { get; }

		[NotNull]
		IFeatureWorkspace FeatureWorkspace { get; }
	}
}
