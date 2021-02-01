using System;

namespace ProSuite.Commons.Progress
{
	public abstract class ProgressFeedbackBase : IProgressFeedback
	{
		private int _minimumValue;
		private int _maximumValue;
		private int _currentValue;
		private int _stepSize = 1;

		protected abstract void SetText(string text);

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
			Advance();
			SetText(message);
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
			SetText(message);
		}

		public void SetComplete(string format, params object[] args)
		{
			SetComplete(string.Format(format, args));
		}

		public void ShowMessage(string message)
		{
			SetText(message);
		}

		public void ShowMessage(string format, params object[] args)
		{
			SetText(string.Format(format, args));
		}

		public void HideMessage()
		{
			SetText(null);
		}

		public int CurrentValue
		{
			get => _currentValue;
			set
			{
				_currentValue = value;

				EnsureValueInRange();
			}
		}

		public int MinimumValue
		{
			get => _minimumValue;
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
			get => _maximumValue;
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
			get => _stepSize;
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

		protected void EnsureValueInRange()
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

		#endregion
	}
}
