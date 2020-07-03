using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Progress
{
	public class LoggingProgressFeedback : IProgressFeedback
	{
		private readonly IMsg _logger;

		private int _minimumValue;
		private int _maximumValue;
		private int _currentValue;
		private int _stepSize = 1;

		public LoggingProgressFeedback([NotNull] IMsg logger)
		{
			Assert.ArgumentNotNull(logger, nameof(logger));

			_logger = logger;
		}

		#region IProgressFeedback

		public void SetRange(int minimumValue, int maximumValue)
		{
			if (minimumValue > maximumValue)
			{
				throw new ArgumentException("minimumValue > maximumValue");
			}

			_minimumValue = minimumValue;
			_maximumValue = maximumValue;

			EnsureValueInRange();
		}

		public void Advance()
		{
			_currentValue += _stepSize;

			EnsureValueInRange();
		}

		public void Advance(string message)
		{
			_currentValue += _stepSize;

			EnsureValueInRange();

			SetMessage(message);
		}

		public void Advance(string format, params object[] args)
		{
			Advance(string.Format(format, args));
		}

		public void SetComplete()
		{
			_currentValue = _maximumValue;
		}

		public void SetComplete(string message)
		{
			_currentValue = _maximumValue;

			SetMessage(message);
		}

		public void SetComplete(string format, params object[] args)
		{
			SetComplete(string.Format(format, args));
		}

		public void ShowMessage(string message)
		{
			SetMessage(message);
		}

		public void ShowMessage(string format, params object[] args)
		{
			SetMessage(string.Format(format, args));
		}

		public void HideMessage()
		{
			SetMessage(string.Empty);
		}

		public int CurrentValue
		{
			get { return _currentValue; }
			set
			{
				_currentValue = value;

				EnsureValueInRange();
			}
		}

		public int MinimumValue
		{
			get { return _minimumValue; }
			set
			{
				_minimumValue = value;

				if (_maximumValue < value)
				{
					_maximumValue = value;
				}

				EnsureValueInRange();
			}
		}

		public int MaximumValue
		{
			get { return _maximumValue; }
			set
			{
				_maximumValue = value;

				if (_minimumValue > value)
				{
					_minimumValue = value;
				}

				EnsureValueInRange();
			}
		}

		public int StepSize
		{
			get { return _stepSize; }
			set
			{
				if (_stepSize <= 0)
				{
					throw new ArgumentException("value must be 1 or greater");
				}

				_stepSize = value;
			}
		}

		public void Dispose()
		{
			// Nothing to dispose of...
		}

		#endregion

		#region Non-public methods

		private void EnsureValueInRange()
		{
			if (_currentValue < _minimumValue)
			{
				_currentValue = _minimumValue;
			}

			if (_currentValue > _maximumValue)
			{
				_currentValue = _maximumValue;
			}
		}

		private void SetMessage(string message)
		{
			if (! string.IsNullOrEmpty(message))
			{
				_logger.Info(message);
			}
		}

		#endregion
	}
}