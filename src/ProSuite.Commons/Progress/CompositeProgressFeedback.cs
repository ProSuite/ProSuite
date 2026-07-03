using System;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Progress
{
	/// <summary>
	/// Fans out every <see cref="IProgressFeedback"/> call to a fixed set of child feedbacks
	/// (e.g. a visible progress window plus an always-on logging sink), in registration order.
	/// Implements <see cref="IProgressFeedback"/> directly rather than deriving from
	/// <see cref="ProgressFeedbackBase"/>, since each child needs its own state (progress bar
	/// range, logging prefix counters, etc.) kept independently in sync — something the base
	/// class's single-<c>SetText</c>-hook model cannot provide.
	/// </summary>
	/// <remarks>
	/// A failing child is caught, logged, and skipped: it can never abort the underlying
	/// checkout/checkin/discard operation, and it never prevents the remaining children (e.g.
	/// the logging sink) from still being called — including during <see cref="Dispose"/>.
	/// </remarks>
	public class CompositeProgressFeedback : IProgressFeedback
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly IProgressFeedback[] _children;

		private int _minimumValue;
		private int _maximumValue;
		private int _currentValue;
		private int _stepSize = 1;

		public CompositeProgressFeedback(params IProgressFeedback[] children)
		{
			_children = children ?? Array.Empty<IProgressFeedback>();
		}

		#region IProgressFeedback

		public void SetRange(int minimumValue, int maximumValue)
		{
			ForEachChild(child => child.SetRange(minimumValue, maximumValue));
		}

		public void Advance()
		{
			ForEachChild(child => child.Advance());
		}

		public void Advance(string message)
		{
			ForEachChild(child => child.Advance(message));
		}

		public void Advance(string format, params object[] args)
		{
			Advance(string.Format(format, args));
		}

		public void SetComplete()
		{
			ForEachChild(child => child.SetComplete());
		}

		public void SetComplete(string message)
		{
			ForEachChild(child => child.SetComplete(message));
		}

		public void SetComplete(string format, params object[] args)
		{
			SetComplete(string.Format(format, args));
		}

		public void ShowMessage(string message, LogLevel level = LogLevel.Info)
		{
			ForEachChild(child => child.ShowMessage(message, level));
		}

		public void ShowMessage(string format, params object[] args)
		{
			ShowMessage(string.Format(format, args));
		}

		public void HideMessage()
		{
			ForEachChild(child => child.HideMessage());
		}

		public int CurrentValue
		{
			get => _currentValue;
			set
			{
				_currentValue = value;
				ForEachChild(child => child.CurrentValue = value);
			}
		}

		public int MinimumValue
		{
			get => _minimumValue;
			set
			{
				_minimumValue = value;
				ForEachChild(child => child.MinimumValue = value);
			}
		}

		public int MaximumValue
		{
			get => _maximumValue;
			set
			{
				_maximumValue = value;
				ForEachChild(child => child.MaximumValue = value);
			}
		}

		public int StepSize
		{
			get => _stepSize;
			set
			{
				_stepSize = value;
				ForEachChild(child => child.StepSize = value);
			}
		}

		public void Dispose()
		{
			ForEachChild(child => child.Dispose());
		}

		#endregion

		#region Non-public methods

		private void ForEachChild(Action<IProgressFeedback> action)
		{
			foreach (IProgressFeedback child in _children)
			{
				try
				{
					action(child);
				}
				catch (Exception ex)
				{
					_msg.Debug(
						$"Progress feedback child {child.GetType().Name} threw and will be " +
						"skipped; the operation continues.", ex);
				}
			}
		}

		#endregion
	}
}
