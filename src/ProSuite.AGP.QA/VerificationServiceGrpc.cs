using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.AGP.QA
{
	/// <summary>
	/// gRPC microservice based implementation for quality verifications.
	/// </summary>
	public class VerificationServiceGrpc : VerificationServiceBase
	{
		[NotNull] private readonly QualityVerificationServiceClient _client;
		private const string _contextTypeWorkUnit = "Work Unit";
		private const string _contextTypePerimeter = "Perimeter";

		public VerificationServiceGrpc([NotNull] QualityVerificationServiceClient client)
		{
			Assert.ArgumentNotNull(client, nameof(client));

			_client = client;
		}

		public override async Task<ServiceCallStatus> VerifyPerimeter(
			IQualitySpecificationReference qualitySpecificationRef,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			Assert.ArgumentNotNull(qualitySpecificationRef, nameof(qualitySpecificationRef));
			Assert.ArgumentNotNull(perimeter, nameof(perimeter));
			Assert.ArgumentNotNull(projectWorkspace, nameof(projectWorkspace));
			Assert.ArgumentNotNull(progress, nameof(progress));

			QualitySpecificationReference specificationRef =
				qualitySpecificationRef as QualitySpecificationReference;

			Assert.NotNull(specificationRef, "Unexpected type of quality specification");

			VerificationRequest request =
				await CreateVerificationRequest(specificationRef, perimeter, projectWorkspace,
				                                resultsPath);

			return await QAUtils.Verify(Assert.NotNull(_client.QaGrpcClient), request, progress);
		}

		public override async Task<ServiceCallStatus> VerifyPerimeter(
			QualitySpecification qualitySpecification,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(perimeter, nameof(perimeter));
			Assert.ArgumentNotNull(projectWorkspace, nameof(projectWorkspace));
			Assert.ArgumentNotNull(progress, nameof(progress));

			VerificationRequest request =
				await CreateVerificationRequest(qualitySpecification, perimeter, projectWorkspace,
				                                resultsPath);

			return await QAUtils.Verify(Assert.NotNull(_client.QaGrpcClient), request, progress);
		}

		public override async Task<ServiceCallStatus> VerifySelection(
			IQualitySpecificationReference qualitySpecificationRef,
			IList<Row> objectsToVerify,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			QualitySpecificationReference specification =
				qualitySpecificationRef as QualitySpecificationReference;

			Assert.NotNull(specification, "Unexpected type of quality specification");

			VerificationRequest request =
				await CreateVerificationRequest(specification, perimeter, projectWorkspace,
				                                resultsPath, objectsToVerify);

			return await QAUtils.Verify(Assert.NotNull(_client.QaGrpcClient), request, progress);
		}

		public override async Task<ServiceCallStatus> VerifySelection(
			QualitySpecification qualitySpecification,
			IList<Row> objectsToVerify,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(projectWorkspace, nameof(projectWorkspace));
			Assert.ArgumentNotNull(progress, nameof(progress));

			VerificationRequest request =
				await CreateVerificationRequest(qualitySpecification, perimeter, projectWorkspace,
				                                resultsPath, objectsToVerify);

			return await QAUtils.Verify(Assert.NotNull(_client.QaGrpcClient), request, progress);
		}

		private async Task<VerificationRequest> CreateVerificationRequest(
			[NotNull] IQualitySpecificationReference specificationRef,
			[CanBeNull] Geometry perimeter,
			[NotNull] ProjectWorkspace projectWorkspace,
			[CanBeNull] string resultsPath,
			[CanBeNull] IList<Row> objectsToVerify = null)
		{
			string projectName = Project.Current.Name;

			VerificationRequest request =
				await QueuedTask.Run(
					() =>
					{
						var result = QAUtils.CreateRequest(projectWorkspace, _contextTypePerimeter,
						                                   projectName, specificationRef.Id,
						                                   perimeter);

						QAUtils.SetObjectsToVerify(result, objectsToVerify, projectWorkspace);

						return result;
					});

			SetPathParameters(resultsPath, request);

			QAUtils.SetVerificationParameters(
				request, GetTileSize(projectWorkspace), false, true, false);

			return request;
		}

		private async Task<VerificationRequest> CreateVerificationRequest(
			[NotNull] QualitySpecification specification,
			[CanBeNull] Geometry perimeter,
			[NotNull] ProjectWorkspace projectWorkspace,
			[CanBeNull] string resultsPath,
			[CanBeNull] IList<Row> objectsToVerify = null)
		{
			string projectName = Project.Current.Name;

			VerificationRequest request =
				await QueuedTask.Run(
					() =>
					{
						VerificationRequest result =
							QAUtils.CreateRequest(
								projectWorkspace, _contextTypePerimeter, projectName, specification,
								perimeter);

						QAUtils.SetObjectsToVerify(result, objectsToVerify, projectWorkspace);

						return result;
					});

			SetPathParameters(resultsPath, request);

			QAUtils.SetVerificationParameters(
				request, GetTileSize(projectWorkspace), false, true, false);

			return request;
		}

		private void SetPathParameters(string resultsPath, VerificationRequest request)
		{
			if (! string.IsNullOrEmpty(resultsPath))
			{
				string htmlReport = Path.Combine(resultsPath, HtmlReportName);
				string xmlReport = Path.Combine(resultsPath, VerificationReportName);
				string gdbDir = Path.Combine(resultsPath, "issues.gdb");

				request.Parameters.HtmlReportPath = htmlReport;
				request.Parameters.VerificationReportPath = xmlReport;
				request.Parameters.IssueFileGdbPath = gdbDir;
			}
		}

		private double GetTileSize(ProjectWorkspace projectWorkspace)
		{
			// TODO
			return -1;
		}
	}
}
