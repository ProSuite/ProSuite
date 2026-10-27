using System;
using System.Collections.Generic;
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

		public sealed override async Task<ServiceCallStatus> Verify(
			IQualitySpecificationReference qualitySpecificationRef,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			Assert.ArgumentNotNull(qualitySpecificationRef, nameof(qualitySpecificationRef));
			Assert.ArgumentNotNull(projectWorkspace, nameof(projectWorkspace));
			Assert.ArgumentNotNull(progress, nameof(progress));

			return await CreateExecution(projectWorkspace, resultsPath, null).Verify(
				       qualitySpecificationRef, perimeter, progress);
		}

		public sealed override async Task<ServiceCallStatus> Verify(
			QualitySpecification qualitySpecification,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));
			Assert.ArgumentNotNull(projectWorkspace, nameof(projectWorkspace));
			Assert.ArgumentNotNull(progress, nameof(progress));

			return await CreateExecution(projectWorkspace, resultsPath, null).Verify(
				       qualitySpecification, perimeter, progress);
		}

		public sealed override async Task<ServiceCallStatus> VerifySelection(
			IQualitySpecificationReference qualitySpecificationRef,
			IList<Row> objectsToVerify,
			Geometry perimeter,
			ProjectWorkspace projectWorkspace,
			QualityVerificationProgressTracker progress,
			string resultsPath)
		{
			return await CreateExecution(projectWorkspace, resultsPath, null).VerifySelection(
				       qualitySpecificationRef, objectsToVerify, perimeter, progress);
		}

		public sealed override async Task<ServiceCallStatus> VerifySelection(
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

			return await CreateExecution(projectWorkspace, resultsPath, null).VerifySelection(
				       qualitySpecification, objectsToVerify, perimeter, progress);
		}

		internal async Task<bool> ConfigureVerificationRunAsync(
			[NotNull] BackgroundVerificationRun verificationRun,
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			bool provideDataFromClient = false;

			IVerificationDataProvider dataProvider =
				CreateVerificationDataProvider(projectWorkspace);

			ClientDataReason clientDataReason = dataProvider == null
				                                    ? ClientDataReason.None
				                                    : await GetClientDataReason(projectWorkspace);

			if (clientDataReason != ClientDataReason.None)
			{
				provideDataFromClient = true;

				verificationRun.VerificationDataProvider = dataProvider;

				// The provider reads the live workspace; the reads must run on the MCT so that
				// unsaved edits are visible. The interactive verification runs on a worker thread,
				// so blocking on the MCT here does not dead-lock.
				verificationRun.DataProvisionScheduler = func => ProContext.Run(func);

				_msg.Info(GetClientDataMessage(clientDataReason));
			}

			return provideDataFromClient;
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

		private static bool IsFeatureService([CanBeNull] Datastore datastore)
		{
			return datastore?.GetConnector() is ServiceConnectionProperties;
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

		protected internal virtual ClientIssueMessageCollector CreateIssueMessageCollector()
		{
			return new ClientIssueMessageCollector();
		}

		[NotNull]
		protected internal virtual QualitySpecificationMsg CreateSpecificationMsg(
			[NotNull] IQualitySpecificationReference specificationRef)
		{
			return QAUtils.CreateSpecificationMsg(specificationRef.Id);
		}

		[NotNull]
		protected internal virtual QualitySpecificationMsg CreateSpecificationMsg(
			[NotNull] QualitySpecification qualitySpecification)
		{
			return QAUtils.CreateSpecificationMsg(qualitySpecification);
		}

		[NotNull]
		protected internal virtual WorkContextMsg CreateWorkContextMsg(
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			string contextType = ContextType ?? _defaultContextType;
			string contextName = ContextName ?? Project.Current.Name;

			return QAUtils.CreateWorkContextMsg(projectWorkspace, contextType, contextName);
		}

		public override IVerificationExecution CreateExecution(
			ProjectWorkspace projectWorkspace, string resultsPath, string contextDisplayName)
		{
			return new ProjectWorkspaceVerificationExecutor(
				       this, _client, projectWorkspace, resultsPath, contextDisplayName)
			       {
				       DdxEnvironmentName = DdxEnvironmentName,
				       HtmlReportName = HtmlReportName,
				       VerificationReportName = VerificationReportName,
				       Parameters = Parameters
			       };
		}

		private enum ClientDataReason
		{
			None,
			Forced,
			FeatureService,
			UnsavedEdits
		}

		private async Task<ClientDataReason> GetClientDataReason(
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			if (AlwaysUseClientData)
			{
				return ClientDataReason.Forced;
			}

			Datastore datastore = projectWorkspace.Datastore;

			return await QueuedTask.Run(() =>
			{
				if (IsFeatureService(datastore))
				{
					return ClientDataReason.FeatureService;
				}

				return HasUnsavedEdits(datastore)
					       ? ClientDataReason.UnsavedEdits
					       : ClientDataReason.None;
			});
		}

		[NotNull]
		private static string GetClientDataMessage(ClientDataReason reason)
		{
			const string sentFromHere =
				"the data is read from this ArcGIS Pro session and sent to the verification service";

			switch (reason)
			{
				case ClientDataReason.UnsavedEdits:
					return $"The verified data has unsaved edits, so {sentFromHere}.";

				case ClientDataReason.FeatureService:
					return
						$"The verified data is provided from a feature service, so {sentFromHere}.";

				case ClientDataReason.Forced:
					return $"Client-provided verification data is switched on, so {sentFromHere}.";

				default:
					return $"For this verification, {sentFromHere}.";
			}
		}
	}
}
