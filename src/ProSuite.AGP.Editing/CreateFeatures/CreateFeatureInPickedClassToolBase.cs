using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;

namespace ProSuite.AGP.Editing.CreateFeatures;

public abstract class CreateFeatureInPickedClassToolBase : ToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	[CanBeNull]
	protected virtual ICollection<string> GetExclusionFieldNames()
	{
		return null;
	}

	protected override SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch()
	{
		return new SymbolizedSketchTypeBasedOnSelection(this);
	}

	protected override Cursor GetSelectionCursor()
	{
		return ToolUtils.CreateCursor(Resources.Arrow,
		                              Resources.CreateFeatureInPickedClassOverlay,
		                              null);
	}

	protected override Cursor GetSelectionCursorLasso()
	{
		return ToolUtils.CreateCursor(Resources.Arrow,
		                              Resources.CreateFeatureInPickedClassOverlay,
		                              Resources.Lasso);
	}

	protected override Cursor GetSelectionCursorPolygon()
	{
		return ToolUtils.CreateCursor(Resources.Arrow,
		                              Resources.CreateFeatureInPickedClassOverlay,
		                              Resources.Polygon);
	}

	protected override bool AllowMultiSelection(out string reason)
	{
		reason = "Cannot create feature. Please select only one template feature.";
		return false;
	}

	protected override CancelableProgressorSource GetProgressorSource()
	{
		return null;
	}

	protected override Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		// NOTE CompleteSketchOnMouseUp has not to be set before the sketch geometry type.
		// Set it on tool activate. In ctor is not enough.
		CompleteSketchOnMouseUp = true;
		GeomIsSimpleAsFeature = false;

		return base.OnToolActivateCoreAsync(hasMapViewChanged);
	}

	protected override async Task HandleEscapeAsync()
	{
		await QueuedTask.Run(() => SelectionUtils.ClearSelection(ActiveMapView?.Map));
	}

	protected override async Task<bool> ProcessSelectionCoreAsync(
		IDictionary<BasicFeatureLayer, List<Feature>> featuresByLayer,
		CancelableProgressor progressor = null)
	{
		Assert.ArgumentCondition(featuresByLayer.Count == 1, "selection count has to be 1");

		(BasicFeatureLayer layer, List<Feature> features) = featuresByLayer.FirstOrDefault();

		Feature feature = features?.FirstOrDefault();

		// todo daro: assert instead?
		if (feature == null)
		{
			_msg.Debug("no selection");
			return false; // startContructionPhase = false
		}

		_msg.Info(
			$"Currently selected template feature {GdbObjectUtils.GetDisplayValue(feature, layer.Name)}");

		_msg.Info("Construct the new feature. Hit [ESC] to reselect the template feature.");

		await StartSketchAsync();

		return true; // startContructionPhase = true
	}

	protected override async Task<bool> OnConstructionSketchCompleteAsync(
		Geometry geometry, IDictionary<BasicFeatureLayer, List<long>> selectionByLayer,
		CancelableProgressor progressor)
	{
		// todo daro: assert instead?
		if (selectionByLayer.Count == 0)
		{
			_msg.Debug("no selection");

			return true; // startSelectionPhase = true;
		}

		await QueuedTaskUtils.Run(async () =>
		{
			try
			{
				var applicableSelection =
					SelectionUtils.GetApplicableSelectedFeatures(
						selectionByLayer, CanSelectFromLayer);

				List<Feature> selectedFeatures = applicableSelection.Values.FirstOrDefault();

				if (selectedFeatures == null || selectedFeatures.Count == 0)
				{
					_msg.Debug("no applicable selection");
					return true;
				}

				BasicFeatureLayer featureLayer = selectionByLayer.Keys.First();
				Feature originalFeature = selectedFeatures.First();

				await StoreNewFeature(featureLayer, originalFeature, geometry,
				                      GetExclusionFieldNames(), progressor);

				return false; // startSelectionPhase = false;
			}
			catch (Exception ex)
			{
				_msg.Error(ex.Message, ex);
				return false; // startSelectionPhase = false;
			}
		});

		return false; // startSelectionPhase = false;
	}

	protected override bool CanSelectGeometryType(GeometryType geometryType)
	{
		switch (geometryType)
		{
			case GeometryType.Point:
			case GeometryType.Polyline:
			case GeometryType.Polygon:
			case GeometryType.Multipoint:
			case GeometryType.Multipatch:
				return true;
			case GeometryType.Unknown:
			case GeometryType.Envelope:
			case GeometryType.GeometryBag:
				_msg.Debug($"{Caption}: cannot select from geometry of type {geometryType}");
				return false;
			default:
				throw new ArgumentOutOfRangeException(nameof(geometryType), geometryType, null);
		}
	}

	protected override void LogPromptForSelection()
	{
		_msg.Info("Select a template feature");
	}

	protected override bool CanSelectFromLayerCore(BasicFeatureLayer layer)
	{
		return layer is FeatureLayer;
	}

	protected override void StartConstructionPhaseCore()
	{
		if (QueuedTask.OnWorker)
		{
			ResetSketchVertexSymbolOptions();
		}
		else
		{
			QueuedTask.Run(ResetSketchVertexSymbolOptions);
		}
	}

	protected override void StartSelectionPhaseCore()
	{
		if (QueuedTask.OnWorker)
		{
			SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
			SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
		}
		else
		{
			QueuedTask.Run(() =>
			{
				SetTransparentVertexSymbol(VertexSymbolType.RegularUnselected);
				SetTransparentVertexSymbol(VertexSymbolType.CurrentUnselected);
			});
		}
	}

	protected override void SetSketchTypeCore(SketchGeometryType? sketchType)
	{
		if (sketchType == SketchGeometryType.Point)
		{
			base.SetSketchTypeCore(SketchGeometryType.Multipoint);
		}
		else
		{
			base.SetSketchTypeCore(sketchType);
		}
	}

	private async Task StoreNewFeature([NotNull] BasicFeatureLayer featureLayer,
	                                   [NotNull] Feature originalFeature,
	                                   [NotNull] Geometry sketchGeometry,
	                                   [CanBeNull] ICollection<string> exclusionFieldNames,
	                                   CancelableProgressor progressor)
	{
		// Prevent invalid Z values and other non-simple geometries:
		Geometry simplifiedSketch =
			Assert.NotNull(GeometryUtils.Simplify(sketchGeometry), "Geometry is null");

		// todo: dispose?
		// maybe don't dispose because it gets opened again
		using FeatureClass targetFeatureClass = originalFeature.GetTable();

		Subtype featureSubtype = GdbObjectUtils.GetSubtype(originalFeature);

		string subtypeName = featureSubtype != null
			                     ? featureSubtype.GetName()
			                     : featureLayer.Name;

		var description = $"Create {subtypeName}";

		List<Feature> newFeatures = null;

		bool succeeded =
			await GdbPersistenceUtils.ExecuteInTransactionAsync(
				editContext =>
				{
					if (simplifiedSketch is Multipoint multipoint)
					{
						newFeatures =
						[
							..CreatePointFeatures(editContext,
							                      targetFeatureClass,
							                      originalFeature, multipoint,
							                      exclusionFieldNames, progressor)
						];
					}
					else
					{
						newFeatures =
						[
							GdbPersistenceUtils.InsertTx(
								editContext, originalFeature, simplifiedSketch,
								exclusionFieldNames)
						];
					}

					return newFeatures.Count > 0;
				}, description, [targetFeatureClass]);

		if (succeeded)
		{
			Assert.NotNull(newFeatures);
			Assert.True(newFeatures.Count > 0, "no result features");

			SelectionUtils.ClearSelection(MapView.Active.Map);

			List<long> oids = newFeatures.Select(f => f.GetObjectID()).ToList();

			// Select only the last point because it's a single selection tool.
			SelectionUtils.SelectFeature(featureLayer, SelectionCombinationMethod.New, oids.Last());

			_msg.Info(
				$"Created new {(oids.Count > 1 ? "features" : "feature")} {featureLayer.Name} ({subtypeName}) {(oids.Count > 1 ? "IDs" : "ID")}: {StringUtils.Concatenate(oids, ",")}");
		}
		else
		{
			_msg.Warn($"{Caption}: edit operation failed");
		}
	}

	private static IEnumerable<Feature> CreatePointFeatures(EditOperation.IEditContext editContext,
	                                                        FeatureClass targetFeatureClass,
	                                                        Feature originalFeature,
	                                                        Multipoint multipoint,
	                                                        ICollection<string> exclusionFieldNames,
	                                                        CancelableProgressor progressor = null)
	{
		using FeatureClassDefinition featureClassDefinition = targetFeatureClass.GetDefinition();

		var pointGeometries = multipoint.Points.Select(
			p => CreatePointGeometry(p, featureClassDefinition)).ToList();

		var copies = new Dictionary<Feature, IList<Geometry>>
		             { { originalFeature, pointGeometries } };

		return GdbPersistenceUtils.InsertTx(editContext, copies, exclusionFieldNames, progressor);
	}

	private static Geometry CreatePointGeometry(MapPoint point,
	                                            FeatureClassDefinition classDefinition)
	{
		GeometryType geometryType = classDefinition.GetShapeType();
		bool classHasZ = classDefinition.HasZ();
		bool classHasM = classDefinition.HasM();

		Assert.True(geometryType == GeometryType.Point ||
		            geometryType == GeometryType.Multipoint,
		            "Invalid target feature class.");

		return geometryType == GeometryType.Point
			       ? CreatePoint(point, classHasZ, classHasM)
			       : CreateSingleMultipoint(point, classHasZ, classHasM);
	}

	private static Geometry CreateSingleMultipoint(MapPoint point, bool zAware, bool mAware)
	{
		var mapPointBuilder = new MultipointBuilderEx(point);

		mapPointBuilder.HasZ = zAware;
		mapPointBuilder.HasM = mAware;

		return mapPointBuilder.ToGeometry();
	}

	private static Geometry CreatePoint(MapPoint point, bool zAware, bool mAware)
	{
		var mapPointBuilder = new MapPointBuilderEx(point);

		mapPointBuilder.HasZ = zAware;
		mapPointBuilder.HasM = mAware;

		return mapPointBuilder.ToGeometry();
	}
}
