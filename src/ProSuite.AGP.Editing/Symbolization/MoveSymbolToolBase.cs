using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Windows;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// Interactive tool to move the symbol (not the geometry)
/// of a feature, provided there are suitable field overrides.
/// There are two modes: Select (use picker to select features)
/// and Move (click+drag to move the symbol with WYSIWYG feedback).
/// Hold SHIFT to go temporarily back to Select mode and hit ESCAPE
/// to cancel the move and return to Select mode.
/// The tool issues a warning if the selection is not "movable",
/// usually because the symbol has no suitable properties with
/// field overrides.
/// </summary>
/// <remarks>
/// "Movable" symbol properties are: OffsetX and OffsetY on CIMMarker,
/// CIMMarkerPlacementInsidePolygon, CIMMarkerPlacementPolygonCenter.
/// There may be others.
/// </remarks>
public abstract class MoveSymbolToolBase : EditSymbolToolBase
{
	private MapPoint _startPoint;
	private Candidates _candidates;
	private Cursor _moveCursor; // cache

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected MoveSymbolToolBase() : base(new MoveSymbolFeedback())
	{ }

	private new MoveSymbolFeedback DisplayFeedback =>
		(MoveSymbolFeedback) base.DisplayFeedback;

	#region Context menu entry points

	public async Task ResetMoveOverride()
	{
		// Silently ignore if not in move mode:
		if (Mode != ToolMode.Act) return;

		await QueuedTask.Run(() =>
		{
			CancelAction();

			_candidates = PrepareAction(out var message);

			if (_candidates is not null)
			{
				ApplyMove(_candidates, null, null);
				EnterMode(ToolMode.Act); // stay in move mode
			}
			else
			{
				_msg.Info($"Cannot move: {message}");
				EnterMode(ToolMode.Select);
			}
		});
	}

	#endregion

	protected override Cursor ActionModeCursor =>
		_moveCursor ??= CursorUtils.GetCursor(Properties.Resources.MoveSymbolCursor);

	protected override bool IsInAction => _startPoint is not null && _candidates is not null;

	protected override bool StartActionMCT(Point clientPoint)
	{
		_startPoint = ActiveMapView.ClientToMap(clientPoint);
		_candidates = PrepareAction(out var message);

		if (_candidates is not null) return true;

		_msg.Warn($"Cannot move symbol: {message}");
		_msg.Info("Select a feature with “movable” symbol (field overrides on Offset X/Y properties)");

		return false;
	}

	protected override void MoreActionMCT(Point clientPoint)
	{
		var endPoint = ActiveMapView.ClientToMap(clientPoint);
		var (dx, dy) = GetOffsetPoints(ActiveMapView.Map, _startPoint, endPoint);
		DrawPreviewOverlay(_candidates, dx, dy);
		DisplayFeedback.DrawMoveHint(_startPoint, endPoint);
		//SketchTip = anything?
	}

	protected override void EndActionMCT(Point clientPoint)
	{
		var endPoint = ActiveMapView.ClientToMap(clientPoint);
		var (dx, dy) = GetOffsetPoints(ActiveMapView.Map, _startPoint, endPoint);
		ApplyMove(_candidates, dx, dy);
		SketchTip = null;
	}

	protected override void SetupSelectMode()
	{
		_startPoint = null;
		SketchTip = "Select “movable” symbol";
	}

	protected override void SetupActionMode()
	{
		SketchTip = "Click+drag to move";
	}

	protected override string ActionVerb => "move";

	protected override bool CanEnterActionMode(out string message)
	{
		bool canMove = PrepareAction(out message) != null;

		return canMove;
	}

	protected override void CancelAction()
	{
		RemoveAllOverlays();
		_candidates?.Dispose();
		_candidates = null;
		_startPoint = null;
	}

	protected override bool IsSuitableOverride(CIMPrimitiveOverride po)
	{
		if (po is null) return false;
		return po.PropertyName is "OffsetX" or "OffsetY";
	}

