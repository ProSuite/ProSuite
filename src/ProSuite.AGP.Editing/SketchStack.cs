using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
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
	private readonly SketchLatch _latch = new();

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
	public bool IsReplayingSketches => _latch.IsLatched;

	/// <summary>
	/// Adds a sketch geometry to the state stack if it's not null or empty.
	/// </summary>
	/// <param name="sketch">The sketch geometry to add</param>
	public void TryPush(Geometry sketch)
	{
		if (sketch is not { IsEmpty: false })
		{
			return;
		}

		_sketches.Push(sketch);
		_msg.VerboseDebug(() => $"Pushed sketch onto stack. Count: {_sketches.Count} sketches");
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
		_latch.Reset();
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
			                  $"Replay: {_sketches.Count} sketches to map view: {mapView.Map?.Name}");

		if (_sketches.Count == 0)
		{
			_msg.VerboseDebug(() => "No sketches to replay");
			return false;
		}

		int latchIncrements = 0;
		try
		{
			Stopwatch watch = Stopwatch.StartNew();

			foreach (Geometry sketch in _sketches.Reverse())
			{
				_latch.Increment();
				latchIncrements++;
				await mapView.SetCurrentSketchAsync(sketch);
			}

			// Reset the latch after successful replay
			// Theoretically it should be 0 already, but the SketchModified events do not fire strictly
			// once per sketch change (e.g. due to fast changes, some events could be dropped).
			_latch.Reset();

			_msg.VerboseDebug(
				() => $"Re-applied sketch states to map view {mapView.Map?.Name} in " +
				      $"{watch.ElapsedMilliseconds}ms.");

			return true;
		}
		catch (Exception ex)
		{
			_msg.Error(
				$"Error during sketch replay after {latchIncrements} operations: {ex.Message}", ex);

			// Reset latch to maintain consistency since replay failed
			_latch.Reset();

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
		if (_latch.IsLatched)
		{
			_latch.Decrement();
			return false; // Don't record - this is a replay operation
		}

		if (isUndo && HasSketches)
		{
			HandleUndo();
			return false; // Don't record - undo was handled internally
		}

		// Record this as a new sketch state
		TryPush(currentSketch);
		return true; // Operation was recorded
	}

	/// <summary>
	/// Gets the most recent sketch geometry without removing it from the stack.
	/// </summary>
	/// <returns>The most recent sketch geometry, or null if the stack is empty</returns>
	public Geometry PeekMostRecent()
	{
		return _sketches.Count > 0 ? _sketches.Peek() : null;
	}

	/// <summary>
	/// Internal class for managing the sketch replay latch mechanism.
	/// This prevents replayed sketch operations from being recorded again.
	/// </summary>
	private class SketchLatch
	{
		public int Count { get; private set; }

		public bool IsLatched => Count > 0;

		public void Increment()
		{
			Count++;
		}

		public void Decrement()
		{
			Count--;
		}

		public void Reset()
		{
			Count = 0;
		}
	}
}
