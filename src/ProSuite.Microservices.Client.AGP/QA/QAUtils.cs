using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Progress;
using ProSuite.DomainModel.AGP.DataModel;
using ProSuite.DomainModel.AGP.QA;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public static class QAUtils
	{
		/// <summary>
		/// Creates the verification request for an extent verification.
		/// </summary>
		/// <param name="projectWorkspace"></param>
		/// <param name="contextType"></param>
		/// <param name="contextName"></param>
		/// <param name="qualitySpecification">The desired quality specification.</param>
		/// <param name="perimeter">The verification perimeter or null for a work unit verification.</param>
		/// <returns></returns>
		public static VerificationRequest CreateRequest(
			[NotNull] ProjectWorkspace projectWorkspace,
			[NotNull] string contextType,
			[NotNull] string contextName,
			[NotNull] QualitySpecificationReference qualitySpecification,
			[CanBeNull] Geometry perimeter)
		{
			const int workContextTypeProject = 1;
			var workContextMsg = new WorkContextMsg
			                     {
				                     DdxId = projectWorkspace.ProjectId,
				                     Type = workContextTypeProject,
				                     ContextType = contextType,
				                     ContextName = contextName,
				                     VersionName = projectWorkspace.GetVersionName() ?? string.Empty
			                     };

			workContextMsg.VerifiedDatasetIds.AddRange(projectWorkspace.GetDatasetIds());

			var specificationMsg = new QualitySpecificationMsg
			                       {
				                       QualitySpecificationId = qualitySpecification.Id
			                       };

			return CreateRequest(workContextMsg, specificationMsg, perimeter);
		}

		/// <summary>
		/// Creates the verification request using the specified work context message.
		/// </summary>
		/// <param name="workContextMsg">The pre-assembled work context message.</param>
		/// <param name="qualitySpecificationMsg">The pre-assembled quality specification message.</param>
		/// <param name="perimeter">The verification perimeter or null to verify the entire work
		/// context.</param>
		/// <returns></returns>
		public static VerificationRequest CreateRequest(
			[NotNull] WorkContextMsg workContextMsg,
			[NotNull] QualitySpecificationMsg qualitySpecificationMsg,
			[CanBeNull] Geometry perimeter)
		{
			var request = new VerificationRequest();

			request.WorkContext = workContextMsg;

			request.Specification = qualitySpecificationMsg;

			request.Parameters = new VerificationParametersMsg();

			if (perimeter != null && ! perimeter.IsEmpty)
			{
				ShapeMsg areaOfInterest = ProtobufConversionUtils.ToShapeMsg(perimeter);

				request.Parameters.Perimeter = areaOfInterest;
			}

			//if (objectsToVerify != null)
			//{
			//	Assert.NotNull(datasetLookup, nameof(datasetLookup));

			//	foreach (IObject objToVerify in objectsToVerify)
			//	{
			//		IGeometry geometry = null;
			//		if (objToVerify is IFeature feature)
			//		{
			//			geometry = feature.Shape;
			//		}

			//		ObjectDataset objectDatset = datasetLookup.GetDataset(objToVerify);

			//		if (objectDatset != null)
			//		{
			//			request.Features.Add(
			//				ProtobufGdbUtils.ToGdbObjectMsg(
			//					objToVerify, geometry, objectDatset.Id,
			//					SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml));
			//		}
			//	}
			//}

			request.UserName = EnvironmentUtils.UserDisplayName;

			return request;
		}

		public static void SetObjectsToVerify([NotNull] VerificationRequest request,
		                                      [CanBeNull] IList<Row> objectsToVerify,
		                                      [NotNull] ProjectWorkspace projectWorkspace)
		{
			if (objectsToVerify == null || objectsToVerify.Count == 0)
			{
				return;
			}

			DatasetLookup datasetLookup = projectWorkspace.GetDatasetLookup();

			Assert.NotNull(datasetLookup, nameof(datasetLookup));

			foreach (Row objToVerify in objectsToVerify)
			{
				Geometry geometry = null;
				if (objToVerify is Feature feature)
				{
					geometry = feature.GetShape();
				}

				BasicDataset objectDatset = datasetLookup.GetDataset(objToVerify.GetTable());

				if (objectDatset != null)
				{
					request.Features.Add(
						ProtobufConversionUtils.ToGdbObjectMsg(
							objToVerify, geometry, objectDatset.Id, false));
				}
			}
		}

		public static void SetVerificationParameters(
			[NotNull] VerificationRequest request,
			double tileSize,
			bool saveVerification,
			bool filterTableRowsUsingRelatedGeometry,
			bool invalidateExceptionsIfAnyInvolvedObjectChanged,
			bool invalidateExceptionsIfConditionWasUpdated = false)
		{
			request.Parameters.TileSize = tileSize;

			// Save verification if it's the full work unit / release cycle
			request.Parameters.SaveVerificationStatistics = saveVerification;

			// ErrorCreation.UseReferenceGeometries (not for WU verification!)
			request.Parameters.FilterTableRowsUsingRelatedGeometry =
				filterTableRowsUsingRelatedGeometry;

			// ErrorCreation.IgnoreAllowedErrors translates to 
			request.Parameters.OverrideAllowedErrors = false;

			// Always report unused (and it has no performance impact)
			request.Parameters.ReportUnusedExceptions = true;

			// Invalid could be
			// - Involved row has been deleted (always determined)
			// - InvalidateAllowedErrorsIfAnyInvolvedObjectChanged (see below)
			// - InvalidateAllowedErrorsIfQualityConditionWasUpdated (see below)
			request.Parameters.ReportInvalidExceptions = true;

			request.Parameters.InvalidateExceptionsIfConditionWasUpdated =
				invalidateExceptionsIfConditionWasUpdated;

			request.Parameters.InvalidateExceptionsIfAnyInvolvedObjectChanged =
				invalidateExceptionsIfAnyInvolvedObjectChanged;
		}

		public static async Task<ServiceCallStatus> Verify(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			[NotNull] VerificationRequest request,
			[NotNull] QualityVerificationProgressTracker progress)
		{
			ClientIssueMessageCollector clientIssueMessageRepository =
				new ClientIssueMessageCollector();

			BackgroundVerificationRun verificationRun =
				CreateQualityVerificationRun(request, clientIssueMessageRepository, progress);

			return await verificationRun.ExecuteAndProcessMessagesAsync(qaClient);
		}

		public static BackgroundVerificationRun CreateQualityVerificationRun(
			[NotNull] VerificationRequest request,
			IClientIssueMessageCollector clientIssueMessageRepository,
			QualityVerificationProgressTracker progressTracker)
		{
			void SaveAction(IQualityVerificationResult verificationResult,
			                ErrorDeletionInPerimeter errorDeletion,
			                bool updateLatestTestDate) { }

			BackgroundVerificationRun verificationRun =
				new BackgroundVerificationRun(request, null,
				                              null,
				                              null, progressTracker)
				{
					ResultIssueCollector = clientIssueMessageRepository,
					SaveAction = SaveAction
				};

			return verificationRun;
		}
	}
}