	/// <remarks>Returned offset is in points (not map units)</remarks>
	private static ValueTuple<double, double> GetOffsetPoints(Map map, MapPoint startPoint, MapPoint endPoint)
	{
		if (map is null)
			throw new ArgumentNullException(nameof(map));

		if (startPoint is null || startPoint.IsEmpty) return default;
		if (endPoint is null || endPoint.IsEmpty) return default;

		var dx = endPoint.X - startPoint.X;
		var dy = endPoint.Y - startPoint.Y;

		double pointsPerMapUnit = map.GetPointsPerMapUnit();
		dx *= pointsPerMapUnit;
		dy *= pointsPerMapUnit;

		return (dx, dy);
	}

	private static object GetFieldValue(Candidates.Override po, double? dx, double? dy)
	{
		// Check override property:
		// - if it looks like X add dx
		// - if it looks like Y add dy
		// to current value (treating NULL as zero)

		if (po.PropertyName == "OffsetX")
		{
			if (dx is null) return null;
			if (double.IsNaN(dx.Value)) return po.CurrentValue;
			return GetDouble(po.CurrentValue, 0.0) + dx.Value;
		}

		if (po.PropertyName == "OffsetY")
		{
			if (dy is null) return null;
			if (double.IsNaN(dy.Value)) return po.CurrentValue;
			return GetDouble(po.CurrentValue, 0.0) + dy.Value;
		}

		return po.CurrentValue;
	}

	private static double GetDouble(object value, double defaultValue)
	{
		if (value is null or DBNull)
		{
			return defaultValue;
		}

		try
		{
			return Convert.ToDouble(value);
		}
		catch (Exception)
		{
			return defaultValue;
		}
	}

	/// <remarks>Must call on MCT</remarks>
	private void ApplyMove(Candidates candidates, double? dx, double? dy)
	{
		if (candidates is null) return;
		if (!candidates.Any()) return;

		var edop = new EditOperation { Name = Caption };
		int featureCount = 0;

		foreach (var candidate in candidates.Where(c => c.Overrides.Any()))
		{
			// The "distinct" is necessary as more than one override may use the same field!
			var attributes = candidate.Overrides
			                           .DistinctBy(o => o.FieldName)
			                           .ToDictionary(o => o.FieldName,
			                                         o => GetFieldValue(o, dx, dy));

			edop.Modify(candidates.Layer, candidate.OID, attributes);
			featureCount += 1;
		}

		if (edop.IsEmpty)
		{
			_msg.Debug("No symbol moved (no feature has suitable overrides)");
			return;
		}

		_msg.Debug($"{Caption}: starting edit operation (Task ID {Task.CurrentId})");

		var ok = edop.Execute();

		_msg.Debug($"Edit Operation: ok={ok}, done={edop.IsDone}, canceled={edop.IsCanceled}, succeeded={edop.IsSucceeded}");

		if (edop.IsSucceeded)
		{
			string info = $"move by {dx ?? 0}, {dy ?? 0}";
			_msg.Info($"{edop.Name}: {featureCount} feature{(featureCount == 1 ? "" : "s")} updated ({info})");
		}
		else if (edop.IsCanceled)
		{
			_msg.Warn($"{edop.Name}: Canceled");
		}
		else
		{
			_msg.Error($"{edop.Name}: Failed");
		}
	}

	private void DrawPreviewOverlay(Candidates candidates, double dx, double dy)
	{
		if (candidates is null) return;

		foreach (var candidate in candidates)
		{
			var symref = candidate.Symbol.Clone();

			// This symbol has overrides already applied; but of course,
			// we still write our rotational override(s) for this preview
			// (and because of this modification we made a copy/clone above):

			foreach (var po in candidate.Overrides)
			{
				var value = GetFieldValue(po, dx, dy);
				SymbolUtils.ApplyOverride(symref.Symbol, po.PrimitiveName, po.PropertyName, value);
			}

			FeaturePreview.Draw(candidates.Layer, candidate.OID, symref, candidate.Shape);
		}
	}
}
