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

		/// <summary>
		/// Creates the data provider that serves verified data (and schema) from the client's edit
		/// session for the given project workspace. The base implementation returns null, which
		/// lets the server pull the data itself. Override to enable client-provided data.
		/// </summary>
		[CanBeNull]
		public virtual IVerificationDataProvider CreateVerificationDataProvider(
			[NotNull] ProjectWorkspace projectWorkspace)
		{
			return null;
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
	}
}
