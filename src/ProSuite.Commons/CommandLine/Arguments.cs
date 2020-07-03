using System;
using System.Linq;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.CommandLine
{
	public class Arguments
	{
		private readonly string[] _args;

		/// <summary>
		/// Initializes a new instance of the <see cref="Arguments"/> class.
		/// </summary>
		/// <param name="argString">The arg string.</param>
		public Arguments([NotNull] string argString)
			: this(argString.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries)) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="Arguments"/> class.
		/// </summary>
		/// <param name="args">The args.</param>
		public Arguments([NotNull] string[] args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			_args = args;
		}

		public int Count
		{
			get { return _args.Length; }
		}

		[CanBeNull]
		public string GetValue([NotNull] string argumentIdentifier)
		{
			for (var i = 0; i < _args.Length; i++)
			{
				if (! _args[i].Equals(argumentIdentifier))
				{
					continue;
				}

				return (i + 1) > _args.Length - 1
					       ? null // there's no argument after the identifier
					       : _args[i + 1];
			}

			return null;
		}

		public bool Exists([NotNull] string argumentIdentifier)
		{
			return _args.Any(arg => arg.Equals(argumentIdentifier));
		}

		public string Concatenate()
		{
			return Concatenate(string.Empty);
		}

		public string Concatenate(params string[] passwordIdentifiers)
		{
			// passwordIdentifier may be null

			string concatenated = string.Empty;

			var argIsPassword = false;
			var first = true;

			foreach (string arg in _args)
			{
				if (! first)
				{
					concatenated += " ";
				}

				first = false;

				if (argIsPassword)
				{
					concatenated += "********";
				}
				else
				{
					concatenated += arg;
				}

				if (passwordIdentifiers != null &&
				    IsContained(arg, passwordIdentifiers))
				{
					argIsPassword = true;
				}
				else
				{
					argIsPassword = false;
				}
			}

			return concatenated;
		}

		public string Concatenate(params int[] passwordIndexes)
		{
			// passwordIdentifier may be null

			string concatenated = string.Empty;

			var first = true;

			for (var i = 0; i < _args.Length; i++)
			{
				if (! first)
				{
					concatenated += " ";
				}

				first = false;

				if (passwordIndexes.Contains(i))
				{
					concatenated += "********";
				}
				else
				{
					concatenated += _args[i];
				}
			}

			return concatenated;
		}

		private static bool IsContained([NotNull] string searchString,
		                                params string[] inStrings)
		{
			foreach (string s in inStrings)
			{
				if (Equals(s, searchString))
				{
					return true;
				}
			}

			return false;
		}

		public bool HasValue([NotNull] string argumentIdentifier,
		                     [NotNull] string argumentValue)
		{
			Assert.ArgumentNotNullOrEmpty(argumentIdentifier, nameof(argumentIdentifier));
			Assert.ArgumentNotNullOrEmpty(argumentValue, nameof(argumentValue));

			string actualValue = GetValue(argumentIdentifier);

			return argumentValue.Equals(actualValue, StringComparison.OrdinalIgnoreCase);
		}
	}
}