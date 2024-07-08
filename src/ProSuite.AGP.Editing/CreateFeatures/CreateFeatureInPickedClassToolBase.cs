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
using ProSuite.AGP.Editing.OneClick;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Misc;
using Attribute = ArcGIS.Desktop.Editing.Attributes.Attribute;

namespace ProSuite.AGP.Editing.CreateFeatures;

public abstract class CreateFeatureInPickedClassToolBase : ToolBase
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private static readonly Latch _latch = new();

	protected CreateFeatureInPickedClassToolBase(SketchGeometryType sketchGeometryType) : base(
		sketchGeometryType) { }

	protected override Cursor SelectionCursorCore =>
		ToolUtils.GetCursor(Resources.CreateFeatureInPickedClassCursor);

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
		if (_latch.IsLatched)
		{
			return false; // startContructionPhase = false
		}

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

		(BasicFeatureLayer layer, List<long> oids) = selectionByLayer.FirstOrDefault();

		if (oids.Count == 0)
		{
			_msg.Debug("no selection");

			return true; // startSelectionPhase = true;
		}

		long selectedOid = oids.FirstOrDefault();

		try
		{
			Inspector inspector = new Inspector();
			await inspector.LoadAsync(layer, selectedOid);
			ProSuite.Commons.AGP.Core.Spatial.GeometryUtils.Simplify(geometry);
			inspector.Shape = geometry;

			Attribute subtype = inspector.SubtypeAttribute;

			string subtypeName = subtype?.CurrentSubtype != null
				                     ? subtype.CurrentSubtype.Name
				                     : layer.Name;

			// note: TooltipHeading is null here.
			var operation = new EditOperation
			                {
				                Name = $"Create {subtypeName}",
				                SelectNewFeatures = true
			                };

			// todo daro move to base? make utils?
			RowToken rowToken = operation.Create(inspector.MapMember,
			                                     inspector.ToDictionary(
				                                     field => field.FieldName,
				                                     field => field.CurrentValue));

			if (operation.IsEmpty)
			{
				_msg.Debug($"{Caption}: edit operation is empty");
				return false;
			}

			bool succeed = false;
			try
			{
				// todo daro latched operation in ToolBase?
				_latch.Increment();
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
					_msg.Info(
						$"Created new feature {layer.Name} ({subtypeName}) ID: {rowToken.ObjectID}");
				}
				else
				{
					_msg.Debug($"{Caption}: edit operation failed");
				}

				_latch.Decrement();
			}

			return false; // startSelectionPhase = false;
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message, ex);
			return false; // startSelectionPhase = false;
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
		_msg.Info("Select a template feature");
	}

	protected override bool CanSelectFromLayerCore(BasicFeatureLayer layer)
	{
		return layer is FeatureLayer;
	}
}
