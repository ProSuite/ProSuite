using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Solution.Commons
{
	public static class LayerUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IList<FeatureLayer> AddFeaturesToMap(string groupLayer, string path, string featureName = null, IList<string> layernames = null, bool select = true)
        {
			// is map visible?
			if (MapView.Active == null) return null;
			if (string.IsNullOrEmpty(path)) return null;

			// are data zipped?
			var featuresGdb = path.Replace("\n", string.Empty).Replace("\r", string.Empty).Replace(@"\\", @"\");
			if (string.Equals(Path.GetExtension(featuresGdb), ".zip", StringComparison.OrdinalIgnoreCase))
			{
				var extractDir = Path.GetDirectoryName(path);
				if (extractDir == null) return null;
				ZipFile.ExtractToDirectory(path, extractDir);
				featuresGdb = Path.Combine(extractDir, Path.GetFileNameWithoutExtension(path), featureName);
				if (!Directory.Exists(featuresGdb)) return null;
			}

			var layerList = new List<FeatureLayer>();
			QueuedTask.Run(() =>
			{
				try
				{
					// TODO remove previous error layers?

					// TODO get layernames from workspace if null?

					var newGroupLayer = LayerFactory.Instance.CreateGroupLayer(MapView.Active.Map, 0, groupLayer);
					Envelope commonExtent = null;

					foreach (var layername in layernames)
					{
						var featureLayer = LayerFactory.Instance.CreateFeatureLayer(new Uri(Path.Combine(featuresGdb, layername)), newGroupLayer);
						layerList.Add(featureLayer);
						commonExtent = featureLayer.QueryExtent();
					}

					MapView.Active.ZoomTo(commonExtent, TimeSpan.FromSeconds(0));
					MapView.Active.ZoomOutFixed(TimeSpan.FromSeconds(0));

					_msg.Info($"LayerUtils: QA error layers were added to map");

					if (select)
					{
						SelectLayersInMap(layerList);
						//SelectLayersInTOC(layerList);
					}
				}
				catch (Exception ex)
				{
					_msg.Error($"LayerUtils: {ex.Message}");
				}
			});
			return layerList;
        }

		public static void SelectLayersInMap(IEnumerable<FeatureLayer> layers = null)
		{
			if (MapView.Active == null) return;
			if (layers == null) return;

			QueuedTask.Run(() =>
			{
				foreach (var layer in layers)
				{
					layer.Select();
				}
			});
		}

		public static void SetLayerSelectability([NotNull] Layer layer, bool selectable)
		{
			var cimDefinition = (CIMFeatureLayer)layer.GetDefinition();
			cimDefinition.Selectable = selectable;
			layer.SetDefinition(cimDefinition);
		}

		public static void SelectLayersInTOC(IEnumerable<FeatureLayer> layers = null)
		{
			if (MapView.Active == null) return;

			var featureLayers = layers ?? MapView.Active.Map.Layers.OfType<FeatureLayer>();
			MapView.Active.SelectLayers(featureLayers.ToList());
		}

		public static void ShowNotification(string title, string message, string icon)
		{
			QueuedTask.Run(() =>
			{
				FrameworkApplication.AddNotification(new Notification
				                                     {
					Message = "QA results are added to map",
					Title = "ProSuite Tools",
					ImageUrl = PackUriForResource("AddInDesktop32.png").AbsoluteUri
				});
			});
		}

		private static Uri PackUriForResource(string resourceName, string folderName = "Images")
		{
			string asm = Path.GetFileNameWithoutExtension(
				System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
			string uriString = folderName.Length > 0
				? string.Format("pack://application:,,,/{0};component/{1}/{2}", asm, folderName, resourceName)
				: string.Format("pack://application:,,,/{0};component/{1}", asm, resourceName);
			return new Uri(uriString, UriKind.Absolute);
		}


	}
}
