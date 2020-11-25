using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class ProgressWatch
	{
		private readonly Action<ProgressArgs> _onProgress;
		private readonly Stopwatch _stopWatch;
		private readonly LinkedList<Transaction> _nestedProgresses;

		public ProgressWatch([NotNull] Action<ProgressArgs> onProgress)
		{
			_onProgress = onProgress;
			_stopWatch = new Stopwatch();
			_stopWatch.Start();

			_nestedProgresses = new LinkedList<Transaction>();
		}

		/// <summary>
		/// Recommended usage: using(ProgressWatch.MakeTransaction(...)) {...}
		/// </summary>
		/// <param name="startStep"></param>
		/// <param name="endStep"></param>
		/// <param name="current"></param>
		/// <param name="total"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		public IDisposable MakeTransaction(
			Step startStep, Step endStep, int current, int total, object tag)
		{
			return new Transaction(_onProgress, _stopWatch, _nestedProgresses,
			                       startStep, endStep, current, total, tag);
		}

		private class Transaction : IDisposable
		{
			private readonly Action<ProgressArgs> _progress;
			private readonly Step _endStep;
			private readonly int _current;
			private readonly int _total;
			private readonly object _tag;

			private readonly Stopwatch _stopWatch;
			private readonly LinkedList<Transaction> _progresses;

			private readonly long _t0;

			public Transaction([NotNull] Action<ProgressArgs> progress,
			                   [NotNull] Stopwatch stopWatch,
			                   [NotNull] LinkedList<Transaction> progresses,
			                   Step startStep, Step endStep,
			                   int current, int total, object tag)
			{
				_progress = progress;
				_stopWatch = stopWatch;
				_progresses = progresses;

				_endStep = endStep;
				_current = current;
				_total = total;
				_tag = tag;

				ProgressArgs args = new ProgressArgs(startStep, current, total, tag);
				progress(args);

				_t0 = _stopWatch.ElapsedTicks;
				_progresses.AddLast(this);
			}

			private long NestedProgressTicks { get; set; }

			void IDisposable.Dispose()
			{
				_progresses.RemoveLast();

				long t1 = _stopWatch.ElapsedTicks;
				long dt = t1 - _t0;

				if (_progresses.Count > 0)
				{
					_progresses.Last.Value.NestedProgressTicks += dt;
				}

				var args = new ProgressArgs(_endStep, _current, _total, _tag);
				args.BruttoTicks = dt;
				args.NettoTicks = dt - NestedProgressTicks;
				_progress(args);
			}
		}
	}
}
