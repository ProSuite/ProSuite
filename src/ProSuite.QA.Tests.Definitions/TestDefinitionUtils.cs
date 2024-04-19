using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Tests
{
	public static class TestDefinitionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly char[] _tokenSeparators = { ' ', ',', ';' };

		[NotNull]
		public static IEnumerable<string> GetTokens([CanBeNull] string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				yield break;
			}

			foreach (
				string token in
				text.Split(_tokenSeparators, StringSplitOptions.RemoveEmptyEntries))
			{
				if (string.IsNullOrEmpty(token))
				{
					continue;
				}

				yield return token.Trim();
			}
		}
	}
}
