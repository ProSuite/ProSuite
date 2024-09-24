using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Attributes;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Picker;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI;
using Attribute = ArcGIS.Desktop.Editing.Attributes.Attribute;

namespace ProSuite.AGP.Editing.DestroyAndRebuild;

public abstract class DestroyAndRebuildToolBase : ToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private DestroyAndRebuildFeedback _feedback;

	protected DestroyAndRebuildToolBase() : base(SketchGeometryType.Rectangle) { }

	protected override Cursor SelectionCursorCore =>
		ToolUtils.GetCursor(Resources.DestroyAndRebuildToolCursor);

	protected override SymbolizedSketchTypeBasedOnSelection GetSymbolizedSketch(
		SketchGeometryType selectionSketchGeometryType)
	{
		return new SymbolizedSketchTypeBasedOnSelection(this, selectionSketchGeometryType);
	}

	protected override bool AllowMultiSelection(out string reason)
	{
		reason = "Destroy and rebuild not possible. Please select only one feature.";
		return false;
	}

	protected override Task OnToolActivateCoreAsync(bool hasMapViewChanged)
	{
		// NOTE CompleteSketchOnMouseUp has not to be set before the sketch geometry type.
		// Set it on tool activate. In ctor is not enough.
		CompleteSketchOnMouseUp = true;
		GeomIsSimpleAsFeature = false;

		_feedback = new DestroyAndRebuildFeedback();

		return base.OnToolActivateCoreAsync(hasMapViewChanged);
	}

	protected override Task OnToolDeactivateCoreAsync(bool hasMapViewChanged)
	{
		_feedback?.Clear();
		_feedback = null;

		return base.OnToolDeactivateCoreAsync(hasMapViewChanged);
	}

	protected override async Task OnSelectionChangedCoreAsync(
		MapSelectionChangedEventArgs args)
	{
		if (args.Selection.Count == 0)
		{
			_feedback.Clear();
		}

		await base.OnSelectionChangedCoreAsync(args);
	}

	protected override async Task HandleEscapeAsync()
	{
		var task = QueuedTask.Run(
			() =>
			{
				SelectionUtils.ClearSelection(ActiveMapView?.Map);

				_feedback.Clear();
			});
		await ViewUtils.TryAsync(task, _msg);
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
			_feedback.Clear();
			return false; // startConstructionPhase = false
		}

		_feedback.UpdatePreview(feature.GetShape());

		_msg.Info(
			$"Destroy and rebuild feature {GdbObjectUtils.GetDisplayValue(feature, layer.Name)}");

		_msg.Info("Sketch the new geometry. Hit [ESC] to reselect the target feature.");

		await StartSketchAsync();

		return true; // startConstructionPhase = true
	}

	protected override async Task<bool> OnConstructionSketchCompleteAsync(
		Geometry geometry, IDictionary<BasicFeatureLayer, List<long>> selectionByLayer)
	{
		// todo daro: assert instead?
		if (selectionByLayer.Count == 0)
		{
			_msg.Debug("no selection");
			_feedback.Clear();

			return true; // startSelectionPhase = true;
		}

		(BasicFeatureLayer layer, List<long> oids) = selectionByLayer.FirstOrDefault();

		if (oids.Count == 0)
		{
			_msg.Debug("no selection");
			_feedback.Clear();

			return true; // startSelectionPhase = true;
		}

		long selectedOid = oids.FirstOrDefault();

		try
		{
			Inspector inspector = new Inspector();
			await inspector.LoadAsync(layer, selectedOid);
			inspector.Shape = GeometryUtils.Simplify(geometry);

			Attribute subtype = inspector.SubtypeAttribute;

			string subtypeName = subtype?.CurrentSubtype != null
				                     ? subtype.CurrentSubtype.Name
				                     : layer.Name;

			// note: TooltipHeading is null here.
			var operation = new EditOperation
			                {
				                Name = $"Destroy and Rebuild {subtypeName}",
				                SelectModifiedFeatures = true
			                };

			// todo daro move to base? make utils?
			operation.Modify(inspector);

			if (operation.IsEmpty)
			{
				_msg.Debug($"{Caption}: edit operation is empty");
				return false;
			}

			bool succeed = false;
			try
			{
				succeed = await operation.ExecuteAsync();
			}
			catch (Exception e)
			{
				_msg.Debug($"{Caption}: edit operation threw an exception", e);
			}
			finally
			{
				if (succeed)
				{
					_msg.Info($"Updated feature in {layer.Name} ({subtypeName}) ID: {selectedOid}");
				}
				else
				{
					_msg.Debug($"{Caption}: edit operation failed");
				}
			}

			_feedback.Clear();

			LogPromptForSelection();
			return true; // startSelectionPhase = true;
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
			return true; // startSelectionPhase = true;
		}
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
		_msg.Info("Select feature for destroy and rebuild.");
	}

	protected override bool CanSelectFromLayerCore(BasicFeatureLayer layer)
	{
		return layer is FeatureLayer;
	}
}
