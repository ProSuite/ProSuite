using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DomainModel.AO.Workflow
{
	public class DatasetNameTransformer : IDatasetNameTransformer
	{
		private readonly ICollection<PatternReplacement> _patternReplacements;
		private const string _assignmentSeparator = "==>";

		public DatasetNameTransformer([CanBeNull] string transformationPatterns)
		{
			_patternReplacements = GetPatternReplacements(transformationPatterns);
		}

		[NotNull]
		private static ICollection<PatternReplacement> GetPatternReplacements(
			[CanBeNull] string patternReplacementText)
		{
			var result = new List<PatternReplacement>();

			if (patternReplacementText == null ||
			    StringUtils.IsNullOrEmptyOrBlank(patternReplacementText))
			{
				return result;
			}

			string[] lines = patternReplacementText.Split(new[] {Environment.NewLine},
			                                              StringSplitOptions.RemoveEmptyEntries);

			foreach (string line in lines)
			{
				string pattern;
				string replacement;
				if (line.IndexOf(_assignmentSeparator, StringComparison.Ordinal) < 0)
				{
					pattern = line.Trim();
					replacement = null;
				}
				else
				{
					string[] tokens = line.Split(new[] {_assignmentSeparator},
					                             StringSplitOptions.RemoveEmptyEntries);

					switch (tokens.Length)
					{
						case 1:
							pattern = tokens[0].Trim();
							replacement = null;
							break;

						case 2:
							pattern = tokens[0].Trim();
							replacement = tokens[1].Trim();
							break;

						default:
							throw new ArgumentException(
								$"Invalid pattern line (unexpected number of assignments): {line}");
					}
				}

				if (StringUtils.IsNullOrEmptyOrBlank(pattern))
				{
					throw new ArgumentException(
						$"Invalid pattern line (empty pattern): {line}");
				}

				result.Add(new PatternReplacement(pattern, replacement));
			}

			return result;
		}

		public string TransformName(string datasetName)
		{
			foreach (PatternReplacement replacement in _patternReplacements)
			{
				if (replacement.IsMatch(datasetName))
				{
					return replacement.Replace(datasetName);
				}
			}

			return datasetName;
		}

		private class PatternReplacement
		{
			[NotNull] private readonly Regex _regex;
			[CanBeNull] private readonly string _replacement;

			public PatternReplacement([NotNull] string pattern, [CanBeNull] string replacement)
			{
				Assert.ArgumentNotNullOrEmpty(pattern, nameof(pattern));

				_replacement = replacement;
				_regex = new Regex(pattern, RegexOptions.IgnoreCase |
				                            RegexOptions.Singleline |
				                            RegexOptions.Compiled);
			}

			public bool IsMatch([NotNull] string text)
			{
				return _regex.IsMatch(text);
			}

			public string Replace([NotNull] string text)
			{
				return _regex.Replace(text, _replacement ?? string.Empty);
			}
		}
	}
}
