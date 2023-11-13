using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Data.PluginDatastore;
using ArcGIS.Core.Data.Realtime;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	/// <summary>
	/// Name object that represents a thread-safe moniker for a datastore.
	/// The datastore can be re-opened from the name object.
	/// Aspirationally, this could
	/// - be turned into a proper serializable memento
	/// - implement the equality comparison
	/// </summary>
	public class DatastoreName
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly Connector _connector;

		public DatastoreName([NotNull] Datastore datastore)
		{
			Assert.ArgumentNotNull(datastore, nameof(datastore));

			// NOTE: Connectors have no thread affinity.
			_connector = datastore.GetConnector();
		}

		public DatastoreName([NotNull] Connector connector)
		{
			Assert.ArgumentNotNull(connector, nameof(connector));

			_connector = connector;
		}

		public Datastore Open()
		{
			try
			{
				switch (_connector)
				{
					case DatabaseConnectionFile dbConnection:
						return new ArcGIS.Core.Data.Geodatabase(dbConnection);

					case DatabaseConnectionProperties dbConnectionProps:
						return new ArcGIS.Core.Data.Geodatabase(dbConnectionProps);

					case FileGeodatabaseConnectionPath fileGdbConnection:
						return new ArcGIS.Core.Data.Geodatabase(fileGdbConnection);

					case FileSystemConnectionPath fileSystemConnection:
						return new FileSystemDatastore(fileSystemConnection);

					case MemoryConnectionProperties memoryConnectionProperties:
						return new ArcGIS.Core.Data.Geodatabase(memoryConnectionProperties);

					case MobileGeodatabaseConnectionPath mobileConnectionProperties:
						return new ArcGIS.Core.Data.Geodatabase(mobileConnectionProperties);

					case PluginDatasourceConnectionPath pluginConnectionPath:
						return new PluginDatastore(pluginConnectionPath);

					case RealtimeServiceConnectionProperties realtimeServiceConnection:
						return new RealtimeDatastore(realtimeServiceConnection);

					case ServiceConnectionProperties serviceConnection:
						return new ArcGIS.Core.Data.Geodatabase(serviceConnection);

					case SQLiteConnectionPath sqLiteConnection:
						return new Database(sqLiteConnection);

					default:
						throw new ArgumentOutOfRangeException(
							$"Unsupported workspace type: {_connector?.GetType()}");
				}
			}
			catch (Exception e)
			{
				_msg.Debug(
					$"Error opening datastore {WorkspaceUtils.GetDatastoreDisplayText(_connector)}",
					e);
				throw;
			}
		}

		public string GetDisplayText()
		{
			return _connector != null
				       ? WorkspaceUtils.GetDatastoreDisplayText(_connector)
				       : "<undefined connection>";
		}

		#region Overrides of Object

		public override string ToString()
		{
			return GetDisplayText();
		}

		#endregion
	}
}
