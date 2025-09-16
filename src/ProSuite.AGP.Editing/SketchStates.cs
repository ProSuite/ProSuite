using System;
using System.Collections.Generic;
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
public class SketchStates
{
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
	public bool IsLatched => _latch.IsLatched;

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

		// Clear the stack if only one sketch remains
		if (_sketches.Count == 1)
		{
			_sketches.Clear();
			_msg.VerboseDebug(() => "Cleared sketches after pop");
		}

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

		TryPop();
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
	public async Task ReplaySketchesAsync([NotNull] MapView mapView)
	{
		if (mapView == null)
		{
			throw new ArgumentNullException(nameof(mapView));
		}

		_msg.VerboseDebug(
			() => $"Replay: {_sketches.Count} sketches to map view: {mapView.Map?.Name}");

		try
		{
			foreach (Geometry sketch in _sketches.Reverse())
			{
				_latch.Increment();
				await mapView.SetCurrentSketchAsync(sketch);
			}
		}
		catch (Exception ex)
		{
			_msg.Error($"Error during sketch replay: {ex.Message}", ex);
			throw;
		}
	}

	/// <summary>
	/// Replays all stored sketch states to the active map view.
	/// </summary>
	/// <returns>A task representing the asynchronous operation</returns>
	/// <exception cref="InvalidOperationException">Thrown when there is no active map view</exception>
	public async Task ReplaySketchesAsync()
	{
		var activeMapView = MapView.Active;
		if (activeMapView == null)
		{
			throw new InvalidOperationException("No active map view available for sketch replay");
		}

		await ReplaySketchesAsync(activeMapView);
	}

	/// <summary>
	/// Increments the latch counter. This should be called when starting a sketch replay
	/// operation to prevent the replayed operations from being recorded again.
	/// </summary>
	public void IncrementLatch()
	{
		_latch.Increment();
	}

	/// <summary>
	/// Decrements the latch counter. This should be called when a replayed sketch operation
	/// is processed to maintain synchronization with the operation stack.
	/// </summary>
	public void DecrementLatch()
	{
		_latch.Decrement();
	}

	/// <summary>
	/// Resets the latch counter to zero.
	/// </summary>
	public void ResetLatch()
	{
		_latch.Reset();
	}

	/// <summary>
	/// Gets the current latch count, useful for debugging and synchronization verification.
	/// </summary>
	public int LatchCount => _latch.Count;

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
