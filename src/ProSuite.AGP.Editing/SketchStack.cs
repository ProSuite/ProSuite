using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Manages a stack of sketch geometries and provides functionality for adding, clearing,
/// and replaying sketch operations. This class focuses on state management without
/// event handling or drawing functionality.
/// </summary>
public class SketchStack
{
	// TODO: Add a redo stack so we can track both undo and redo operations of sketch operations
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly Stack<Geometry> _sketches = new();

	private const int MaxSketchCount = 10;

	/// <summary>
	/// Gets the number of sketch states currently stored.
	/// </summary>
	public int Count => _sketches.Count;

	/// <summary>
	/// Gets a value indicating whether there are any sketch states stored.
	/// </summary>
	public bool HasSketches => _sketches.Count > 0;

	/// <summary>
	/// Gets a value indicating whether the replay latch is currently active.
	/// This is used to prevent recording operations during replay.
	/// </summary>
	public bool IsReplayingSketches { get; private set; }

	/// <summary>
	/// Adds a sketch geometry to the state stack if it's not null or empty and no identical
	/// sketch is already on top of the stack.
	/// </summary>
	/// <param name="sketch">The sketch geometry to add</param>
	public bool TryPush(Geometry sketch)
	{
		if (sketch == null || sketch.IsEmpty)
		{
			return false;
		}

		if (_sketches.TryPeek(out Geometry last))
		{
			if (last.IsEqual(sketch))
			{
				return false;
			}
		}

		// NOTE: While the OnSketchModifiedAsync call back on the tools are called on the UI
		// thread and also get called again once the Z value comes in,
		// this event handler is called on the background and typically only once per vertex
		// -> If we want to avoid NaN Zs in the sketch stack (which we want) there is no good
		//    solution relying on just the SketchModifiedEvent. We could just leave out the
		//    trailing vertices with Nan Z...
		//    Long term plan:
		//    A. Make it fast (by providing a local raster surface)
		//    B. Provide an event fired by the tool (if our tool is active)
		//    that provides extra meta-information whether the sketch is 'stack-ready' or if there
		//    are still nan Zs.
		// TEST
		Multipart polycurve = sketch as Multipart;

		if (polycurve?.PointCount > 0)
		{
			MapPoint lastPoint = polycurve.Points[polycurve.PointCount - 1];

			_msg.VerboseDebug(() =>
				                  $"Pushing polycurve with {polycurve.PointCount} points onto stack. " +
				                  $"Last point: {lastPoint.X}|{lastPoint.Y}|{lastPoint.Z}");

			bool noNanOnStack =
				! EnvironmentUtils.GetBooleanEnvironmentVariableValue(
					"PROSUITE_ALLOW_NANZ_ON_STACK");

			if (double.IsNaN(lastPoint.Z) && noNanOnStack)
			{
				_msg.Warn(
					"Sketch point could be missing in sketch (Repressed from stack due to NaN-Z). " +
					"MAKE SURE TO CHECK sketch when finishing sketch after switching to stereo");
				return false;
			}
		}

		// END TEST

		_sketches.Push(sketch);
		_msg.VerboseDebug(() => $"Pushed sketch onto stack. Count: {_sketches.Count} sketches");

		return true;
	}

	/// <summary>
	/// Removes and returns the most recent sketch geometry from the state stack.
	/// </summary>
	/// <returns>The most recent sketch geometry, or null if the stack is empty</returns>
	public Geometry TryPop()
	{
		if (_sketches.Count == 0)
		{
			return null;
		}

		var sketch = _sketches.Pop();
		_msg.VerboseDebug(() => $"Pop: {_sketches.Count} sketches remaining");

		return sketch;
	}

	/// <summary>
	/// Handles an undo operation by removing the most recent sketch from the stack.
	/// </summary>
	/// <returns>True if a sketch was removed, false if the stack was empty</returns>
	public bool HandleUndo()
	{
		if (_sketches.Count == 0)
		{
			return false;
		}

		_sketches.Pop();
		_msg.VerboseDebug(() => $"HandleUndo pop: {_sketches.Count} sketches remaining");

		// TODO: Understand and explain why this is necessary (it is!). Probably some previous sketch remains?
		if (_sketches.Count == 1)
		{
			_sketches.Clear();
			_msg.VerboseDebug(() => "Cleared sketches after undo (original logic)");
		}

		return true;
	}

