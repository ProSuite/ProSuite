using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Erase
{
	public abstract class EraseToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// </summary>
		protected EraseToolBase()
		{
			// This is our property:
			RequiresSelection = true;

			SelectionCursor = ToolUtils.GetCursor(Resources.EraseToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.EraseToolCursorShift);
		}

		//protected override SketchGeometryType GetSketchGeometryType()
		//{
		//	return SketchGeometryType.Polygon;
		//}

		protected override void LogEnteringSketchMode()
		{
			_msg.Info("Define the hole.&lt;br&gt;Hit [ESC] to reselect the polygon(s).");
		}

		protected override void LogPromptForSelection()
		{
			//string enterMsg = CanUseSelection()
			//		  ? "- To re-use the existing selection, press Enter"
			//		  : string.Empty;

			_msg.InfoFormat(
				"Select one or more polygon features.&lt;br&gt;- Press and hold SHIFT to add or remove features from the existing selection.&lt;br&gt;- Press and hold P to draw a polygon that completely contains the features to be selected. Finish the polygon with double-click.&lt;br&gt;");
		}

		protected override bool CanUseSelection(Dictionary<MapMember, List<long>> selection)
		{
			bool hasPolycurveSelection = false;

			foreach (MapMember mapMember in selection.Keys)
			{
				var layer = mapMember as FeatureLayer;

				if (layer == null)
				{
					continue;
				}

				if (layer.ShapeType == esriGeometryType.esriGeometryPolygon ||
				    layer.ShapeType == esriGeometryType.esriGeometryPolyline)
				{
					hasPolycurveSelection = true;
				}
			}

			return hasPolycurveSelection;
		}

		protected override CancelableProgressor GetSelectionProgressor()
		{
			var selectionCompleteProgressorSource = new CancelableProgressorSource(
				"Selecting features bla bla...", "cancelled", true);

			CancelableProgressor selectionProgressor = selectionCompleteProgressorSource.Progressor;

			return selectionProgressor;
		}

		/// <summary>
		/// </summary>
		/// <param name="sketchGeometry"></param>
		/// <param name="editTemplate"></param>
		/// <param name="activeView"></param>
		/// <param name="cancelableProgressor"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		protected override async Task<bool> OnEditSketchCompleteCoreAsync(
			Geometry sketchGeometry,
			EditingTemplate editTemplate,
			MapView activeView,
			CancelableProgressor cancelableProgressor = null)
		{
			var polygon = (Polygon) sketchGeometry;

			var resultFeatures = await QueuedTaskUtils.Run(
				                     () => CalculateResultFeatures(activeView, polygon),
				                     cancelableProgressor);

			var taskSave = QueuedTaskUtils.Run(() => SaveAsync(resultFeatures));
			var taskFlash = QueuedTaskUtils.Run(() => FlashAsync(activeView, resultFeatures));

			await Task.WhenAll(taskFlash, taskSave);

			return taskSave.Result;
		}

		private static IDictionary<Feature, Geometry> CalculateResultFeatures(
			MapView activeView, Polygon sketchPolygon)
		{
			Dictionary<MapMember, List<long>> selectedFeatures = activeView.Map.GetSelection();

			var resultFeatures = CalculateResultFeatures(selectedFeatures, sketchPolygon);

			return resultFeatures;
		}

		private static IDictionary<Feature, Geometry> CalculateResultFeatures(
			Dictionary<MapMember, List<long>> selection,
			Polygon cutPolygon)
		{
			var result = new Dictionary<Feature, Geometry>();

			foreach (var feature in MapUtils.GetFeatures(selection))
			{
				Geometry featureGeometry = feature.GetShape();
				featureGeometry = GeometryEngine.Instance.SimplifyAsFeature(featureGeometry, true);
				cutPolygon = (Polygon) GeometryEngine.Instance.SimplifyAsFeature(cutPolygon, true);
				cutPolygon =
					(Polygon) GeometryEngine.Instance.Project(cutPolygon,
					                                          featureGeometry.SpatialReference);

				Geometry resultGeometry =
					GeometryEngine.Instance.Difference(featureGeometry, cutPolygon);

				if (resultGeometry.IsEmpty)
				{
					throw new Exception("One or more result geometries have become empty.");
				}

				result.Add(feature, resultGeometry);
			}

			return result;
		}

		private async Task<bool> FlashAsync(MapView activeView,
		                                    IDictionary<Feature, Geometry> resultFeatures)
		{
			if (resultFeatures.Count > 5)
			{
				_msg.InfoFormat("{0} have been updated. No flashing will be performed.",
				                resultFeatures.Count);
			}

			var polySymbol = SymbolFactory.Instance.DefaultPolygonSymbol;

			foreach (Geometry resultGeometry in resultFeatures.Values)
			{
				using (await activeView.AddOverlayAsync(resultGeometry,
				                                        polySymbol.MakeSymbolReference()))
				{
					await Task.Delay(400);
				}
			}

			return true;
		}

		private static async Task<bool> SaveAsync(IDictionary<Feature, Geometry> result)
		{
			// create an edit operation
			var editOperation = new EditOperation();

			EditorTransaction transaction = new EditorTransaction(editOperation);

			return await transaction.ExecuteAsync(
				       editContext => Store(editContext, result),
				       "Erase polygon from feature(s)", GetDatasets(result.Keys));
		}

		private static bool Store(
			EditOperation.IEditContext editContext,
			IDictionary<Feature, Geometry> result)
		{
			foreach (KeyValuePair<Feature, Geometry> keyValuePair in result)
			{
				Feature feature = keyValuePair.Key;
				Geometry geometry = keyValuePair.Value;

				if (geometry.IsEmpty)
				{
					throw new Exception("One or more result geometries have become empty.");
				}

				feature.SetShape(geometry);
				feature.Store();

				editContext.Invalidate(feature);

				feature.Dispose();
			}

			_msg.InfoFormat("Successfully stored {0} updated features.", result.Count);

			return true;
		}

		private static IEnumerable<Dataset> GetDatasets(IEnumerable<MapMember> mapMembers)
		{
			foreach (MapMember mapMember in mapMembers)
			{
				var featureLayer = mapMember as FeatureLayer;

				if (featureLayer != null)
				{
					yield return featureLayer.GetFeatureClass();
				}

				var standaloneTable = mapMember as StandaloneTable;

				if (standaloneTable != null)
				{
					yield return standaloneTable.GetTable();
				}
			}
		}

		private static IEnumerable<Dataset> GetDatasets(IEnumerable<Feature> features)
		{
			foreach (Feature feature in features)
			{
				yield return feature.GetTable();
			}
		}
	}
}
