using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;

namespace ProSuite.Microservices.Server.AO.QA
{
	/// <summary>
	/// Provides look-up functionality for virtual schemas, i.e. workspace names
	/// that reference GdbWorkspaces.
	/// </summary>
	public class VirtualWorkspacesModelContext : ICurrentModelContext
	{
		private readonly IModelRepository _modelRepository;
		private readonly IList<DdxModel> _knownModels;

		public VirtualWorkspacesModelContext([NotNull] IEnumerable<DdxModel> knownModels,
		                                     [NotNull] IModelRepository modelRepository)
		{
			_modelRepository = modelRepository;
			_knownModels = knownModels.ToList();
		}

		public Dataset GetDataset(string gdbDatasetName, IWorkspaceName workspaceName)
		{
			DdxModel model = GetModel(workspaceName);

			Dataset result = model.GetDatasetByModelName(gdbDatasetName);

			return result;
		}

		public Association GetAssociation(string relationshipClass, IWorkspaceName workspaceName)
		{
			DdxModel model = GetModel(workspaceName);

			return model.GetAssociationByModelName(relationshipClass);
		}

		private DdxModel GetModel(IWorkspaceName workspaceName)
		{
			IWorkspace workspace = (IWorkspace) ((IName) workspaceName).Open();

			if (! (workspace is GdbWorkspace gdbWorkspace))
			{
				throw new ArgumentException(
					"The workspace name does not reference a virtual gdb workspace");
			}

			int? modelId = Convert.ToInt32(gdbWorkspace.WorkspaceHandle);

			Assert.NotNull(modelId, "Workspace handle is null");

			DdxModel model = _knownModels.FirstOrDefault(m => m.Id == modelId.Value);

			if (model == null)
			{
				model = Assert.NotNull(_modelRepository.Get(modelId.Value),
				                       $"No module found with Id {modelId}");
				_knownModels.Add(model);
			}

			return model;
		}
	}
}
