using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public static class DatasetListBuilderUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static bool CanOpenDataset([NotNull] IDatasetName datasetName,
		                                  [NotNull] out string errorMessage)
		{
			// try to open the dataset

			try
			{
				((IName) datasetName).Open();
			}
			catch (Exception e)
			{
				errorMessage = e.Message;
				return false;
			}

			errorMessage = string.Empty;
			return true;
		}

		public static bool IsUnregisteredTable([NotNull] IDatasetName datasetName)
		{
			var tableName = datasetName as ITableName;

			if (tableName == null)
			{
				return false;
			}

			IWorkspace workspace = WorkspaceUtils.OpenWorkspace(datasetName);

			return ! DatasetUtils.IsRegisteredAsObjectClass(
				       workspace, datasetName.Name);
		}

		public static bool SupportsVersioning([NotNull] IWorkspace workspace)
		{
			var versionedWorkspace = workspace as IVersionedWorkspace;
			return versionedWorkspace != null;
		}

		public static bool IsVersioned([NotNull] IDatasetName datasetName)
		{
			IVersionedObject versionedObj;
			try
			{
				versionedObj = ((IName) datasetName).Open() as IVersionedObject;
			}
			catch (Exception ex)
			{
				_msg.WarnFormat("Unable to open dataset {0}: {1}",
				                datasetName.Name, ex.Message);
				return false;
			}

			return versionedObj != null && versionedObj.IsRegisteredAsVersioned;
		}
	}
}
