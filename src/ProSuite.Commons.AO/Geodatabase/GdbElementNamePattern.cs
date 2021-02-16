using System;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class GdbElementNamePattern
	{
		private readonly string _pattern;
		private const char _separator = '.';

		private readonly Regex _nameRegex;
		private readonly Regex _ownerRegex;
		private readonly Regex _databaseRegex;

		public GdbElementNamePattern([NotNull] string pattern, bool matchCase = false)
		{
			Assert.ArgumentNotNull(pattern, nameof(pattern));

			_pattern = pattern;

			string[] tokens = pattern.Split(_separator);

			if (tokens.Length > 0)
			{
				_nameRegex = CreateWildcardRegex(tokens[tokens.Length - 1], matchCase);
			}

			if (tokens.Length > 1)
			{
				_ownerRegex = CreateWildcardRegex(tokens[tokens.Length - 2], matchCase);
			}

			if (tokens.Length > 2)
			{
				_databaseRegex = CreateWildcardRegex(tokens[tokens.Length - 3], matchCase);
			}

			if (tokens.Length > 3)
			{
				throw new ArgumentOutOfRangeException(nameof(pattern), pattern,
				                                      @"Name filter must not contain more than three name parts");
			}
		}

		[NotNull]
		public string Pattern => _pattern;

		public bool Matches([NotNull] IDatasetName datasetName)
		{
			string databaseName;
			string ownerName;
			string tableName;
			DatasetUtils.ParseTableName(datasetName,
			                            out databaseName,
			                            out ownerName,
			                            out tableName);

			return Matches(tableName, ownerName, databaseName);
		}

		public bool Matches([CanBeNull] string unqualifiedName,
		                    [CanBeNull] string ownerName = null,
		                    [CanBeNull] string databaseName = null)
		{
			if (_nameRegex != null && ! _nameRegex.IsMatch(unqualifiedName ?? string.Empty))
			{
				return false;
			}

			if (_ownerRegex != null && ! _ownerRegex.IsMatch(ownerName ?? string.Empty))
			{
				return false;
			}

			if (_databaseRegex != null &&
			    ! _databaseRegex.IsMatch(databaseName ?? string.Empty))
			{
				return false;
			}

			// all defined regexes (if any) match
			return true;
		}

		[NotNull]
		private static Regex CreateWildcardRegex([NotNull] string token, bool matchCase)
		{
			const bool matchCompleteString = true;
			return RegexUtils.GetWildcardMatchRegex(token, matchCase,
			                                        matchCompleteString);
		}
	}
}
