using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Essentials.Callbacks
{
	public class DisposableCallback<T> : IDisposable
	{
		[NotNull] private readonly Action<T> _action;
		private readonly T _val;

		/// <summary>
		/// Initializes a new instance of the <see cref="DisposableCallback{T}"/> class.
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="val">The val.</param>
		public DisposableCallback([NotNull] Action<T> action, T val)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			_action = action;
			_val = val;
		}

		public void Dispose()
		{
			_action(_val);
		}
	}

	public class DisposableCallback : IDisposable
	{
		[NotNull] private readonly Action _action;

		/// <summary>
		/// Initializes a new instance of the <see cref="DisposableCallback"/> class.
		/// </summary>
		/// <param name="action">The action.</param>
		public DisposableCallback([NotNull] Action action)
		{
			Assert.ArgumentNotNull(action, nameof(action));

			_action = action;
		}

		public void Dispose()
		{
			_action();
		}
	}
}
