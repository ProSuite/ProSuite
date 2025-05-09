using System;
using System.Diagnostics;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Logging
{
	public interface IMsg
	{
		/// <summary>
		/// Increments the indentation level for subsequent messages.
		/// </summary>
		/// <returns>Disposable callback for safely decrementing the indentation level by means of a <c>using</c> block.</returns>
		/// <remarks>The return value can optionally be used as follows:
		/// <code>
		/// _msg.Debug("start process xy");
		/// 
		/// using (_msg.IncrementIndention())
		/// {
		///     _msg.Debug("doing stuff");
		///     DoStuff();
		/// 
		///     _msg.Debug("doing more stuff");
		///     DoMoreStuff();
		/// }
		/// 
		/// _msg.Debug("done with process xy");
		/// </code>
		/// This guarantees that the indentation is decremented at the end of the using block. 
		/// Alternatively, the return value can be ignored and the indentation can be decremented manually.
		/// </remarks>
		[NotNull]
		IDisposable IncrementIndentation();

		[NotNull]
		IDisposable IncrementIndentation([CanBeNull] string infoMessage);

		[NotNull]
		IDisposable IncrementIndentation([StructuredMessageTemplate] string infoFormat,
		                                 params object[] args);

		/// <summary>
		/// Decrements the indentation level.
		/// </summary>
		void DecrementIndentation();

		/// <summary>
		/// Resets the indentation to level 0 (no indentation).
		/// </summary>
		void ResetIndentation();

		/// <summary>
		/// Gets the current indentation level.
		/// </summary>
		/// <value>The indentation level.</value>
		int IndentationLevel { get; }

		/// <summary>
		/// Gets the maximum indentation level.
		/// </summary>
		/// <value>The maximum indentation level.</value>
		int MaximumIndentationLevel { get; }

		[Obsolete(
			"Use VerboseDebug with Func<string> or Debug in conjunction with IsVerboseDebugEnabled")]
		void VerboseDebugFormat([StructuredMessageTemplate] string format, params object[] args);

		[Obsolete(
			"Use VerboseDebug with Func<string> or Debug in conjunction with IsVerboseDebugEnabled")]
		void VerboseDebug(object message);

		[Obsolete(
			"Use VerboseDebug with Func<string> or Debug in conjunction with IsVerboseDebugEnabled")]
		void VerboseDebug(object message, Exception exception);

		void VerboseDebug(Func<string> message);

		void VerboseDebug(Func<string> message, Exception exception);

		/// <summary>
		/// Logs a formatted message string with the Debug level.
		/// </summary>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <remarks>The message is formatted using the <c>String.Format</c> method. See 
		/// <see cref="String.Format(string, object)"/> for details of the syntax of the 
		/// format string and the behavior of the formatting.<para/>
		/// This method does not take an Exception object to include in the log event.
		/// To pass an Exception use one of the <see cref="Debug(object)"/> methods instead.</remarks>
		void DebugFormat([StructuredMessageTemplate] string format, params object[] args);

		/// <summary>
		/// Log a message object with the Debug level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>This method first checks if this logger is DEBUG enabled by 
		/// comparing the level of this logger with the Debug level. 
		/// If this logger is DEBUG enabled, then it converts the message object 
		/// (passed as parameter) to a string by invoking the appropriate 
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then proceeds to call all the registered 
		/// appenders in this logger and also higher in the hierarchy 
		/// depending on the value of the additivity flag.<para/>
		/// WARNING Note that passing an Exception to this method will print the name 
		/// of the Exception but no stack trace. To print a stack trace use the 
		/// <see cref="Debug(object, Exception)"/> form instead. 
		/// </remarks>
		void Debug(object message);

		/// <summary>
		/// Log a message object with the Debug level including the stack trace of the 
		/// Exception passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="exception">The exception to log, including its stack trace.</param>
		/// <remarks>See the <see cref="Debug(object)"/> form for more detailed information.</remarks>
		void Debug(object message, Exception exception);

		void DebugMemory(object message);

		void DebugMemory([StructuredMessageTemplate] string format, params object[] args);

		/// <summary>
		/// Logs a formatted message string with the Info level.
		/// </summary>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <remarks>The message is formatted using the <c>String.Format</c> method. See 
		/// <see cref="String.Format(string, object)"/> for details of the syntax of the 
		/// format string and the behavior of the formatting.<para/>
		/// This method does not take an Exception object to include in the log event.
		/// To pass an Exception use one of the <see cref="Info(object)"/> methods instead.</remarks>
		void InfoFormat([StructuredMessageTemplate] string format, params object[] args);

		/// <summary>
		/// Log a message object with the Info level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>This method first checks if this logger is INFO enabled by 
		/// comparing the level of this logger with the Info level. 
		/// If this logger is INFO enabled, then it converts the message object 
		/// (passed as parameter) to a string by invoking the appropriate 
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then proceeds to call all the registered 
		/// appenders in this logger and also higher in the hierarchy 
		/// depending on the value of the additivity flag.<para/>
		/// WARNING Note that passing an Exception to this method will print the name 
		/// of the Exception but no stack trace. To print a stack trace use the 
		/// <see cref="Info(object, Exception)"/> form instead. 
		/// </remarks>        
		void Info(object message);

		/// <summary>
		/// Log a message object with the Info level including the stack trace of the 
		/// Exception passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="exception">The exception to log, including its stack trace.</param>
		/// <remarks>See the <see cref="Info(object)"/> form for more detailed information.</remarks>
		void Info(object message, Exception exception);

		/// <summary>
		/// Logs a formatted message string with the Warn level.
		/// </summary>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <remarks>The message is formatted using the <c>String.Format</c> method. See 
		/// <see cref="String.Format(string, object)"/> for details of the syntax of the 
		/// format string and the behavior of the formatting.<para/>
		/// This method does not take an Exception object to include in the log event.
		/// To pass an Exception use one of the <see cref="Warn(object)"/> methods instead.</remarks>
		void WarnFormat([StructuredMessageTemplate] string format, params object[] args);

		/// <summary>
		/// Log a message object with the Warn level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>This method first checks if this logger is WARN enabled by 
		/// comparing the level of this logger with the Warn level. 
		/// If this logger is WARN enabled, then it converts the message object 
		/// (passed as parameter) to a string by invoking the appropriate 
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then proceeds to call all the registered 
		/// appenders in this logger and also higher in the hierarchy 
		/// depending on the value of the additivity flag.<para/>
		/// WARNING Note that passing an Exception to this method will print the name 
		/// of the Exception but no stack trace. To print a stack trace use the 
		/// <see cref="Warn(object, Exception)"/> form instead. 
		/// </remarks>        
		void Warn(object message);

		/// <summary>
		/// Log a message object with the Warn level including the stack trace of the 
		/// Exception passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="exception">The exception to log, including its stack trace.</param>
		/// <remarks>See the <see cref="Warn(object)"/> form for more detailed information.</remarks>
		void Warn(object message, Exception exception);

		/// <summary>
		/// Logs a formatted message string with the Error level.
		/// </summary>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <remarks>The message is formatted using the <c>String.Format</c> method. See 
		/// <see cref="String.Format(string, object)"/> for details of the syntax of the 
		/// format string and the behavior of the formatting.<para/>
		/// This method does not take an Exception object to include in the log event.
		/// To pass an Exception use one of the <see cref="Error(object)"/> methods instead.</remarks>
		void ErrorFormat([StructuredMessageTemplate] string format, params object[] args);

		/// <summary>
		/// Log a message object with the Error level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>This method first checks if this logger is ERROR enabled by 
		/// comparing the level of this logger with the Error level. 
		/// If this logger is ERROR enabled, then it converts the message object 
		/// (passed as parameter) to a string by invoking the appropriate 
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then proceeds to call all the registered 
		/// appenders in this logger and also higher in the hierarchy 
		/// depending on the value of the additivity flag.<para/>
		/// WARNING Note that passing an Exception to this method will print the name 
		/// of the Exception but no stack trace. To print a stack trace use the 
		/// <see cref="Error(object, Exception)"/> form instead. 
		/// </remarks>        
		void Error(object message);

		/// <summary>
		/// Log a message object with the Error level including the stack trace of the 
		/// Exception passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="exception">The exception to log, including its stack trace.</param>
		/// <remarks>See the <see cref="Error(object)"/> form for more detailed information.</remarks>
		void Error(object message, Exception exception);

		/// <summary>
		/// Logs a formatted message string with the Fatal level.
		/// </summary>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <remarks>The message is formatted using the <c>String.Format</c> method. See 
		/// <see cref="String.Format(string, object)"/> for details of the syntax of the 
		/// format string and the behavior of the formatting.<para/>
		/// This method does not take an Exception object to include in the log event.
		/// To pass an Exception use one of the <see cref="Fatal(object)"/> methods instead.</remarks>
		void FatalFormat([StructuredMessageTemplate] string format, params object[] args);

		/// <summary>
		/// Log a message object with the Fatal level.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <remarks>This method first checks if this logger is FATAL enabled by 
		/// comparing the level of this logger with the Fatal level. 
		/// If this logger is FATAL enabled, then it converts the message object 
		/// (passed as parameter) to a string by invoking the appropriate 
		/// <see cref="log4net.ObjectRenderer.IObjectRenderer"/>. It then proceeds to call all the registered 
		/// appenders in this logger and also higher in the hierarchy 
		/// depending on the value of the additivity flag.<para/>
		/// WARNING Note that passing an Exception to this method will print the name 
		/// of the Exception but no stack trace. To print a stack trace use the 
		/// <see cref="Fatal(object, Exception)"/> form instead. 
		/// </remarks>  
		void Fatal(object message);

		/// <summary>
		/// Log a message object with the Fatal level including the stack trace of the 
		/// Exception passed as a parameter.
		/// </summary>
		/// <param name="message">The message object to log.</param>
		/// <param name="exception">The exception to log, including its stack trace.</param>
		/// <remarks>See the <see cref="Fatal(object)"/> form for more detailed information.</remarks>
		void Fatal(object message, Exception exception);

		/// <summary>
		/// Return a started <see cref="Stopwatch"/> if Debug is enabled.
		/// </summary>
		/// <returns>Started <see cref="Stopwatch"/>.</returns>
		[CanBeNull]
		Stopwatch DebugStartTiming();

		/// <summary>
		/// Return a started <see cref="Stopwatch"/> if Debug is enabled, and logs a formatted 
		/// message with the Debug level.
		/// </summary>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <returns>Started <see cref="Stopwatch"/>.</returns>
		[CanBeNull]
		Stopwatch DebugStartTiming([StructuredMessageTemplate] [CanBeNull] string format,
		                           params object[] args);

		/// <summary>
		/// Log a message object with the Debug level, appending a string that reports
		/// the elapsed milliseconds as measured by a Stopwatch.
		/// </summary>
		/// <param name="stopwatch">The running <see cref="Stopwatch"/>.</param>
		/// <param name="format">A String containing zero or more format items.</param>
		/// <param name="args">An Object array containing zero or more objects to format</param>
		/// <remarks>The <see cref="Stopwatch"/> is stopped before reporting the elapsed time.</remarks>
		void DebugStopTiming([CanBeNull] Stopwatch stopwatch,
		                     [StructuredMessageTemplate] [CanBeNull]
		                     string format,
		                     params object[] args);

		/// <summary>
		/// Checks if this logger is enabled for the Debug level.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this logger is enabled for Debug events, otherwise, <c>false</c>.
		/// </value>
		/// <remarks>This function is intended to lessen the computational cost of 
		/// disabled log debug statements.</remarks>
		bool IsDebugEnabled { get; }

		/// <summary>
		/// Checks if this logger is enabled for the Debug level.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this logger is enabled for Debug events, otherwise, <c>false</c>.
		/// </value>
		/// <remarks>This function is intended to lessen the computational cost of 
		/// disabled log debug statements.</remarks>
		bool IsVerboseDebugEnabled { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether memory consumption figures should be added
		/// to error (and fatal) messages
		/// </summary>
		/// <value>
		/// 	<c>true</c> if memory figures are appended to all error or fatal messages; 
		/// otherwise, <c>false</c>.
		/// </value>
		bool ReportMemoryConsumptionOnError { get; set; }

		/// <summary>
		/// Checks if this logger is enabled for the Info level.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this logger is enabled for Info events, otherwise, <c>false</c>.
		/// </value>
		bool IsInfoEnabled { get; }

		/// <summary>
		/// Checks if this logger is enabled for the Warn level.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this logger is enabled for Warn events, otherwise, <c>false</c>.
		/// </value>
		bool IsWarnEnabled { get; }

		/// <summary>
		/// Checks if this logger is enabled for the Error level.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this logger is enabled for Error events, otherwise, <c>false</c>.
		/// </value>
		bool IsErrorEnabled { get; }

		/// <summary>
		/// Checks if this logger is enabled for the Fatal level.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this logger is enabled for Fatal events, otherwise, <c>false</c>.
		/// </value>
		bool IsFatalEnabled { get; }
	}
}
