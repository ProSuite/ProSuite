using System;
using System.Collections.Generic;
using System.Web;
using ArcGIS.Core.Data.Exceptions;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Core.Geodatabase.PluginDatasources.WireFrame
{
	/// <summary>
	/// Plugin datasource containing a table which unions the features from a set of input
	/// polygon or polyline feature classes. The geometries are converted to polylines.
	/// The <see cref="IWireFrameSourceLayers"/> interface is implemented by the map, providing the
	/// currently visible feature classes. In the future, other implementations could
	/// be created.
	/// </summary>
	public abstract class WireFrameDatasourceBase : PluginDatasourceTemplate
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private string _mapId;

		private readonly IReadOnlyList<string> _tableNames = new List<string>
		                                                     {
			                                                     WireFrameConstants.TableName
		                                                     };

		public override void Open([NotNull] Uri connectionPath) // "open workspace"
		{
			// Note: throw a GeodatabaseException if there's a problem opening

			if (connectionPath is null)
				throw new ArgumentNullException(nameof(connectionPath));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug($"WireFrame plugin: opening datasource {connectionPath}");
			}

			// Empirical: when opening a project (.aprx) with a saved layer using our Plugin
			// Datasource, the connectionPath will be prepended with the project file's
			// directory path and two times URL encoded (e.g., ' ' => %20 => %2520)!

			// TODO: Consider just using the map name and ignore the prepended path to adapt to moved project folder?
			// TODO: Or, ignore the connectionPath altogether -- we're on the active map, always!

			var path = connectionPath.IsAbsoluteUri
				           ? connectionPath.LocalPath
				           : connectionPath.ToString();

			path = HttpUtility.UrlDecode(path);
			path = HttpUtility.UrlDecode(path);

			_mapId = path;
		}

		public override void Close()
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug("WireFrame plugin: closing datasource");
			}
		}

		public override PluginTableTemplate OpenTable([NotNull] string name)
		{
			// Note: the name given is one of those returned by GetTableNames()
			// Note: throw a GeodatabaseException if there's a problem opening
			//       (Pro should show the red exclamation mark next to the layer)

			if (name is null)
				throw new ArgumentNullException(nameof(name));

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug($"WireFrame plugin: opening table {name}");
			}

			if (string.Equals(name, _tableNames[0]))
			{
				return new WireFrameTable(_mapId, name);
			}

			_msg.Warn($"Wire frame data source has no such table: {name}");
			throw new GeodatabaseException($"No such table in wire frame datasource: {name}");
		}

		public override IReadOnlyList<string> GetTableNames()
		{
			return _tableNames;
		}

		public override bool IsQueryLanguageSupported()
		{
			return false;
		}

		public override string GetDatasourceDescription(bool inPluralForm)
		{
			return inPluralForm ? "WireFrame Datasources" : "WireFrame Datasource";
		}
	}
}
