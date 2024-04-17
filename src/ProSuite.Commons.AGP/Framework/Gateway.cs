using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Logging;
using MessageBox = ArcGIS.Desktop.Framework.Dialogs.MessageBox;

namespace ProSuite.Commons.AGP.Framework;

/// <summary>
/// Utils at the gateway between Pro SDK and our own code.
/// </summary>
public static class Gateway
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	/// <summary>
	/// Log entry into a method called directly from the Pro SDK framework,
	/// typically at the beginning of a Button's OnClick() method.
	/// </summary>
	/// <remarks> Do NOT call in performance sensitive areas!
	/// This method SHALL NOT throw exceptions.</remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void LogEntry(IMsg logger)
	{
		logger ??= _msg; // default to our own logger

		if (logger.IsVerboseDebugEnabled)
		{
			var callerName = GetCallerName();
			logger.VerboseDebug($"Enter {callerName}");
		}
	}

	/// <summary>
	/// Show a message box, running it (synchronously) on the proper thread.
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	public static MessageBoxResult ShowMessage(
		string message, string caption,
		MessageBoxButton button = MessageBoxButton.OK,
		MessageBoxImage icon = MessageBoxImage.None)
	{
		if (string.IsNullOrEmpty(message))
		{
			return MessageBoxResult.None;
		}

		try
		{
			return Application.Current.Dispatcher.Invoke(() =>
			{
				var owner = GetMainWindow();
				if (owner is null) return MessageBoxResult.None;
				return MessageBox.Show(owner, message, caption ?? string.Empty, button, icon);
			});
		}
		catch (Exception ex)
		{
			_msg.Error(ex.Message);
			return MessageBoxResult.None;
		}
	}

	/// <summary>
	/// Shows the Window <typeparamref name="TWindow"/> as a modal dialog,
	/// creating and running it (synchronously) on the proper thread.
	/// </summary>
	/// <returns>The DialogResult property before the dialog closes</returns>
	public static bool? ShowDialog<TWindow>(params object[] args) where TWindow : Window
	{
		args ??= Array.Empty<object>();

		try
		{
			var dispatcher = Application.Current.Dispatcher;

			return dispatcher.Invoke(() =>
			{
				var owner = GetMainWindow();
				if (owner is null) return null;
				var dialog = (Window) Activator.CreateInstance(typeof(TWindow), args, null);
				if (dialog is null) return null;
				dialog.Owner = owner;
				_msg.Debug($"Showing dialog: {dialog.Title}");
				var result = dialog.ShowDialog();
				return result;
			});
		}
		catch (Exception ex)
		{
			_msg.Error($"{nameof(ShowDialog)}: {ex.Message}", ex);
			return null;
		}
	}

	/// <summary>
	/// Report the given error (exception): (1) write it to
	/// the given logger and (2) show it in a modal message box.
	/// The <paramref name="caption"/> defaults to the caller's name.
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void HandleError(Exception ex, IMsg logger, string caption = null)
	{
		if (ex is null) return;

		if (caption is null)
		{
			string callerName = GetCallerName();
			caption = $"Error in {callerName}";
		}

		var message = FormatMessage(ex);

		logger ??= _msg; // default to our own logger
		logger.Error(message, ex);

		ShowMessage(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	/// <summary>
	/// Report the given error message: (1) write it to
	/// the given logger, and (2) show it in a modal message box.
	/// The <paramref name="caption"/> defaults to the caller's name.
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void HandleError(string message, IMsg logger, string caption = null)
	{
		if (message is null) return;

		if (caption is null)
		{
			string callerName = GetCallerName();
			caption = $"Error in {callerName}";
		}

		logger ??= _msg; // default to our own logger
		logger.Error(message);

		ShowMessage(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	/// <summary>
	/// Execute <pararef name="action"/> creating a single composite
	/// operation on the undo stack (or as few operations as possible,
	/// see the OperationManager.CreateCompositeOperation documentation
	/// in the Pro SDK).
	/// </summary>
	public static void CompositeOperation(OperationManager manager, string name, Action action)
	{
		if (action is null)
		{
			return; // no-op
		}

		if (manager is null)
		{
			action(); // non-composite
			return;
		}

		// If we have manager and action, a name is required:
		if (string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		Exception exception = null;
		manager.CreateCompositeOperation(() =>
		{
			// Note: action in CreateCompositeOperation MUST NOT fail or Undo Stack is broken FOREVER (empirical)
			try
			{
				action();
			}
			catch (Exception ex)
			{
				exception = ex;
				_msg.Error($"Operation {name} failed: {ex.Message}", ex);
			}
		}, name);

		if (exception != null)
		{
			throw exception;
		}
	}

	/// <summary>
	/// Execute <paramref name="func"/> creating a single composite
	/// operation on the undo stack (or as few operations as possible,
	/// see the OperationManager.CreateCompositeOperation documentation
	/// in the Pro SDK). Return the result of running <paramref name="func"/>
	/// </summary>
	public static T CompositeOperation<T>(OperationManager manager, string name, Func<T> func)
	{
		if (func is null)
		{
			return default; // no-op
		}

		if (manager is null)
		{
			return func(); // non-composite
		}

		// If we have manager and action, a name is required:
		if (string.IsNullOrEmpty(name))
			throw new ArgumentNullException(nameof(name));

		T result = default;
		Exception exception = null;
		manager.CreateCompositeOperation(() =>
		{
			// Note: action in CreateCompositeOperation MUST NOT fail or Undo Stack is broken FOREVER (empirical)
			try
			{
				result = func();
			}
			catch (Exception ex)
			{
				exception = ex;
				_msg.Error($"Operation {name} failed: {ex.Message}", ex);
			}
		}, name);

		if (exception != null)
		{
			throw exception;
		}

		return result;
	}

	private static Window GetMainWindow()
	{
		try
		{
			// Available only on thread that created the Application:
			return Application.Current.MainWindow;
		}
		catch
		{
			return null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static string GetCallerName()
	{
		try
		{
			const int framesToSkip = 2;
			var frame = new StackFrame(framesToSkip, false);
			var method = frame.GetMethod();
			if (method is null) return null;
			var type = method.DeclaringType;
			return type is null ? method.Name : $"{type.Name}.{method.Name}";
		}
		catch
		{
			return null;
		}
	}

	private static string FormatMessage(Exception ex)
	{
		if (ex is null) return string.Empty;

		var sb = new StringBuilder();

		sb.Append(ex.Message);

		if (ex is ExternalException exex)
		{
			sb.Append($" (error code: {exex.ErrorCode})");
		}

		if (ex.InnerException is { } ex1)
		{
			sb.AppendLine();
			sb.Append($"---> {ex1.Message}");

			if (ex1.InnerException is { } ex2)
			{
				sb.AppendLine();
				sb.Append($"---> {ex2.Message}");

				if (ex2.InnerException is not null)
				{
					// Could recurse, but hey, would have handle circular troubles, so not today

					sb.AppendLine();
					sb.Append("---> ...");
				}
			}
		}

		if (! string.IsNullOrEmpty(ex.StackTrace))
		{
			sb.AppendLine();
			sb.Append(ex.StackTrace);
		}

		return sb.ToString();
	}
}
