using System;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.UI.WPF
{
	public class RelayCommand<T> : ICommand
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly Action<T> _execute;
		private readonly Predicate<T> _canExecute;
		private bool _previousCanExecute;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of <see cref="RelayCommand{T}"/>.
		/// </summary>
		/// <param name="execute">The execution logic.</param>
		/// <param name="canExecute">The execution status logic. If null, <seealso cref="CanExecute"/>
		/// will always return true.</param>
		public RelayCommand([NotNull] Action<T> execute,
		                    [CanBeNull] Predicate<T> canExecute = null)
		{
			Assert.NotNull(execute, nameof(execute));

			_execute = execute;
			_canExecute = canExecute;
		}

		#endregion

		#region ICommand Members

		///<summary>
		///Defines the method that determines whether the command can execute in its current state.
		///</summary>
		///<param name="parameter">Data used by the command.  If the command does not require data
		/// to be passed, this object can be set to null.</param>
		///<returns>
		///true if this command can be executed; otherwise, false.
		///</returns>
		public bool CanExecute(object parameter)
		{
			try
			{
				bool result = _canExecute?.Invoke((T) parameter) ?? true;

				if (result != _previousCanExecute)
				{
					_previousCanExecute = result;

					RaiseCanExecuteChanged(true);
				}

				return result;
			}
			catch (Exception e)
			{
				_msg.Debug("Error in relay command's CanExecute method", e);

				throw;
			}
		}

		/// <summary>
		/// In some situations the CommandManager does not figure out that it should re-query
		/// the can-execute method and the command remains disabled.
		/// This method will check if the value has changed and if so, fire the appropriate event.
		/// </summary>
		/// <param name="knownChanged"></param>
		/// <param name="parameter"></param>
		public void RaiseCanExecuteChanged(bool knownChanged = false,
		                                   object parameter = null)
		{
			if (_canExecute == null)
			{
				return;
			}

			if (! knownChanged)
			{
				// This will test the Can method an trigger this method with knownChanged == true.
				CanExecute(parameter);
			}

			// NOTE: When setting properties on objects that implement INotifyPropertyChanged
			// from a background thread, WPF automatically invokes the setter on the UI thread
			// which is necessary to avoid InvalidOperationExceptions ('The calling thread cannot
			// access this object because a different thread owns it').
			// However, for raising events this is not the case! The command and the event handler
			// were created on the UI thread and hence must be accessed on the UI thread:
			Application.Current.Dispatcher.BeginInvoke(
				new Action(delegate
				{
					// This invokes the CommandManager.RequerySuggested event which fires the
					// CanExecuteChanged event below.
					CommandManager.InvalidateRequerySuggested();
				}),
				DispatcherPriority.ApplicationIdle);
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public void Execute(object parameter)
		{
			try
			{
				_execute((T) parameter);
			}
			catch (Exception e)
			{
				// Throwing from here can crash the (WinForms host) application.
				// Just for diagnostic purposes, log the exception.
				_msg.Debug("Error in relay command's Execute method", e);

				throw;
			}
		}

		#endregion
	}
}
