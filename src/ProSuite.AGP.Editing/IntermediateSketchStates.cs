using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ArcGIS.Core.Events;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing;

/// <summary>
/// Records and maintains the history of the intermediate states of an edit sketch during its
/// creation. The sketching can be suspended while the current sketch is displayed as an overlay.
/// After the sketch is restored, this history allows correct undo/redo stack for all the previous
/// states of this edit sketch.
/// </summary>
public class IntermediateSketchStates
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly Stack<Geometry> _sketches = new();
	private readonly SketchLatch _latch = new();
	[NotNull] private readonly SketchDrawer _sketchDrawer = new SketchDrawer();
	private bool _active;
	[CanBeNull] private SubscriptionToken _onSketchModifiedToken;
	[CanBeNull] private SubscriptionToken _onSketchCompletedToken;

	public bool IsInIntermittentSelectionPhase { get; private set; }

	public async Task ActivateAsync()
	{
		Assert.False(_active, "Already recording");

		WireEvents();

		_active = true;

		Geometry sketch = await MapView.Active.GetCurrentSketchAsync();

		TryPush(sketch);
	}

	/// <summary>
	/// Draws the current sketch as an overlay and enters the 'intermittent selection phase'.
	/// </summary>
	/// <returns></returns>
	public async Task StartIntermittentSelection()
	{
		Assert.False(IsInIntermittentSelectionPhase, "Already suspended");
		Assert.True(_active, "Not recording");
		IsInIntermittentSelectionPhase = true;

		var mapView = MapView.Active;
		if (mapView is null)
		{
			return;
		}

		Geometry sketch = await mapView.GetCurrentSketchAsync();

		await _sketchDrawer.ShowSketch(sketch, mapView);
	}

	/// <summary>
	/// Stops the intermittent selection phase, restores the last sketch state and clears the overlays.
	/// </summary>
	/// <returns></returns>
	public async Task StopIntermittentSelectionAsync()
	{
		try
		{
			if (! IsInIntermittentSelectionPhase)
			{
				// This happens for example when the map is changed while pressing shift
				return;
			}

			IsInIntermittentSelectionPhase = false;

			_msg.VerboseDebug(() => $"Replay: {_sketches.Count} sketches");

			foreach (Geometry sketch in _sketches.Reverse())
			{
				_latch.Increment();
				await MapView.Active.SetCurrentSketchAsync(sketch);
			}
		}
		catch (Exception e)
		{
			_msg.Error($"Error setting current sketch: {e.Message}", e);
		}

		_sketchDrawer.ClearSketch();
	}

	/// <summary>
	/// Resets the recorded sketch states and aborts the intermittent selection phase, in case ist is active.
	/// </summary>
	public void ResetSketchStates()
	{
		_sketches.Clear();
		_latch.Reset();

		IsInIntermittentSelectionPhase = false;

		_sketchDrawer.ClearSketch();
	}

	/// <summary>
	/// Deactivates the sketch state history entirely.
	/// </summary>
	public void Deactivate()
	{
		UnwireEvents();

		_active = false;

		ResetSketchStates();
	}

	private void TryPush(Geometry sketch, [CallerMemberName] string caller = null)
	{
		if (sketch is not { IsEmpty: false })
		{
			return;
		}

		_sketches.Push(sketch);
		_msg.VerboseDebug(() => $"{caller}: {_sketches.Count} sketches");
	}

	private void OnSketchModified(SketchModifiedEventArgs args)
	{
		if (IsInIntermittentSelectionPhase)
		{
			// Do not record sketch states for the selection sketch!
			return;
		}

		try
		{
			_msg.VerboseDebug(() => $"{args.SketchOperationType}");

			Assert.True(_active, "not recording");

			if (_latch.IsLatched)
			{
				_latch.Decrement();
				Assert.True(_latch.Count >= 0, "Sketch stack isn't in sync with latch");
				return;
			}

			if (args.IsUndo && _sketches.Count != 0)
			{
				Assert.NotNull(_sketches.Pop());
				_msg.VerboseDebug(
					() => $"{nameof(OnSketchModified)} pop: {_sketches.Count} sketches");

				if (_sketches.Count == 1)
				{
					_sketches.Clear();
					_msg.VerboseDebug(() => "clear sketches");
				}

				return;
			}

			TryPush(args.CurrentSketch);
		}
		catch (Exception e)
		{
			_msg.Error($"Error in {nameof(OnSketchModified)}: {e.Message}", e);
		}
	}

	private void OnSketchCompleted(SketchCompletedEventArgs args)
	{
		Assert.True(_active, "not recording");

		if (IsInIntermittentSelectionPhase)
		{
			return;
		}

		_msg.VerboseDebug(() => $"{nameof(OnSketchCompleted)}: {_sketches.Count} sketches");

		_sketches.Clear();
		_latch.Reset();
	}

	private void WireEvents()
	{
		_onSketchModifiedToken = SketchModifiedEvent.Subscribe(OnSketchModified);
		_onSketchCompletedToken = SketchCompletedEvent.Subscribe(OnSketchCompleted);
	}

	private void UnwireEvents()
	{
		if (_onSketchModifiedToken != null)
		{
			SketchModifiedEvent.Unsubscribe(_onSketchModifiedToken);
		}

		if (_onSketchCompletedToken != null)
		{
			SketchCompletedEvent.Unsubscribe(_onSketchCompletedToken);
		}

		_onSketchModifiedToken = null;
		_onSketchCompletedToken = null;
	}

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
