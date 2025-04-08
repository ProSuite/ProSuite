using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.VerificationReports
{
	public class InvolvedDatasetsCollector : IVerificationReportBuilder
	{
		private readonly SimpleSet<Dataset> _involvedDatasets = new SimpleSet<Dataset>();

		private readonly SimpleSet<string> _processedQualityConditions =
			new SimpleSet<string>(StringComparer.Ordinal);

		[NotNull]
		public ICollection<Dataset> InvolvedDatasets => _involvedDatasets;

		public void BeginVerification(AreaOfInterest areaOfInterest) { }

		public void AddVerifiedDataset(QualityVerificationDataset verificationDataset,
		                               string workspaceDisplayText,
		                               ISpatialReference spatialReference) { }

		public void AddIssue(Issue issue, IGeometry errorGeometry)
		{
			QualityCondition qualityCondition = issue.QualityCondition;

			if (_processedQualityConditions.Contains(qualityCondition.Name))
			{
				return;
			}

			_processedQualityConditions.Add(qualityCondition.Name);

			foreach (Dataset dataset in qualityCondition.GetDatasetParameterValues())
			{
				_involvedDatasets.TryAdd(dataset);
			}
		}

		public void AddRowsWithStopConditions(
			IEnumerable<RowWithStopCondition> rowsWithStopCondition) { }

		public void AddExceptionStatistics(IExceptionStatistics statistics) { }

		public void EndVerification(bool cancelled) { }

		public void AddVerifiedQualityCondition(
			QualitySpecificationElement qualitySpecificationElement) { }
	}
}
