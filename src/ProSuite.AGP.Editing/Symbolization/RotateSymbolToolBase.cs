using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons;
using ProSuite.Commons.AGP.Core.Carto;
using ProSuite.Commons.AGP.Windows;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Symbolization;

/// <summary>
/// Interactive map tool to rotate the symbol (not the geometry)
/// of a feature, provided there are suitable field overrides.
/// There are two modes: Select (use picker to select features)
/// and Rotate (click+drag to rotate the symbol with WYSIWYG feedback).
/// Hold SHIFT to go temporarily back to Select mode and hit
/// ESCAPE to cancel rotation and return to Select mode.
/// The tool issues a warning if the selection is not "rotatable",
/// usually because the symbol has no suitable property with a
/// field override.
/// </summary>
/// <remarks>
/// "Rotatable" symbol properties are: CIMMarker.Rotation and
/// CIMMarkerPlacementInsidePolygon.GridAngle. There may be
/// others. CIMPointSymbol.Angle is NOT rotatable (because
/// CIMPointSymbol is not a primitive).
/// </remarks>
public abstract class RotateSymbolToolBase : EditSymbolToolBase
{
	private MapPoint _startPoint;
	private Candidates _candidates;
	private Cursor _rotateCursor; // cache

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected RotateSymbolToolBase() : base(new RotateSymbolFeedback())
	{
		RotationKind = SymbolRotationKind.Additive;
	}

	private new RotateSymbolFeedback DisplayFeedback =>
		(RotateSymbolFeedback) base.DisplayFeedback;

	#region Context Menu entry points

	public SymbolRotationKind RotationKind { get; private set; }

	public async Task SetRotationKind(SymbolRotationKind rotationKind)
	{
		await QueuedTask.Run(CancelAction);

		RotationKind = rotationKind;
	}

	public async Task ResetRotationOverride()
	{
		// Silently ignore if not in rotate mode:
		if (Mode != ToolMode.Act) return;

		await QueuedTask.Run(() =>
		{
			CancelAction();

			_candidates = PrepareAction(out var message);

			if (_candidates is not null)
			{
				ApplyRotation(_candidates, null);
				EnterMode(ToolMode.Act); // stay in rotate mode
			}
			else
			{
				_msg.Info($"Cannot rotate: {message}");
				EnterMode(ToolMode.Select);
			}
		});
	}

	#endregion

	protected override Cursor ActionModeCursor =>
		_rotateCursor ??= CursorUtils.GetCursor(Properties.Resources.RotateSymbolCursor);

	protected override bool IsInAction => _startPoint is not null && _candidates is not null;

	protected override bool StartActionMCT(Point clientPoint)
	{
		_startPoint = ActiveMapView.ClientToMap(clientPoint);
		_candidates = PrepareAction(out var message);

		if (_candidates is not null) return true;

		_msg.Warn($"Cannot rotate symbol: {message}");
		_msg.Info("Select a feature with a “rotatable” symbol (field override on a Rotation property)");

		return false;
	}

	protected override void MoreActionMCT(Point clientPoint)
	{
		var endPoint = ActiveMapView.ClientToMap(clientPoint);

		if (RotationKind == SymbolRotationKind.Additive)
		{
			var centerPoint = _candidates.ReferencePoint;
			var angle = GetAngleDegrees(_startPoint, endPoint, centerPoint);

			DrawPreviewOverlay(_candidates, angle);
			DisplayFeedback.DrawRotateHint(_startPoint, double.IsNaN(angle) ? null : endPoint, centerPoint);
			SketchTip = null; // don't show angle value for additive rotation
		}
		else
		{
			var angle = GetAngleDegrees(_startPoint, endPoint);

			DrawPreviewOverlay(_candidates, angle);
			DisplayFeedback.DrawRotateHint(_startPoint, double.IsNaN(angle) ? null : endPoint, null);
			SketchTip = double.IsNaN(angle) ? "N/A" : $"{angle:F0}°"; // works, even if not sketching
		}
	}

	protected override void EndActionMCT(Point clientPoint)
	{
		double angle;
		var endPoint = ActiveMapView.ClientToMap(clientPoint);

		if (RotationKind == SymbolRotationKind.Additive)
		{
			var centerPoint = _candidates.ReferencePoint;
			angle = GetAngleDegrees(_startPoint, endPoint, centerPoint);
		}
		else
		{
			angle = GetAngleDegrees(_startPoint, endPoint);
		}

		if (double.IsNaN(angle)) return;
		ApplyRotation(_candidates, angle);
		SketchTip = null;
	}

	protected override void SetupSelectMode()
	{
		_startPoint = null;
		SketchTip = "Select “rotatable” symbol";
	}

	protected override void SetupActionMode()
	{
		SketchTip = "Click+drag to rotate";
	}

	protected override string ActionVerb => "rotate";

	protected override bool CanEnterActionMode(out string message)
	{
		bool canRotate = PrepareAction(out message) != null;

		return canRotate;
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
		// Unsure... want: PropertyName sounds like rotation
		return po.PropertyName is "Angle" or "Rotation" or "GridAngle";
	}

