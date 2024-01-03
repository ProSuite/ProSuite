using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using Google.Protobuf.Collections;
using Grpc.Core;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Com;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using Quaestor.LoadReporting;

namespace ProSuite.Microservices.Server.AO.QA
{
	public class QualityVerificationDdxGrpcImpl<TModel>
		: QualityVerificationDdxGrpc.QualityVerificationDdxGrpcBase where TModel : ProductionModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IDomainTransactionManager _domainTransactions;

		// Technically probably not necessary because no proper AO-objects are used.
		// But rather be safe than sorry (and experiencing locks and hangs).
		private readonly StaTaskScheduler _staThreadScheduler = new StaTaskScheduler(5);

		public QualityVerificationDdxGrpcImpl(
			[NotNull] IDomainTransactionManager domainTransactions)
		{
			_domainTransactions = domainTransactions;
		}

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

		/// <summary>
		/// The default value to use if the environment variable that indicates whether or not the
		/// service should continue serving (or shut down) in case of an exception.
		/// </summary>
		public bool KeepServingOnErrorDefaultValue { get; set; }

		#region Overrides of QualityVerificationDdxGrpcBase

		public override async Task<GetProjectWorkspacesResponse> GetProjectWorkspaces(
			GetProjectWorkspacesRequest request, ServerCallContext context)
		{
			GetProjectWorkspacesResponse response;

			try
			{
				string peer = context.Peer;

				await StartRequestAsync(peer, request);

				Stopwatch watch = _msg.DebugStartTiming();

				Func<ITrackCancel, GetProjectWorkspacesResponse> func =
					trackCancel => GetProjectWorkspacesCore(request);

				using (_msg.IncrementIndentation("Getting project workspaces for {0}",
				                                 peer))
				{
					response =
						await GrpcServerUtils.ExecuteServiceCall(
							func, context, _staThreadScheduler, true) ??
						new GetProjectWorkspacesResponse();
				}

				_msg.DebugStopTiming(
					watch, "Gotten project workspaces for peer {0} ({1} object class(es))",
					peer, request.ObjectClasses.Count);

				_msg.InfoFormat("Returning {0} project workspaces",
				                response.ProjectWorkspaces.Count);
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting project workspaces {request}", e);

				if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				{
					ServiceUtils.SetUnhealthy(Health, GetType());
				}

				throw;
			}
			finally
			{
				EndRequest();
			}

			return response;
		}

		public override async Task<GetSpecificationRefsResponse> GetQualitySpecificationRefs(
			GetSpecificationRefsRequest request, ServerCallContext context)
		{
			GetSpecificationRefsResponse response;

			try
			{
				await StartRequestAsync(context.Peer, request);

				Stopwatch watch = _msg.DebugStartTiming();

				Func<ITrackCancel, GetSpecificationRefsResponse> func =
					trackCancel => GetSpecificationRefsCore(request);

				using (_msg.IncrementIndentation("Getting quality specifications for {0}",
				                                 context.Peer))
				{
					response =
						await GrpcServerUtils.ExecuteServiceCall(
							func, context, _staThreadScheduler, true) ??
						new GetSpecificationRefsResponse();
				}

				_msg.DebugStopTiming(
					watch, "Gotten quality specifications for peer {0} ({1} dataset(s))",
					context.Peer, request.DatasetIds);

				_msg.InfoFormat("Returning {0} quality specifications",
				                response.QualitySpecifications.Count);
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting quality specifications {request}", e);

				if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				{
					ServiceUtils.SetUnhealthy(Health, GetType());
				}

				throw;
			}
			finally
			{
				EndRequest();
			}

			return response;
		}

		public override async Task<GetSpecificationResponse> GetQualitySpecification(
			GetSpecificationRequest request, ServerCallContext context)
		{
			GetSpecificationResponse response;

			try
			{
				await StartRequestAsync(context.Peer, request);

				Stopwatch watch = _msg.DebugStartTiming();

				Func<ITrackCancel, GetSpecificationResponse> func =
					trackCancel => GetSpecificationCore(request);

				using (_msg.IncrementIndentation("Getting quality specification for {0}",
				                                 context.Peer))
				{
					response =
						await GrpcServerUtils.ExecuteServiceCall(
							func, context, _staThreadScheduler, true) ??
						new GetSpecificationResponse();
				}

				_msg.DebugStopTiming(
					watch, "Gotten quality specification for peer {0} (<id> {1})",
					context.Peer, request.QualitySpecificationId);

				_msg.InfoFormat("Returning quality specification {0} with {1} conditions",
				                response.Specification.Name, response.Specification.Elements.Count);
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting quality specifications {request}", e);

				if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				{
					ServiceUtils.SetUnhealthy(Health, GetType());
				}

				throw;
			}
			finally
			{
				EndRequest();
			}

			return response;
		}

