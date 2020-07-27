using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
//using Commons.Logger;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Clients.AGP.ProSuiteSolution.Layers
{
	public class LayerUtils
    {
        public static IList<FeatureLayer> AddFeaturesToMap(string groupLayer, string path, string featureName = null, IList<string> layernames = null, bool select = true)
        {
			// is map visible?
			if (!(MapView.Active != null || String.IsNullOrEmpty(path))) return null;

			// are data zipped?
			var featuresGdb = path.Replace("\n", String.Empty).Replace("\r", String.Empty).Replace(@"\\", @"\");
			if (Path.GetExtension(featuresGdb) == ".zip")
			{
				var extractDir = Path.GetDirectoryName(path);
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

					//ProSuiteLogger.Logger.Log(LogType.Info, $"LayerUtils: QA error layers were added to map");

					if (select)
					{
						SelectLayersInMap(layerList);
						//SelectLayersInTOC(layerList);
					}
				}
				catch (Exception ex)
				{
					//ProSuiteLogger.Logger.Log(LogType.Error, $"LayerUtils: {ex.Message}");
				}
			});
			return layerList;
        }

		public static void SelectLayersInMap(IEnumerable<FeatureLayer> layers = null)
		{
			if (MapView.Active == null) return;

			QueuedTask.Run(() =>
			{
				foreach (var layer in layers)
				{
					layer.Select();
				}
			});

		}


		public static void SelectLayersInTOC(IEnumerable<FeatureLayer> layers = null)
		{
			if (MapView.Active == null) return;

			var featureLayers = layers ?? MapView.Active.Map.Layers.OfType<FeatureLayer>();
			MapView.Active.SelectLayers(featureLayers.ToList());
		}

		public void ShowNotification(string title, string message, string icon)
		{
			QueuedTask.Run(() =>
			{
				FrameworkApplication.AddNotification(new Notification()
				{
					Message = "QA results are added to map",
					Title = "ProSuite Tools",
					ImageUrl = PackUriForResource("AddInDesktop32.png").AbsoluteUri
				});
			});
		}

		private Uri PackUriForResource(string resourceName, string folderName = "Images")
		{
			string asm = System.IO.Path.GetFileNameWithoutExtension(
				System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
			string uriString = folderName.Length > 0
				? string.Format("pack://application:,,,/{0};component/{1}/{2}", asm, folderName, resourceName)
				: string.Format("pack://application:,,,/{0};component/{1}", asm, resourceName);
			return new Uri(uriString, UriKind.Absolute);
		}


	}
}