	/// <summary>
	/// Angle between the positive X axis and a line from
	/// <paramref name="startPoint"/> to <paramref name="endPoint"/>,
	/// ccw, in degrees.
	/// </summary>
	/// <returns>an angle, or <c>NaN</c> if either point is null
	/// or empty or if the two points coincide</returns>
	private static double GetAngleDegrees(MapPoint startPoint, MapPoint endPoint)
	{
		if (startPoint is null || startPoint.IsEmpty) return double.NaN;
		if (endPoint is null || endPoint.IsEmpty) return double.NaN;

		var dx = endPoint.X - startPoint.X;
		var dy = endPoint.Y - startPoint.Y;

		if (Math.Abs(dx) < double.Epsilon && Math.Abs(dy) < double.Epsilon)
		{
			// startPoint and endPoint coincide: Atan2 reasonably
			// returns zero, but we want to preserve this special case:
			return double.NaN;
		}

		var radians = Math.Atan2(dy, dx); // -pi .. pi
		var angle = MathUtils.ToDegrees(radians); // -180 .. 180

		//angle %= 360; // paranoia
		//if (angle < 0) angle += 360;

		return angle;
	}

	private static double GetAngleDegrees(MapPoint startPoint, MapPoint endPoint, MapPoint centerPoint)
	{
		var angle0 = GetAngleDegrees(centerPoint, startPoint);
		var angle1 = GetAngleDegrees(centerPoint, endPoint);
		return angle1 - angle0;
	}

	private double? GetFieldValue(double? angle, object currentValue)
	{
		if (angle is null) return null;

		if (RotationKind == SymbolRotationKind.Additive)
		{
			if (currentValue is null || currentValue is DBNull)
			{
				return angle;
			}

			try
			{
				var currentAngle = Convert.ToDouble(currentValue);
				return currentAngle + angle;
			}
			catch (Exception)
			{
				return angle;
			}
		}

		return angle;
	}

	/// <summary>
	/// Normalize the given angle (in degrees) to the range 0 (inclusive)
	/// to 360 (exclusive). Subclass may override to change this behavior.
	/// </summary>
	/// <param name="fieldName">Rotation override field name</param>
	/// <param name="angleDegrees">Rotation override angle in degrees</param>
	/// <returns>Normalized angle or <c>null</c></returns>
	protected virtual double? NormalizeFieldValue(string fieldName, double? angleDegrees)
	{
		if (angleDegrees is null) return null;

		angleDegrees %= 360;

		if (angleDegrees < 0)
		{
			angleDegrees += 360;
		}

		return angleDegrees;
	}

	/// <remarks>Must run on MCT</remarks>
	private void ApplyRotation(Candidates candidates, double? angle)
	{
		if (candidates is null) return;
		if (! candidates.Any()) return;

		var edop = new EditOperation { Name = Caption };
		int featureCount = 0;

		foreach (var candidate in candidates.Where(c => c.Overrides.Any()))
		{
			// The "distinct" is necessary as more than one override may use the same field!
			var attributes = candidate.Overrides
			                          .DistinctBy(o => o.FieldName)
			                          .ToDictionary(o => o.FieldName,
			                                        o => (object) NormalizeFieldValue(
				                                        o.FieldName,
				                                        GetFieldValue(angle, o.CurrentValue)));
			edop.Modify(candidates.Layer, candidate.OID, attributes);
			featureCount += 1;
		}
			
		if (edop.IsEmpty)
		{
			_msg.Debug("No symbol rotated (no feature had suitable overrides)");
			return;
		}

		_msg.Debug($"{Caption}: starting edit operation (Task ID {Task.CurrentId})");

		var ok = edop.Execute();

		_msg.Debug($"Edit Operation: ok={ok}, done={edop.IsDone}, canceled={edop.IsCanceled}, succeeded={edop.IsSucceeded}");

		if (edop.IsSucceeded)
		{
			string info = angle is null
				              ? "reset override"
				              : RotationKind == SymbolRotationKind.Additive
					              ? $"add {angle:F1}°"
					              : $"set {angle:F1}°";
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

	private void DrawPreviewOverlay(Candidates candidates, double angle)
	{
		if (candidates is null) return;

		foreach (var candidate in candidates)
		{
			var symref = candidate.Symbol.Clone();

			// This symbol has overrides already applied; but of course,
			// we still write our rotational override(s) for this preview
			// (and because of this modification we made a copy/clone above):

			if (! double.IsNaN(angle))
			{
				foreach (var po in candidate.Overrides)
				{
					var value = GetFieldValue(angle, po.CurrentValue);
					value = NormalizeFieldValue(po.FieldName, value);
					SymbolUtils.ApplyOverride(symref.Symbol, po.PrimitiveName, po.PropertyName, value);
				}
			}

			FeaturePreview.Draw(candidates.Layer, candidate.OID, symref, candidate.Shape);
		}
	}
}
