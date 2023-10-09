using System;
using System.Threading;
using ESRI.ArcGIS.esriSystem;

namespace ProSuite.Microservices.Server.AO
{
	public class TrackCancellationToken : ITrackCancel
	{
		private readonly CancellationToken _cancellationToken;
		private bool _cancel;

		private readonly int _defaultCheckInterval = 1000;

		private DateTime lastCheck;

		public TrackCancellationToken(CancellationToken cancellationToken)
		{
			_cancellationToken = cancellationToken;
			CheckTime = _defaultCheckInterval;
		}

		public void Cancel()
		{
			_cancel = true;
		}

		public void Reset()
		{
			_cancel = false;
		}

		public bool Continue()
		{
			if (_cancel)
			{
				return false;
			}

			if ((DateTime.Now - lastCheck).TotalMilliseconds < CheckTime)
			{
				return true;
			}

			lastCheck = DateTime.Now;

			if (_cancellationToken.IsCancellationRequested)
			{
				_cancel = true;
			}

			return ! _cancel;
		}

		public void StartTimer(int hWnd, int Milliseconds) { }

		public void StopTimer() { }

		public IProgressor Progressor { get; set; }

		public int CheckTime { get; set; }

		public bool ProcessMessages { get; set; }

		public bool TimerFired { get; }

		public bool CancelOnClick { get; set; }

		public bool CancelOnKeyPress { get; set; }

		public Exception Exception { get; set; }
	}
}
