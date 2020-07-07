using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;

namespace ProSuite.Commons.UI.Progress
{
	public class WinFormsProgressFeedback : IProgressFeedback
	{
		private readonly ToolStripProgressBar _progressBar;
		private readonly ToolStripStatusLabel _statusLabel;
		private readonly Cursor _oldCursor;
		private readonly bool _oldProgressBarVisible;
		private readonly bool _oldStatusLabelVisible;
		private readonly IMsg _logger;

		public WinFormsProgressFeedback([NotNull] ToolStripProgressBar progressBar,
		                                [NotNull] ToolStripStatusLabel statusLabel,
		                                bool showWaitCursor = false,
		                                [CanBeNull] IMsg logger = null)
		{
			Assert.ArgumentNotNull(progressBar, nameof(progressBar));
			Assert.ArgumentNotNull(statusLabel, nameof(statusLabel));

			_progressBar = progressBar;
			_statusLabel = statusLabel;
			_logger = logger;

			if (showWaitCursor)
			{
				_oldCursor = Cursor.Current;
				// Cursor.Current is the app level cursor
				Cursor.Current = Cursors.WaitCursor;
			}
			else
			{
				_oldCursor = null;
			}

			_oldProgressBarVisible = _progressBar.Visible;
			_progressBar.Visible = true;

			_oldStatusLabelVisible = _statusLabel.Visible;
			_statusLabel.Visible = true;
		}

		#region IProgressFeedback

		public void SetRange(int minimumValue, int maximumValue)
		{
			Assert.ArgumentCondition(minimumValue <= maximumValue,
			                         "minimumValue > maximumValue");

			_progressBar.Step = 1;
			_progressBar.Minimum = minimumValue;
			_progressBar.Maximum = maximumValue;

			EnsureValueInRange();

			RefreshAppearance();
		}

		public void Advance()
		{
			_progressBar.PerformStep();

			RefreshAppearance();
		}

		public void Advance(string message)
		{
			_progressBar.PerformStep();

			SetMessage(message);

			RefreshAppearance();
		}

		public void Advance(string format, params object[] args)
		{
			// ReSharper disable once RedundantStringFormatCall
			Advance(string.Format(format, args));
		}

		public void SetComplete()
		{
			SetComplete(string.Empty);
		}

		public void SetComplete(string message)
		{
			_progressBar.Value = _progressBar.Maximum;

			SetMessage(message);

			RefreshAppearance();
		}

		public void SetComplete(string format, params object[] args)
		{
			// ReSharper disable once RedundantStringFormatCall
			SetComplete(string.Format(format, args));
		}

		public void ShowMessage(string message)
		{
			SetMessage(message);
			RefreshAppearance();
		}

		public void ShowMessage(string format, params object[] args)
		{
			// ReSharper disable once RedundantStringFormatCall
			ShowMessage(string.Format(format, args));
		}

		public void HideMessage()
		{
			ShowMessage(string.Empty);
		}

		public int CurrentValue
		{
			get { return _progressBar.Value; }
			set
			{
				_progressBar.Value = value;

				EnsureValueInRange();
				RefreshAppearance();
			}
		}

		public int MinimumValue
		{
			get { return _progressBar.Minimum; }
			set
			{
				if (_progressBar.Maximum < value)
				{
					_progressBar.Maximum = value;
				}

				_progressBar.Minimum = value;

				EnsureValueInRange();
				RefreshAppearance();
			}
		}

		public int MaximumValue
		{
			get { return _progressBar.Maximum; }
			set
			{
				if (_progressBar.Minimum > value)
				{
					_progressBar.Minimum = value;
				}

				_progressBar.Maximum = value;

				EnsureValueInRange();
				RefreshAppearance();
			}
		}

		public int StepSize
		{
			get { return _progressBar.Step; }
			set { _progressBar.Step = value; }
		}

		public void Dispose()
		{
			// Restore label's visibility but leave its Text unchanged.
			_statusLabel.Visible = _oldStatusLabelVisible;

			_progressBar.Value = _progressBar.Minimum;
			_progressBar.Visible = _oldProgressBarVisible;

			if (_oldCursor != null)
			{
				Cursor.Current = _oldCursor;
			}
		}

		#endregion

		#region Non-public methods

		private static void RefreshAppearance()
		{
			Application.DoEvents();
		}

		private void SetMessage(string message)
		{
			if (message == null)
			{
				return; // no-op if null message
			}

			_statusLabel.Text = message;

			if (_logger != null && ! string.IsNullOrEmpty(message))
			{
				_logger.Info(message);
			}
		}

		private void EnsureValueInRange()
		{
			if (_progressBar.Value < _progressBar.Minimum)
			{
				_progressBar.Value = _progressBar.Minimum;
			}

			if (_progressBar.Value > _progressBar.Maximum)
			{
				_progressBar.Value = _progressBar.Maximum;
			}
		}

		#endregion
	}
}