	/// <summary>
	/// Clears all sketch states and resets the latch.
	/// </summary>
	public void Clear()
	{
		_sketches.Clear();
		IsReplayingSketches = false;
		_msg.VerboseDebug(() => "Sketch states cleared");
	}

	/// <summary>
	/// Replays all stored sketch states to the specified map view.
	/// The sketches are applied in reverse order (oldest first) to rebuild the operation stack correctly.
	/// </summary>
	/// <param name="mapView">The map view to apply the sketches to</param>
	/// <returns>A task representing the asynchronous operation</returns>
	public async Task<bool> ReplaySketchesAsync([NotNull] MapView mapView)
	{
		if (mapView == null)
		{
			throw new ArgumentNullException(nameof(mapView));
		}

		_msg.VerboseDebug(() =>
			                  $"Replay: {_sketches.Count} sketches in stack. Applying to {mapView.Map?.Name}...");

		if (_sketches.Count == 0)
		{
			_msg.VerboseDebug(() => "No sketches to replay");
			return false;
		}

		try
		{
			Stopwatch watch = Stopwatch.StartNew();

			// Only replay the most recent MaxSketchCount sketches, oldest-first (for performance reasons).
			// Stack<T> enumerates top-to-bottom, so Take() yields the most recent entries.
			List<Geometry> sketchesToReplay =
				_sketches.Take(MaxSketchCount).Reverse().ToList();

			IsReplayingSketches = true;

			foreach (Geometry sketch in sketchesToReplay)
			{
				await mapView.SetCurrentSketchAsync(sketch);
			}

			IsReplayingSketches = false;

			// Trim the stack. Subsequent undo operations and future replays remain consistent.
			// sketchesToReplay is already oldest-first, so re-pushing it rebuilds the stack.
			if (_sketches.Count > MaxSketchCount)
			{
				_sketches.Clear();
				foreach (Geometry sketch in sketchesToReplay)
				{
					_sketches.Push(sketch);
				}

				_msg.VerboseDebug(() => $"Trimmed sketch stack to {_sketches.Count} entries");
			}

			_msg.VerboseDebug(() =>
				                  $"Re-applied {_sketches.Count} sketch states to map view {mapView.Map?.Name} in " +
				                  $"{watch.ElapsedMilliseconds}ms.");

			return true;
		}
		catch (Exception ex)
		{
			_msg.Error($"Error during sketch replay: {ex.Message}", ex);

			// Reset flag to maintain consistency since replay failed
			IsReplayingSketches = false;

			throw;
		}
	}

	/// <summary>
	/// Replays all stored sketch states to the active map view.
	/// </summary>
	/// <returns>A task representing the asynchronous operation</returns>
	/// <exception cref="InvalidOperationException">Thrown when there is no active map view</exception>
	public async Task<bool> ReplaySketchesAsync()
	{
		var activeMapView = MapView.Active;
		if (activeMapView == null)
		{
			throw new InvalidOperationException("No active map view available for sketch replay");
		}

		return await ReplaySketchesAsync(activeMapView);
	}

	/// <summary>
	/// Processes a sketch modification event, handling replay synchronization automatically.
	/// Returns true if the operation should be recorded, false if it should be ignored.
	/// </summary>
	/// <param name="isUndo">Whether this is an undo operation</param>
	/// <param name="currentSketch">The current sketch geometry</param>
	/// <returns>True if the operation should be recorded as a new state</returns>
	public bool ProcessSketchModification(bool isUndo, Geometry currentSketch)
	{
		if (IsReplayingSketches)
		{
			return false; // Don't record - this is a replay operation
		}

		if (isUndo && HasSketches)
		{
			HandleUndo();
			return false; // Don't record - undo was handled internally
		}

		// Record this as a new sketch state
		return TryPush(currentSketch);
	}

	/// <summary>
	/// Gets the most recent sketch geometry without removing it from the stack.
	/// </summary>
	/// <returns>The most recent sketch geometry, or null if the stack is empty</returns>
	public Geometry PeekMostRecent()
	{
		return _sketches.Count > 0 ? _sketches.Peek() : null;
	}
}