		public override async Task<GetConditionResponse> GetQualityCondition(
			GetConditionRequest request, ServerCallContext context)
		{
			GetConditionResponse response;

			try
			{
				await StartRequestAsync(context.Peer, request);

				Stopwatch watch = _msg.DebugStartTiming();

				Func<ITrackCancel, GetConditionResponse> func =
					trackCancel => GetConditionCore(request);

				using (_msg.IncrementIndentation("Getting quality condition {0} for {1}",
				                                 request.ConditionName, context.Peer))
				{
					response =
						await GrpcServerUtils.ExecuteServiceCall(
							func, context, _staThreadScheduler, true) ??
						new GetConditionResponse();
				}

				_msg.DebugStopTiming(
					watch, "Gotten quality condition for peer {0} ({1})",
					context.Peer, request.ConditionName);

				_msg.InfoFormat("Returning quality condition <id> {0}",
				                response.Condition?.ConditionId);
			}
			catch (Exception e)
			{
				_msg.Error($"Error getting quality specifications {request}", e);

				if (! ServiceUtils.KeepServingOnError(KeepServingOnErrorDefaultValue))
				{
					ServiceUtils.SetUnhealthy(Health, GetType());
				}

				throw;
			}
			finally
			{
				EndRequest();
			}

			return response;
		}

		#endregion

		private GetProjectWorkspacesResponse GetProjectWorkspacesCore(
			GetProjectWorkspacesRequest request)
		{
			IList<GdbWorkspace> gdbWorkspaces =
				ProtobufConversionUtils.CreateSchema(request.ObjectClasses,
				                                     request.Workspaces);

			List<IObjectClass> objectClasses = new List<IObjectClass>();

			foreach (GdbWorkspace gdbWorkspace in gdbWorkspaces)
			{
				objectClasses.AddRange(gdbWorkspace.GetDatasets());
			}

			IVerificationDataDictionary<TModel> verificationDataDictionary =
				Assert.NotNull(VerificationDdx,
				               "Data Dictionary access has not been configured or failed.");

			IList<ProjectWorkspaceBase<Project<TModel>, TModel>> projectWorkspaces = null;

			_domainTransactions.UseTransaction(
				() =>
				{
					projectWorkspaces =
						verificationDataDictionary.GetProjectWorkspaceCandidates(objectClasses);
				});

			GetProjectWorkspacesResponse response = PackProjectWorkspaceResponse(projectWorkspaces);

			return response;
		}

		private static GetProjectWorkspacesResponse PackProjectWorkspaceResponse(
			[NotNull] IList<ProjectWorkspaceBase<Project<TModel>, TModel>> projectWorkspaces)
		{
			var response = new GetProjectWorkspacesResponse();

			var projects = new HashSet<Project<TModel>>();

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
				TModel productionModel = project.ProductionModel;

				var projectMsg =
					new ProjectMsg
					{
						ProjectId = project.Id,
						ModelId = productionModel.Id,
						Name = project.Name,
						ShortName = project.ShortName,
						MinimumScaleDenominator = project.MinimumScaleDenominator,
						ExcludeReadOnlyDatasetsFromProjectWorkspace =
							project.ExcludeReadOnlyDatasetsFromProjectWorkspace
					};

				CallbackUtils.DoWithNonNull(
					projectMsg.ToolConfigDirectory, s => project.ToolConfigDirectory = s);

				response.Projects.Add(projectMsg);

				RepeatedField<DatasetMsg> responseDatasets = response.Datasets;

				ModelMsg modelMsg = ToModelMsg(productionModel, responseDatasets);

				response.Models.Add(modelMsg);
			}

