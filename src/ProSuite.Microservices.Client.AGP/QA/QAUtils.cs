using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.VerificationProgress;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.AGP.QA
{
	public static class QAUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Creates the verification request for an extent verification.
		/// </summary>
		/// <param name="projectWorkspace"></param>
		/// <param name="contextType"></param>
		/// <param name="contextName"></param>
		/// <param name="qualitySpecificationId">The desired quality specification's Id.</param>
		/// <param name="perimeter">The verification perimeter or null for a work unit verification.</param>
		/// <returns></returns>
		public static VerificationRequest CreateRequest(
			[NotNull] ProjectWorkspace projectWorkspace,
			[NotNull] string contextType,
			[NotNull] string contextName,
			int qualitySpecificationId,
			[CanBeNull] Geometry perimeter)
		{
			WorkContextMsg workContextMsg =
				CreateWorkContextMsg(projectWorkspace, contextType, contextName);

			var specificationMsg = new QualitySpecificationMsg
			                       {
				                       QualitySpecificationId = qualitySpecificationId
			                       };

			return CreateRequest(workContextMsg, specificationMsg, perimeter);
		}

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
			[NotNull] QualitySpecification qualitySpecification,
			[CanBeNull] Geometry perimeter)
		{
			WorkContextMsg workContextMsg =
				CreateWorkContextMsg(projectWorkspace, contextType, contextName);

			CustomQualitySpecification customSpecification =
				(CustomQualitySpecification) qualitySpecification;

			int specificationId = customSpecification.BaseSpecification.Id;

			var specificationMsg = new QualitySpecificationMsg
			                       {
				                       QualitySpecificationId = specificationId
			                       };

			specificationMsg.ExcludedConditionIds.AddRange(
				customSpecification.GetDisabledConditions().Select(c => c.Id));

			return CreateRequest(workContextMsg, specificationMsg, perimeter);
		}

		// TODO: Full specification, once the actual parameter changes are supported:

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

			foreach (Row objToVerify in objectsToVerify)
			{
				Table table = DatasetUtils.GetDatabaseTable(objToVerify.GetTable());

				if (table.GetDatastore().Handle != projectWorkspace.Datastore.Handle)
				{
					_msg.VerboseDebug(() => $"The object {GdbObjectUtils.ToString(objToVerify)} " +
					                        $"is not part of the project workspace");
					continue;
				}

				Geometry geometry = null;
				if (objToVerify is Feature feature)
				{
					geometry = feature.GetShape();
				}

				IDdxDataset objectDataset = projectWorkspace.GetDataset(table.GetName());

				if (objectDataset != null)
				{
					request.Features.Add(
						ProtobufConversionUtils.ToGdbObjectMsg(
							objToVerify, geometry, objectDataset.Id, false));
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

		private static WorkContextMsg CreateWorkContextMsg(
			[NotNull] ProjectWorkspace projectWorkspace,
			string contextType,
			string contextName)
		{
			const int workContextTypeProject = 1;

			// NOTE: If it is not a child workspace we should absolutely not include the path
			//       because this triggers the notoriously slow GetDatasetMappings() method.
			bool includePath = ! projectWorkspace.IsMasterDatabaseWorkspace;

			WorkspaceMsg workspaceMsg =
				ProtobufConversionUtils.ToWorkspaceRefMsg(projectWorkspace.Datastore, includePath);

			var workContextMsg = new WorkContextMsg
			                     {
				                     DdxId = projectWorkspace.ProjectId,
				                     Type = workContextTypeProject,
				                     ContextType = contextType,
				                     ContextName = contextName,
				                     Workspace = workspaceMsg,
				                     VersionName = projectWorkspace.GetVersionName() ?? string.Empty
			                     };

			workContextMsg.VerifiedDatasetIds.AddRange(projectWorkspace.GetDatasetIds());

			return workContextMsg;
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
