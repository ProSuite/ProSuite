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
/// The ShowFoo() methods bring up a modal dialog and log.
/// The ReportFoo() methods do anything modeless and log.
/// The LogFoo() methods only log (no other reporting).
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
			logger.Debug($"Enter {callerName}");
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
	/// Show the window <typeparamref name="T"/> as a modal dialog,
	/// creating and running it (synchronously) on the proper thread.
	/// </summary>
	/// <typeparam name="T">The <see cref="Window"/> subclass to create
	/// and show (must have a public parameterless constructor)</typeparam>
	/// <param name="viewModel">The view model (optional, used
	/// as the window's <see cref="Window.DataContext"/>)</param>
	/// <returns>The value of the <see cref="Window.DialogResult"/>
	/// property just before the window closes</returns>
	public static bool? ShowDialog<T>(object viewModel) where T : Window
	{
		try
		{
			var dispatcher = Application.Current.Dispatcher;

			return dispatcher.Invoke(() =>
			{
				// Available only on thread that created the Application:
				Window owner = Application.Current.MainWindow;

				var dialog = (Window) Activator.CreateInstance(typeof(T));
				if (dialog is null) return null;

				dialog.Owner = owner;
				dialog.DataContext = viewModel;

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
	public static void ShowError(Exception ex, IMsg logger, string caption = null)
	{
		if (ex is null) return;

		if (caption is null)
		{
			string callerName = GetCallerName();
			caption = $"Error in {callerName}";
		}

		var message = FormatMessage(ex);

		logger ??= _msg; // default to our own logger
		logger.Error($"{caption}: {message}", ex);

		ShowMessage(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	/// <summary>
	/// Report the given error (exception) without interrupting the
	/// user (i.e., no modal dialog box): write it to the given logger
	/// (and maybe to other feedback channels, e.g. toast notifications).
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ReportError(Exception ex, IMsg logger, string caption = null)
	{
		// Here we COULD consider a toast notification (in addition to a log entry)
		// For now, just forward to LogError (but only after GetCallerName, which
		// makes call stack assumptions):

		if (ex is null) return;

		if (caption is null)
		{
			string callerName = GetCallerName();
			caption = $"Error in {callerName}";
		}

		LogError(ex, logger, caption);
	}

	/// <summary>
	/// Write the given error to the appropriate log.
	/// Do no other reporting (no dialog, no toast, just log).
	/// The <paramref name="caption"/> defaults to the caller's name.
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void LogError(Exception ex, IMsg logger, string caption = null)
	{
		if (ex is null) return;

		if (caption is null)
		{
			string callerName = GetCallerName();
			caption = $"Error in {callerName}";
		}

		var message = FormatMessage(ex);

		logger ??= _msg; // default to our own logger
		logger.Error($"{caption}: {message}", ex);
	}

	/// <summary>
	/// Report the given error message: (1) write it to
	/// the given logger, and (2) show it in a modal message box.
	/// The <paramref name="caption"/> defaults to the caller's name.
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	[MethodImpl(MethodImplOptions.NoInlining)]
	public static void ShowError(string message, IMsg logger, string caption = null)
	{
		if (message is null) return;

		logger ??= _msg; // default to our own logger

		if (caption is null)
		{
			logger.Error(message);
		}
		else
		{
			logger.Error($"{caption}: {message}");
		}

		ShowMessage(message, caption, MessageBoxButton.OK, MessageBoxImage.Error);
	}

	/// <summary>
	/// Report the given error message without interrupting the
	/// user (i.e., no modal dialog box): write it to the given logger
	/// (and maybe to other feedback channels, e.g., toast notifications).
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	public static void ReportError(string message, IMsg logger, string caption = null)
	{
		// Here we COULD consider a toast notification (in addition to a log entry)
		// For now, just forward to LogError:
		LogError(message, logger, caption);
	}

	/// <summary>
	/// Write the given message to the appropriate log.
	/// Do no other reporting (no dialog, no toast, just log).
	/// </summary>
	/// <remarks>This method SHALL NOT throw exceptions.</remarks>
	public static void LogError(string message, IMsg logger, string caption = null)
	{
		if (message is null) return;

		logger ??= _msg; // default to our own logger

		if (string.IsNullOrEmpty(caption))
		{
			logger.Error(message);
		}
		else
		{
			logger.Error($"{caption}: {message}");
		}
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
			const int framesToSkip = 2; // GetCallerName() and LogEntry()
			var frame = new StackFrame(framesToSkip);
			//var method = frame.GetMethod();
			//if (method is null) return null;
			//var type = method.DeclaringType;
			//return type is null ? method.Name : $"{type.Name}.{method.Name}";
			var trace = new StackTrace(frame);
			var s = trace.ToString().Trim();
			if (s.StartsWith("at ")) s = s.Substring(3);
			return s;

			// The detour through a single-frame stack trace is to cope with
			// compiler-generated code (yielding and async methods): ToString()
			// on StackTrace resolves these additional methods on the call stack
			// to the one the programmer actually wrote.
			//
			// For example, if LogEntry() is called from an async method, the
			// method is reported to be "MoveNext", which is part of the state
			// machine generated by the compiler from the async method. Or, if
			// LogEntry() is called from within an iterator method "Foo", the
			// declaring type would be the compiler-generated "<Foo>__d".
			// For details decompile StackTrace.ToString() and read
			// https://devblogs.microsoft.com/dotnet/how-async-await-really-works/
			// (copy at https://dev.to/dotnet/how-asyncawait-really-works-in-c-4ia1)
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
