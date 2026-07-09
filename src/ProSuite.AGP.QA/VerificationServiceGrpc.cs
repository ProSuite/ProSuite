using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
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
	public abstract class VerificationServiceGrpc : VerificationServiceBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IQualityVerificationClient _client;

		private const string _defaultContextType = "Perimeter";

		protected VerificationServiceGrpc([NotNull] IQualityVerificationClient client,
		                                  [CanBeNull] string contextType = null,
		                                  [CanBeNull] string contextName = null)
		{
			Assert.ArgumentNotNull(client, nameof(client));

			_client = client;
			ContextType = contextType;
			ContextName = contextName;
		}

		[CanBeNull]
		public string DdxEnvironmentName { get; set; }

		[CanBeNull]
		public string ContextType { get; }

		[CanBeNull]
		public string ContextName { get; }

		/// <summary>
		/// Forces the client to provide the verified data from its edit session, regardless of
		/// whether unsaved edits or a branch version are detected. Intended for testing/forcing.
		/// </summary>
		public bool AlwaysUseClientData { get; set; }

		public override async Task<ServiceCallStatus> Verify(
			IQualitySpecificationReference qualitySpecificationRef,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath,
			bool saveVerification = false)
		{
			Assert.ArgumentNotNull(qualitySpecificationRef, nameof(qualitySpecificationRef));
			Assert.ArgumentNotNull(projectWorkspace, nameof(projectWorkspace));
			Assert.ArgumentNotNull(progress, nameof(progress));

			QualitySpecificationReference specificationRef =
				qualitySpecificationRef as QualitySpecificationReference;

			Assert.NotNull(specificationRef, "Unexpected type of quality specification");

			VerificationRequest request =
				await CreateVerificationRequest(specificationRef, perimeter, projectWorkspace,
				                                resultsPath, saveVerification: saveVerification);

			ClientIssueMessageCollector messageCollector = CreateIssueMessageCollector();
			messageCollector.SetVerifiedSpecificationId(qualitySpecificationRef.Id);

			return await Verify(Assert.NotNull(_client.QaGrpcClient), request,
			                    messageCollector, projectWorkspace, progress);
		}

		public override async Task<ServiceCallStatus> Verify(
			QualitySpecification qualitySpecification,
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
				                                resultsPath);

			ClientIssueMessageCollector messageCollector = CreateIssueMessageCollector();
			messageCollector.SetVerifiedSpecification(qualitySpecification);

			return await Verify(Assert.NotNull(_client.QaGrpcClient), request,
			                    messageCollector, projectWorkspace, progress);
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
				                                resultsPath, objectsToVerify, false);

			ClientIssueMessageCollector messageCollector = CreateIssueMessageCollector();

			messageCollector.SetVerifiedObjects(objectsToVerify);
			messageCollector.SetVerifiedSpecificationId(qualitySpecificationRef.Id);

			return await Verify(Assert.NotNull(_client.QaGrpcClient), request, messageCollector,
			                    projectWorkspace, progress);
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

			ClientIssueMessageCollector messageCollector = CreateIssueMessageCollector();

			messageCollector.SetVerifiedObjects(objectsToVerify);
			messageCollector.SetVerifiedSpecification(qualitySpecification);

			return await Verify(Assert.NotNull(_client.QaGrpcClient), request, messageCollector,
			                    projectWorkspace, progress);
		}

		private async Task<ServiceCallStatus> Verify(
			[NotNull] QualityVerificationGrpc.QualityVerificationGrpcClient qaClient,
			[NotNull] VerificationRequest request,
			[NotNull] ClientIssueMessageCollector messageCollector,
			[NotNull] ProjectWorkspace projectWorkspace,
			[NotNull] QualityVerificationProgressTracker progress)
		{
			BackgroundVerificationRun verificationRun =
				QAUtils.CreateQualityVerificationRun(request, messageCollector, progress);

			bool provideDataFromClient = false;

			IVerificationDataProvider dataProvider =
				CreateVerificationDataProvider(projectWorkspace);

			if (dataProvider != null && await ShouldProvideDataFromClient(projectWorkspace))
			{
				provideDataFromClient = true;

				verificationRun.VerificationDataProvider = dataProvider;

				// The provider reads the live workspace; the reads must run on the MCT so that
				// unsaved edits are visible. The interactive verification runs on a worker thread,
				// so blocking on the MCT here does not dead-lock.
				verificationRun.DataProvisionScheduler = func => ProContext.Run(func);

				_msg.DebugFormat(
					"Verification data will be provided by the client (branch version or " +
					"unsaved edits detected, or client data forced).");
			}

			// Mode 2 (server-driven schema): send no schema; the client answers the server's
			// schema requests on demand.
			return await verificationRun.ExecuteAndProcessMessagesAsync(
				       qaClient, provideDataFromClient, schemaMsg: null);
		}

		/// <summary>
		/// Creates the data provider that serves verified data (and schema) from the client's edit
		/// session for the given project workspace. The base implementation returns null, which
		/// lets the server pull the data itself. Override to enable client-provided data.
		/// </summary>
		[CanBeNull]
		protected virtual IVerificationDataProvider CreateVerificationDataProvider(
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			return null;
		}

		private async Task<bool> ShouldProvideDataFromClient(
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			if (AlwaysUseClientData)
			{
				return true;
			}

			Datastore datastore = projectWorkspace.Datastore;

			bool result = await QueuedTask.Run(() =>
			{
				// Branch versions cannot be opened by the server's Enterprise SDK (it silently falls
				// back to Default) -> always provide the data from the client.
				if (IsBranchVersion(datastore))
				{
					return true;
				}

				// Unsaved edits are invisible to the server -> provide the edited data from the client.
				return HasUnsavedEdits(datastore);
			});

			return result;
		}

		private static bool IsBranchVersion([CanBeNull] Datastore datastore)
		{
			return datastore?.GetConnector() is DatabaseConnectionProperties dbConnectionProperties
			       && ! string.IsNullOrEmpty(dbConnectionProperties.Branch);
		}

		private static bool HasUnsavedEdits([CanBeNull] Datastore datastore)
		{
			if (datastore == null)
			{
				return false;
			}

			try
			{
				Project project = Project.Current;
				if (project?.HasEdits != true)
				{
					return false;
				}

				IReadOnlyList<Datastore> editedDatastores = project.EditedDatastores;
				if (editedDatastores == null || editedDatastores.Count == 0)
				{
					return false;
				}

				return editedDatastores.Any(edited =>
					                            WorkspaceUtils.IsSameDatastore(edited, datastore));
			}
			catch (Exception e)
			{
				_msg.Debug($"Error checking edited datastores: {e.Message}", e);
				return false;
			}
		}

		protected virtual ClientIssueMessageCollector CreateIssueMessageCollector()
		{
			return new ClientIssueMessageCollector();
		}

		[NotNull]
		protected virtual QualitySpecificationMsg CreateSpecificationMsg(
			[NotNull] IQualitySpecificationReference specificationRef)
		{
			return new QualitySpecificationMsg
			       {
				       QualitySpecificationId = specificationRef.Id
			       };
		}

		[NotNull]
		protected virtual QualitySpecificationMsg CreateSpecificationMsg(
			[NotNull] QualitySpecification qualitySpecification)
		{
			CustomQualitySpecification customSpecification =
				(CustomQualitySpecification) qualitySpecification;

			int specificationId = customSpecification.BaseSpecification.Id;

			var specificationMsg = new QualitySpecificationMsg
			                       {
				                       QualitySpecificationId = specificationId
			                       };

			specificationMsg.ExcludedConditionIds.AddRange(
				customSpecification.GetDisabledConditions().Select(c => c.Id));

			return specificationMsg;
		}

		[NotNull]
		protected virtual WorkContextMsg CreateWorkContextMsg(
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			string contextType = ContextType ?? _defaultContextType;
			string contextName = ContextName ?? Project.Current.Name;

			return QAUtils.CreateWorkContextMsg(projectWorkspace, contextType, contextName);
		}

		private async Task<VerificationRequest> CreateVerificationRequest(
			[NotNull] IQualitySpecificationReference specificationRef,
			[CanBeNull] Geometry perimeter,
			[NotNull] ProjectWorkspace projectWorkspace,
			[CanBeNull] string resultsPath,
			[CanBeNull] IList<Row> objectsToVerify = null,
			bool saveVerification = false)
		{
			QualitySpecificationMsg specificationMsg = CreateSpecificationMsg(specificationRef);

			VerificationRequest request =
				await QueuedTask.Run(() =>
				{
					WorkContextMsg workContextMsg = CreateWorkContextMsg(projectWorkspace);

					VerificationRequest result =
						QAUtils.CreateRequest(workContextMsg, specificationMsg, perimeter,
						                      DdxEnvironmentName);

					QAUtils.SetObjectsToVerify(result, objectsToVerify, projectWorkspace);

					return result;
				});

			SetPathParameters(resultsPath, request);

			QAUtils.SetVerificationParameters(
				request, GetTileSize(projectWorkspace), saveVerification, true, false);

			return request;
		}

		private async Task<VerificationRequest> CreateVerificationRequest(
			[NotNull] QualitySpecification specification,
			[CanBeNull] Geometry perimeter,
			[NotNull] ProjectWorkspace projectWorkspace,
			[CanBeNull] string resultsPath,
			[CanBeNull] IList<Row> objectsToVerify = null)
		{
			QualitySpecificationMsg specificationMsg = CreateSpecificationMsg(specification);

			VerificationRequest request =
				await QueuedTask.Run(() =>
				{
					WorkContextMsg workContextMsg = CreateWorkContextMsg(projectWorkspace);

					VerificationRequest result =
						QAUtils.CreateRequest(
							workContextMsg, specificationMsg, perimeter, DdxEnvironmentName);

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
				// GOTOP-796: Guard against inexistent path on server
				// TODO: Make paths configurable and remove
				if (! _client.RunsLocally())
				{
					// If not a local machine path, set the results path to null to avoid errors:
					if (! resultsPath.StartsWith(@"\\"))
					{
						_msg.Debug(
							"Paths will not be provided to server (no localhost and not UNC path)");
						return;
					}
				}

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
