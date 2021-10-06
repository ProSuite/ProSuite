using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESRI.ArcGIS.Geodatabase;
using Grpc.Core;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using Quaestor.LoadReporting;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class QualityVerificationDdxGrpcImpl<TModel>
		: QualityVerificationDdxGrpc.QualityVerificationDdxGrpcBase where TModel : ProductionModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// The overall service process health. If it has been set, it will be marked as not serving
		/// in case any error occurs in this service implementation. Later this might be limited to
		/// specific, serious errors (such as out-of-memory, TNS could not be resolved).
		/// </summary>
		[CanBeNull]
		public IServiceHealth Health { get; set; }

		/// <summary>
		/// The current service load to be kept up-to-date by the quality verification service.
		/// A reference will also be passed to <see cref="LoadReportingGrpcImpl"/> to report the
		/// current load to interested load balancers. 
		/// </summary>
		[CanBeNull]
		public ServiceLoad CurrentLoad { get; set; }

		/// <summary>
		/// The license checkout action to be performed before any service call is executed.
		/// By default the lowest available license (basic, standard, advanced) is checked out
		/// in a 32-bit process, the server license is checked out in a 64-bit process. In case
		/// a test requires a specific license or an extension, provide a different function.
		/// </summary>
		[CanBeNull]
		public Func<Task<bool>> LicenseAction { get; set; }

		/// <summary>
		/// The data dictionary facade that wraps the necessary repositories.
		/// </summary>
		public IVerificationDataDictionary<TModel> VerificationDdx { get; set; }

		public override async Task<GetProjectWorkspacesResponse> GetProjectWorkspaces(
			GetProjectWorkspacesRequest request,
			ServerCallContext context)
		{
			GetProjectWorkspacesResponse response;

			try
			{
				await StartRequestAsync(context.Peer, request);

				_msg.InfoFormat("Getting project workspaces for {0}", context.Peer);
				_msg.VerboseDebug(() => $"Request details: {request}");

				response = GetProjectWorkspacesCore(request);

				_msg.InfoFormat("Returning {0} project workspaces",
				                response.ProjectWorkspaces.Count);
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting project workspaces {request}", e);

				SetUnhealthy();

				throw;
			}
			finally
			{
				EndRequest();
			}

			return response;
		}

		public override async Task<GetSpecificationsResponse> GetQualitySpecifications(
			GetSpecificationsRequest request,
			ServerCallContext context)
		{
			var response = new GetSpecificationsResponse();

			try
			{
				await StartRequestAsync(context.Peer, request);

				_msg.InfoFormat("Getting quality specifications for {0}", context.Peer);

				IVerificationDataDictionary<TModel> verificationDataDictionary =
					Assert.NotNull(VerificationDdx);

				IList<QualitySpecification> foundSpecifications =
					verificationDataDictionary.GetQualitySpecifications(
						request.DatasetIds, request.IncludeHidden);

				response.QualitySpecifications.AddRange(
					foundSpecifications.Select(qs => new QualitySpecificationRefMsg
					                                 {
						                                 Name = qs.Name,
						                                 QualitySpecificationId = qs.Id
					                                 }));

				_msg.InfoFormat("Returning {0} specifications",
				                response.QualitySpecifications.Count);
			}
			catch (Exception e)
			{
				_msg.Error($"Error verifying quality for request {request}", e);

				SetUnhealthy();

				throw;
			}
			finally
			{
				EndRequest();
			}

			return response;
		}

		private GetProjectWorkspacesResponse GetProjectWorkspacesCore(
			GetProjectWorkspacesRequest request)
		{
			IList<GdbWorkspace> gdbWorkspaces =
				ProtobufConversionUtils.CreateSchema(request.ObjectClasses,
				                                     request.Workspaces);

			List<IObjectClass> objectClasses = new List<IObjectClass>();

			foreach (GdbWorkspace gdbWorkspace in gdbWorkspaces)
			{
				objectClasses.AddRange(gdbWorkspace.GetDatasets().Cast<IObjectClass>());
			}

			IVerificationDataDictionary<TModel> verificationDataDictionary =
				Assert.NotNull(VerificationDdx);

			var projectWorkspaces =
				verificationDataDictionary.GetProjectWorkspaceCandidates(objectClasses);

			var projects = new HashSet<Project<TModel>>();

			GetProjectWorkspacesResponse response =
				PackProjectWorkspaceResponse(projectWorkspaces, projects);

			return response;
		}

		private static GetProjectWorkspacesResponse PackProjectWorkspaceResponse(
			[NotNull] IList<ProjectWorkspaceBase<Project<TModel>, TModel>> projectWorkspaces,
			[NotNull] HashSet<Project<TModel>> projects)
		{
			var response = new GetProjectWorkspacesResponse();

			foreach (var projectWorkspace in projectWorkspaces)
			{
				ProjectWorkspaceMsg projectWorkspaceMsg = new ProjectWorkspaceMsg();

				projectWorkspaceMsg.ProjectId = projectWorkspace.Project.Id;

				var gdbWorkspace = projectWorkspace.Workspace as GdbWorkspace;

				projectWorkspaceMsg.WorkspaceHandle = gdbWorkspace?.WorkspaceHandle ?? -1;

				projectWorkspaceMsg.DatasetIds.AddRange(
					projectWorkspace.Datasets.Select(ds => ds.Id));

				response.ProjectWorkspaces.Add(projectWorkspaceMsg);

				projects.Add(projectWorkspace.Project);
			}

			foreach (Project<TModel> project in projects)
			{
				response.Projects.Add(
					new ProjectMsg()
					{
						ProjectId = project.Id,
						ModelId = project.ProductionModel.Id,
						Name = project.Name,
						ShortName = project.ShortName,
						MinimumScaleDenominator = project.MinimumScaleDenominator,
						ToolConfigDirectory = project.ToolConfigDirectory,
						ExcludeReadOnlyDatasetsFromProjectWorkspace =
							project.ExcludeReadOnlyDatasetsFromProjectWorkspace
					});

				var srWkId = ProtobufGeometryUtils.ToSpatialReferenceMsg(
					project.ProductionModel.SpatialReferenceDescriptor.SpatialReference,
					SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid);

				var modelMsg =
					new ModelMsg()
					{
						ModelId = project.ProductionModel.Id,
						Name = project.ProductionModel.Name,
						SpatialReference = srWkId
					};

				IWorkspace masterWorkspace =
					project.ProductionModel.GetMasterDatabaseWorkspace();

				// If necessary, return the list of referenced workspaces
				modelMsg.MasterDbWorkspaceHandle = masterWorkspace?.GetHashCode() ?? -1;

				foreach (Dataset dataset in project.ProductionModel.Datasets)
				{
					modelMsg.DatasetIds.Add(dataset.Id);

					if (dataset is IErrorDataset)
					{
						modelMsg.ErrorDatasetIds.Add(dataset.Id);
					}

					response.Datasets.Add(new DatasetMsg()
					                      {
						                      DatasetId = dataset.Id,
						                      Name = dataset.Name,
						                      AliasName = dataset.AliasName
					                      });
				}

				response.Models.Add(modelMsg);
			}

			return response;
		}

		private async Task<bool> StartRequestAsync(string peerName, object request,
		                                           bool requiresLicense = true)
		{
			CurrentLoad?.StartRequest();

			_msg.InfoFormat("Starting {0} request from {1}", request.GetType().Name, peerName);

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug(() => $"Request details: {request}");
			}

			return await EnsureLicenseAsync();
		}

		private void EndRequest()
		{
			CurrentLoad?.EndRequest();
		}

		private void SetUnhealthy()
		{
			if (Health != null)
			{
				_msg.Warn("Setting service health to \"not serving\" due to exception " +
				          "because the process might be compromised.");

				Health?.SetStatus(GetType(), false);
			}
		}

		public async Task<bool> EnsureLicenseAsync()
		{
			if (LicenseAction == null)
			{
				return true;
			}

			return await LicenseAction();
		}
	}
}
