using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Web;
using ArcGIS.Core.Data.PluginDatastore;
using ProSuite.Commons.Essentials.Assertions;
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
			                                                     "WireFrame"
		                                                     };

		public override void Open([NotNull] Uri connectionPath) // "open workspace"
		{
			Try(() =>
			{
				Assert.ArgumentNotNull(connectionPath, nameof(connectionPath));

				_msg.Debug($"WireFrame Datasource: Trying to open {connectionPath}");

				// Empirical: when opening a project (.aprx) with a saved layer using our Plugin
				// Datasource, the connectionPath will be prepended with the project file's
				// directory path and two times URL encoded (e.g., ' ' => %20 => %2520)!

				// TODO: Consider just using the map name and ignore the prepended path to adapt to moved project folder?

				var path = connectionPath.IsAbsoluteUri
					           ? connectionPath.LocalPath
					           : connectionPath.ToString();

				path = HttpUtility.UrlDecode(path);
				path = HttpUtility.UrlDecode(path);

				_mapId = path;
			}, "Error opening wire frame data source");
		}

		public override void Close()
		{
			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.VerboseDebug($"{nameof(WireFrameDatasourceBase)}.{nameof(Close)}");
			}
		}

		public override PluginTableTemplate OpenTable([NotNull] string name)
		{
			WireFrameTable result = null;
			Try(() =>
			    {
				    Assert.ArgumentNotNull(name, nameof(name));

				    // The given name is one of those returned by GetTableNames()
				    _msg.Debug($"Open wire frame table '{name}'");

				    if (name == _tableNames[0])
				    {
					    result = new WireFrameTable(_mapId, name);
				    }
				    else
				    {
					    _msg.Warn($"Cannot find data source of wire frame table: {name}");
				    }
			    }, $"Error opening wire frame table {name}");

			return result; // TODO is null ok? shouldn't we throw a GeodatabaseException or similar?
		}

		public override IReadOnlyList<string> GetTableNames()
		{
			return _tableNames;
		}

		public override bool IsQueryLanguageSupported()
		{
			return false;
		}

		private static void Try([NotNull] Action action,
		                        [NotNull] string message,
		                        [CallerMemberName] string caller = null)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			try
			{
				_msg.VerboseDebug(() => $"WireFrameDataSource.{caller}");

				action();
			}
			catch (Exception e)
			{
				_msg.Warn(message, e);
			}
		}
	}
}