			return response;
		}

		private static ModelMsg ToModelMsg(TModel productionModel,
		                                   ICollection<DatasetMsg> referencedDatasetMsgs)
		{
			SpatialReferenceMsg srWkId = ProtobufGeometryUtils.ToSpatialReferenceMsg(
				productionModel.SpatialReferenceDescriptor.SpatialReference,
				SpatialReferenceMsg.FormatOneofCase.SpatialReferenceWkid);

			ModelMsg modelMsg =
				ProtoDataQualityUtils.ToDdxModelMsg(productionModel, srWkId, referencedDatasetMsgs);

			// If necessary, return the list of referenced workspaces
			// However, this is currently not needed anywhere and requires opening workpaces, which is slow!

			//IWorkspace masterWorkspace =
			//	productionModel.GetMasterDatabaseWorkspace();
			//modelMsg.MasterDbWorkspaceHandle = masterWorkspace?.GetHashCode() ?? -1;

			return modelMsg;
		}

		private GetSpecificationRefsResponse GetSpecificationRefsCore(
			GetSpecificationRefsRequest request)
		{
			var response = new GetSpecificationRefsResponse();

			IVerificationDataDictionary<TModel> verificationDataDictionary =
				Assert.NotNull(VerificationDdx,
				               "Data Dictionary access has not been configured or failed.");

			IList<QualitySpecification> foundSpecifications = null;
			_domainTransactions.UseTransaction(
				() =>
				{
					foundSpecifications =
						verificationDataDictionary.GetQualitySpecifications(
							request.DatasetIds, request.IncludeHidden);
				});

			response.QualitySpecifications.AddRange(
				foundSpecifications.Select(qs => new QualitySpecificationRefMsg
				                                 {
					                                 Name = qs.Name,
					                                 QualitySpecificationId = qs.Id
				                                 }));

			return response;
		}

		private GetSpecificationResponse GetSpecificationCore(
			[NotNull] GetSpecificationRequest request)
		{
			var response = new GetSpecificationResponse();

			IVerificationDataDictionary<TModel> verificationDataDictionary =
				Assert.NotNull(VerificationDdx,
				               "Data Dictionary access has not been configured or failed.");

			_domainTransactions.UseTransaction(
				() =>
				{
					QualitySpecification qualitySpecification =
						verificationDataDictionary.GetQualitySpecification(
							request.QualitySpecificationId);

					if (qualitySpecification == null)
					{
						return;
					}

					ConditionListSpecificationMsg specificationMsg =
						ProtoDataQualityUtils.CreateConditionListSpecificationMsg(
							qualitySpecification, null,
							out IDictionary<int, DdxModel> modelsById);

					response.Specification = specificationMsg;

					response.ReferencedInstanceDescriptors.AddRange(
						ProtoDataQualityUtils.GetInstanceDescriptorMsgs(qualitySpecification));

					RepeatedField<DatasetMsg> referencedDatasets = response.ReferencedDatasets;
					foreach (DdxModel model in modelsById.Values)
					{
						ModelMsg modelMsg = ToModelMsg((TModel) model, referencedDatasets);
						response.ReferencedModels.Add(modelMsg);
					}
				});

			return response;
		}

		private GetConditionResponse GetConditionCore([NotNull] GetConditionRequest request)
		{
			var response = new GetConditionResponse();

			IVerificationDataDictionary<TModel> verificationDataDictionary =
				Assert.NotNull(VerificationDdx,
				               "Data Dictionary access has not been configured or failed.");

			_domainTransactions.UseTransaction(
				() =>
				{
					QualityCondition qualityCondition =
						verificationDataDictionary.GetQualityCondition(request.ConditionName);

					if (qualityCondition == null)
					{
						return;
					}

					// The parameters must be initialized!
					InstanceConfigurationUtils.InitializeParameterValues(qualityCondition);

					IDictionary<int, DdxModel> modelsById = new Dictionary<int, DdxModel>();

					QualityConditionMsg conditionMsg =
						ProtoDataQualityUtils.CreateQualityConditionMsg(
							qualityCondition, null, modelsById);

					response.Condition = conditionMsg;
					response.CategoryName = qualityCondition.Category?.Name ?? string.Empty;

					response.ReferencedInstanceDescriptors.AddRange(
						ProtoDataQualityUtils.GetInstanceDescriptorMsgs(
							new[] { qualityCondition }));

					RepeatedField<DatasetMsg> referencedDatasets = response.ReferencedDatasets;
					foreach (DdxModel model in modelsById.Values)
					{
						ModelMsg modelMsg = ToModelMsg((TModel) model, referencedDatasets);
						response.ReferencedModels.Add(modelMsg);
					}
				});

			return response;
		}

		private async Task<bool> StartRequestAsync(string peerName, object request,
		                                           bool requiresLicense = true)
		{
			// The request comes in on a .NET thread-pool thread, which has no useful name
			// when it comes to logging. Set the ID as its name.
			ProcessUtils.TrySetThreadIdAsName();

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

		private async Task<bool> EnsureLicenseAsync()
		{
			if (LicenseAction == null)
			{
				return true;
			}

			return await LicenseAction();
		}
	}
}
