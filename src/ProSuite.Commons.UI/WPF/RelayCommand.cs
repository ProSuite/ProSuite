using System;
using System.Reflection;
using System.Windows.Input;
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
				return _canExecute?.Invoke((T) parameter) ?? true;
			}
			catch (Exception e)
			{
				_msg.Debug("Error in relay command's CanExecute method", e);

				throw;
			}
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
