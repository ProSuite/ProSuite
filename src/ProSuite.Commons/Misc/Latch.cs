using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Misc
{
	/// <summary>
	/// Helper for controlling message cycles (e.g. in the interaction between view and controller/presenter)
	/// From http://codebetter.com/blogs/jeremy.miller/archive/2007/07/02/build-your-own-cab-12-rein-in-runaway-events-with-the-quot-latch-quot.aspx
	/// </summary>
	public class Latch
	{
		private int _count;

		public bool IsLatched => _count > 0;

		public void Increment()
		{
			_count++;
		}

		public void Decrement()
		{
			_count--;
		}

		/// <summary>
		/// Runs the specified procedure inside a latch.
		/// </summary>
		/// <param name="procedure">The handler.</param>
		public void RunInsideLatch([NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			Increment();
			try
			{
				procedure();
			}
			finally
			{
				Decrement();
			}
		}

		/// <summary>
		/// Runs the specified function inside a latch.
		/// </summary>
		/// <param name="function">The handler.</param>
		public T RunInsideLatch<T>([NotNull] Func<T> function)
		{
			Assert.ArgumentNotNull(function, nameof(function));

			Increment();
			try
			{
				return function();
			}
			finally
			{
				Decrement();
			}
		}

		/// <summary>
		/// Runs the specified procedure only if no latch is set.
		/// </summary>
		/// <param name="procedure">The procedure.</param>
		public void RunLatchedOperation([NotNull] Action procedure)
		{
			Assert.ArgumentNotNull(procedure, nameof(procedure));

			if (IsLatched)
			{
				return;
			}

			procedure();
		}
	}
}
