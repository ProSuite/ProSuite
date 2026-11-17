using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AGP.Workflow;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Client.AGP.QA;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;

namespace ProSuite.AGP.QA
{
	internal sealed class ProjectWorkspaceVerificationExecutor : GrpcVerificationExecutor
	{
		[NotNull] private readonly VerificationServiceGrpc _service;
		[NotNull] private readonly IQualityVerificationClient _client;
		[NotNull] private readonly ProjectWorkspace _workspace;

		public ProjectWorkspaceVerificationExecutor(
			[NotNull] VerificationServiceGrpc service,
			[NotNull] IQualityVerificationClient client,
			[NotNull] ProjectWorkspace workspace,
			[CanBeNull] string resultsPath,
			[CanBeNull] string contextDisplayName)
			: base(resultsPath, contextDisplayName)
		{
			Assert.ArgumentNotNull(service, nameof(service));
			Assert.ArgumentNotNull(client, nameof(client));
			Assert.ArgumentNotNull(workspace, nameof(workspace));
			_service = service;
			_client = client;
			_workspace = workspace;
		}

		protected override bool RunsLocally => _client.RunsLocally();

		protected override bool AlwaysUseClientData => _service.AlwaysUseClientData;

		protected override QualityVerificationGrpc.QualityVerificationGrpcClient GetQaClient()
		{
			return Assert.NotNull(_client.QaGrpcClient);
		}

		protected override WorkContextMsg CreateWorkContextMsg()
		{
			return _service.CreateWorkContextMsg(_workspace);
		}

		protected override void SetObjectsToVerify(
			VerificationRequest request, IList<Row> objectsToVerify)
		{
			QAUtils.SetObjectsToVerify(request, objectsToVerify, _workspace);
		}

		protected override QualitySpecificationMsg CreateSpecificationMsg(
			IQualitySpecificationReference specification)
		{
			return _service.CreateSpecificationMsg(specification);
		}

		protected override QualitySpecificationMsg CreateSpecificationMsg(
			QualitySpecification specification)
		{
			return _service.CreateSpecificationMsg(specification);
		}

		protected override ClientIssueMessageCollector CreateIssueMessageCollector()
		{
			return _service.CreateIssueMessageCollector();
		}

		protected override Task<VerificationRequest> CreateVerificationRequestAsync(
			Func<QualitySpecificationMsg> createSpecification, Geometry perimeter,
			IList<Row> objectsToVerify)
		{
			// Preserve the caller-thread execution of the existing specification hooks.
			QualitySpecificationMsg specification = createSpecification();
			return base.CreateVerificationRequestAsync(
				() => specification, perimeter, objectsToVerify);
		}

		protected override IVerificationDataProvider CreateVerificationDataProvider()
		{
			return _service.CreateVerificationDataProvider(_workspace);
		}

		protected override Task<Datastore> GetCurrentDatastore()
		{
			return Task.FromResult(_workspace.Datastore);
		}
	}
}
