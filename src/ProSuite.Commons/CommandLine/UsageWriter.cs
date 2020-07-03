using System;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.CommandLine
{
	public class UsageWriter
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly UsageTarget _target;

		public UsageWriter()
		{
			_target = UsageTarget.Console;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UsageWriter"/> class.
		/// </summary>
		/// <param name="target">The target.</param>
		public UsageWriter(UsageTarget target)
		{
			_target = target;
		}

		public string FormatOptions(bool optional, params string[] options)
		{
			var sb = new StringBuilder();

			sb.Append(optional
				          ? "{"
				          : "<");

			for (int i = 0; i < options.Length; i++)
			{
				sb.Append(options[i]);

				if (i != options.Length - 1)
				{
					sb.Append(" | ");
				}
			}

			sb.Append(optional
				          ? "}"
				          : ">");

			return sb.ToString();
		}

		public string WriteArgUsage(string argumentIdentifier, string message)
		{
			return WriteArgUsage(argumentIdentifier, message, false);
		}

		public string WriteArgUsage(string argumentIdentifier, string message,
		                            bool optional)
		{
			const int defaultBlanks = 5;

			return WriteArgUsage(argumentIdentifier, message, optional, defaultBlanks);
		}

		public string WriteArgUsage(string argumentIdentifier, string message,
		                            bool optional, int leadingBlanks)
		{
			var format = new string(' ', leadingBlanks);

			if (optional)
			{
				format += "[{0}] {1}";
			}
			else
			{
				format += "{0} {1}";
			}

			return Write(format, argumentIdentifier, message);
		}

		public void WriteLine()
		{
			if ((_target & UsageTarget.Console) != 0)
			{
				Console.Out.WriteLine();
			}

			if ((_target & UsageTarget.Log) != 0)
			{
				_msg.Info(string.Empty);
			}
		}

		[StringFormatMethod("format")]
		public string Write(string format, params object[] args)
		{
			string message = string.Format(format, args);

			if ((_target & UsageTarget.Console) != 0)
			{
				Console.Out.WriteLine(message);
			}

			if ((_target & UsageTarget.Log) != 0)
			{
				_msg.Info(message);
			}

			return message;
		}
	}
}