using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
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
		Geometry geometry, IDictionary<BasicFeatureLayer, List<long>> selectionByLayer)
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

				await StoreNewFeature(featureLayer, originalFeature, geometry, GetExclusionFieldNames());

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

	private async Task StoreNewFeature([NotNull] BasicFeatureLayer featureLayer,
	                                   [NotNull] Feature originalFeature,
	                                   [NotNull] Geometry sketchGeometry,
	                                   [CanBeNull] ICollection<string> exclusionFieldNames)
	{
		// Prevent invalid Z values and other non-simple geometries:
		Geometry simplifiedSketch =
			Assert.NotNull(GeometryUtils.Simplify(sketchGeometry), "Geometry is null");

		Subtype featureSubtype = GdbObjectUtils.GetSubtype(originalFeature);

		string subtypeName = featureSubtype != null
			                     ? featureSubtype.GetName()
			                     : featureLayer.Name;

		Feature newFeature = null;
		bool transactionSucceeded =
			await GdbPersistenceUtils.ExecuteInTransactionAsync(
				editContext =>
				{
					newFeature = GdbPersistenceUtils.InsertTx(
						editContext, originalFeature, simplifiedSketch, exclusionFieldNames);

					return true;
				}, $"Create {subtypeName}",
				new[] { originalFeature.GetTable() });

		if (transactionSucceeded)
		{
			SelectionUtils.ClearSelection(MapView.Active.Map);
			SelectionUtils.SelectFeature(featureLayer, SelectionCombinationMethod.New,
			                             newFeature.GetObjectID());
			_msg.Info(
				$"Created new feature {featureLayer.Name} ({subtypeName}) ID: {newFeature.GetObjectID()}");
		}
		else
		{
			_msg.Warn($"{Caption}: edit operation failed");
		}
	}
}
