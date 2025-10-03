using System;
using System.Collections.Generic;

namespace ProSuite.Commons.CommandLine
{
	public static class CommandLineUtils
	{
		/// <summary>
		/// Look in <paramref name="args"/> for all "--foo bar" and "-foo=bar"
		/// and "--foo" (flag option) BEFORE any non-option argument or "--".
		/// </summary>
		/// <remarks>
		/// Global options are those options that occur before any
		/// non-option argument on a command line. This is similar
		/// but simpler than what CommandLineConfigurationProvider
		/// (in Microsoft.Extensions.Configuration.CommandLine) does, and
		/// we only look at global options, and we never throw an exception!
		/// <para/>
		/// If using .NET Core's IConfiguration system, consider an
		/// extension method AddGlobalOptions on IConfigurationBuilder
		/// that takes an args array, calls this function, and adds an
		/// in-memory collection to the configuration builder (but don't
		/// do it here as we don't want this dependency).
		/// </remarks>
		/// <returns>Dictionary of (optionName, argument) pairs;
		/// flags are returned as (optionName, "true") pairs</returns>
		public static Dictionary<string, string> ParseGlobalOptions(string[] args)
		{
			const string trueString = "true"; // "argument" for flags

			var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			if (args is null)
			{
				return data;
			}

			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				if (arg == "--") break; // end of options
				if (! arg.StartsWith("-")) break; // end of options
				if (! arg.StartsWith("--")) continue; // skip short option

				string key, value;
				int index = arg.IndexOf('=');

				if (index < 0)
				{
					key = arg.Substring(2);

					if (i + 1 >= args.Length || args[i + 1].StartsWith("-"))
					{
						value = trueString; // option is a flag (has no arg)
					}
					else
					{
						value = args[++i]; // next arg is option value
					}
				}
				else
				{
					key = arg.Substring(2, index - 2);
					value = arg.Substring(index + 1);
				}

				data[key] = value; // last one wins
			}

			return data;
		}
	}
}
