using System;
using System.Collections.Generic;
using System.Linq;
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
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.AGP.Editing.Erase
{
	public abstract class EraseToolBase : ConstructionToolBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected EraseToolBase()
		{
			// This is our property:
			RequiresSelection = true;

			SelectionCursor = ToolUtils.GetCursor(Resources.EraseToolCursor);
			SelectionCursorShift = ToolUtils.GetCursor(Resources.EraseToolCursorShift);
		}

		protected override SketchGeometryType GetSketchGeometryType()
		{
			return SketchGeometryType.Polygon;
		}

		protected override SketchGeometryType GetSelectionSketchGeometryType()
		{
			return SketchGeometryType.Rectangle;
		}

		protected override void LogEnteringSketchMode()
		{
			_msg.Info(LocalizableStrings.EraseTool_LogEnteringSketchMode);
		}

		protected override void LogPromptForSelection()
		{
			//string enterMsg = CanUseSelection()
			//		  ? "- To re-use the existing selection, press Enter"
			//		  : string.Empty;

			_msg.InfoFormat(LocalizableStrings.EraseTool_LogPromptForSelection);
		}

		protected override bool CanUseSelection(Dictionary<BasicFeatureLayer, List<long>> selection,
		                                        NotificationCollection notifications = null)
		{
			bool hasPolycurveSelection = false;

			foreach (var layer in selection.Keys.OfType<FeatureLayer>())
			{
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
			var taskFlash =
				QueuedTaskUtils.Run(
					() => ToolUtils.FlashResultPolygonsAsync(activeView, resultFeatures));

			await Task.WhenAll(taskFlash, taskSave);

			return taskSave.Result;
		}

		private IDictionary<Feature, Geometry> CalculateResultFeatures(
			MapView activeView, Polygon sketchPolygon)
		{
			IEnumerable<Feature> selectedFeatures = GetApplicableSelectedFeatures(activeView);

			var resultFeatures = CalculateResultFeatures(selectedFeatures, sketchPolygon);

			return resultFeatures;
		}

		private static IDictionary<Feature, Geometry> CalculateResultFeatures(
			IEnumerable<Feature> selectedFeatures,
			Polygon cutPolygon)
		{
			var result = new Dictionary<Feature, Geometry>();

			foreach (var feature in selectedFeatures)
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

				FeatureClass featureClass = feature.GetTable();
				FeatureClassDefinition classDefinition = featureClass.GetDefinition();
				GeometryType geometryType = classDefinition.GetShapeType();
				bool classHasZ = classDefinition.HasZ();
				bool classHasM = classDefinition.HasM();

				Geometry geometryToStore =
					GeometryUtils.EnsureGeometrySchema(geometry, classHasZ, classHasM);
				feature.SetShape(geometryToStore);
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
