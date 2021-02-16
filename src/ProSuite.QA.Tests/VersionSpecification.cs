using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests
{
	public class VersionSpecification : IEquatable<VersionSpecification>
	{
		private const string _invalidVersionFormat = "Invalid version string: {0}";

		[NotNull]
		public static VersionSpecification Create([NotNull] string versionString)
		{
			Assert.ArgumentNotNullOrEmpty(versionString, nameof(versionString));

			var separators = new[] {'.'};

			string[] tokens = versionString.Split(separators, StringSplitOptions.None);
			Assert.ArgumentCondition(tokens.Length > 0 && tokens.Length < 4,
			                         _invalidVersionFormat,
			                         (object) versionString);

			int major;
			if (! int.TryParse(tokens[0], out major))
			{
				throw new ArgumentException(string.Format(_invalidVersionFormat, versionString));
			}

			int? minor = GetOptionalVersionComponent(versionString, tokens, 1);
			int? bugfix = GetOptionalVersionComponent(versionString, tokens, 2);

			if (bugfix != null && minor == null)
			{
				throw new ArgumentException(string.Format(_invalidVersionFormat, versionString));
			}

			return new VersionSpecification(major, minor, bugfix);
		}

		private readonly string _versionString;

		public VersionSpecification(int majorVersion, int? minorVersion, int? bugfixVersion)
		{
			Assert.ArgumentCondition(majorVersion > 0,
			                         "Invalid major version: {0}", (object) majorVersion);
			Assert.ArgumentCondition(minorVersion == null || minorVersion > 0,
			                         "Invalid minor version: {0}", (object) minorVersion);
			Assert.ArgumentCondition(bugfixVersion == null || bugfixVersion > 0,
			                         "Invalid bugfix version: {0}", (object) bugfixVersion);
			Assert.ArgumentCondition(bugfixVersion == null || minorVersion != null,
			                         "If minor version is undefined then bugfix version must not be defined");

			MajorVersion = majorVersion;
			MinorVersion = minorVersion;
			BugfixVersion = bugfixVersion;

			const string undefinedPlaceholder = "*";

			_versionString = string.Format("{0}.{1}.{2}",
			                               majorVersion,
			                               minorVersion == null
				                               ? undefinedPlaceholder
				                               : minorVersion.ToString(),
			                               bugfixVersion == null
				                               ? undefinedPlaceholder
				                               : bugfixVersion.ToString());
		}

		public int MajorVersion { get; set; }

		public int? MinorVersion { get; set; }

		public int? BugfixVersion { get; set; }

		[NotNull]
		public string VersionString => _versionString;

		public bool IsLowerThan(int major, int minor, int bugfix)
		{
			if (MajorVersion < major)
			{
				return true;
			}

			if (MajorVersion > major)
			{
				return false;
			}

			// major is equal
			if (MinorVersion == null || MinorVersion > minor)
			{
				return false;
			}

			if (MinorVersion < minor)
			{
				return true;
			}

			// major and minor are equal
			return BugfixVersion != null && BugfixVersion < bugfix;
		}

		public bool IsGreaterThan(int major, int minor, int bugfix)
		{
			if (MajorVersion < major)
			{
				return false;
			}

			if (MajorVersion > major)
			{
				return true;
			}

			// major is equal
			if (MinorVersion == null || MinorVersion < minor)
			{
				return false;
			}

			if (MinorVersion > minor)
			{
				return true;
			}

			// major and minor are equal
			return BugfixVersion != null && BugfixVersion > bugfix;
		}

		public bool Equals(VersionSpecification other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other.MajorVersion == MajorVersion &&
			       other.MinorVersion.Equals(MinorVersion) &&
			       other.BugfixVersion.Equals(BugfixVersion);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(VersionSpecification))
			{
				return false;
			}

			return Equals((VersionSpecification) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = MajorVersion;
				result = (result * 397) ^ (MinorVersion.HasValue
					                           ? MinorVersion.Value
					                           : 0);
				result = (result * 397) ^ (BugfixVersion.HasValue
					                           ? BugfixVersion.Value
					                           : 0);
				return result;
			}
		}

		public override string ToString()
		{
			return _versionString;
		}

		private static int? GetOptionalVersionComponent([NotNull] string versionString,
		                                                [NotNull] IList<string> tokens,
		                                                int tokenIndex)
		{
			if (tokens.Count <= tokenIndex)
			{
				return null;
			}

			string token = tokens[tokenIndex];
			Assert.ArgumentCondition(StringUtils.IsNotEmpty(token),
			                         _invalidVersionFormat,
			                         (object) versionString);

			const string wildcard = "*";

			if (Equals(token.Trim(), wildcard))
			{
				return null;
			}

			int parsed;
			if (int.TryParse(token, out parsed) && parsed >= 0)
			{
				return parsed;
			}

			throw new ArgumentException(string.Format(_invalidVersionFormat, versionString));
		}
	}
}
