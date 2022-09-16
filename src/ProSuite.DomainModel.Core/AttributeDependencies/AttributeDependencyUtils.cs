using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.DataModel;
using Attribute = ProSuite.DomainModel.Core.DataModel.Attribute;

namespace ProSuite.DomainModel.Core.AttributeDependencies
{
	/// <summary>
	/// A container for AttributeDependency-related utilities.
	/// </summary>
	public static class AttributeDependencyUtils
	{
		/// <summary>
		/// Convert <i>value</i> to a type suitable for <i>fieldType</i>.
		/// If <i>culture</i> is null, use InvariantCulture.
		/// Impossible conversions will throw an exception.
		/// </summary>
		public static object Convert([CanBeNull] object value, FieldType fieldType,
		                             [CanBeNull] IFormatProvider culture)
		{
			if (culture == null)
			{
				culture = CultureInfo.InvariantCulture;
			}

			if (value is string s)
			{
				s = s.Trim();

				if (string.Equals(s, "null", StringComparison.OrdinalIgnoreCase))
				{
					return DBNull.Value;
				}

				if (string.Equals(s, Wildcard.ValueString))
				{
					return Wildcard.Value;
				}

				if (fieldType == FieldType.ShortInteger ||
				    fieldType == FieldType.LongInteger)
				{
					if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
					{
						return 0;
					}

					if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
					{
						return 1;
					}
				}

				if (fieldType == FieldType.Text)
				{
					if (s.Length > 1 && s[0] == '"' && s[s.Length - 1] == '"')
					{
						s = s.Substring(1, s.Length - 2); // strip quotes
					}

					return s; // trimmed, double quotes stripped
				}
			}

			if (value == null || value == DBNull.Value || value == Wildcard.Value)
			{
				return value; // don't convert these special values
			}

			switch (fieldType)
			{
				case FieldType.ShortInteger:
				case FieldType.LongInteger:
					return System.Convert.ToInt32(value, culture);

				case FieldType.Float:
					return System.Convert.ToSingle(value, culture);
				case FieldType.Double:
					return System.Convert.ToDouble(value, culture);

				case FieldType.Text:
					return System.Convert.ToString(value, culture);

				case FieldType.Date:
					return System.Convert.ToDateTime(value, culture);

				case FieldType.Guid:
					throw new NotImplementedException(); // TODO string => GUID

				//case esriFieldType.esriFieldTypeOID:
				//case esriFieldType.esriFieldTypeGeometry:
				//case esriFieldType.esriFieldTypeBlob:
				//case esriFieldType.esriFieldTypeRaster:
				//case esriFieldType.esriFieldTypeGlobalID:
				//case esriFieldType.esriFieldTypeXML:

				default:
					throw new NotImplementedException("fieldType not supported");
			}
		}

		/// <summary>
		/// Compare two values, disregarding the multitude of numeric types.
		/// For example, the value 1 can be represented by a Byte, an Int32,
		/// a Double, etc., but it's always the same value, namely 1.
		/// </summary>
		/// <returns>-1 if a &lt; b, 0 if a == b, +1 if a &gt; b</returns>
		public static int Compare(object a, object b)
		{
			bool aIsNull = a == null || System.Convert.IsDBNull(a);
			bool bIsNull = b == null || System.Convert.IsDBNull(b);

			if (aIsNull && bIsNull)
			{
				// Notice!
				// In database logic, NULL does NOT equal NULL,
				// but here, for ordering, NULL does equal NULL.
				return 0;
			}

			if (aIsNull || bIsNull)
			{
				// Null sorts before any other value
				return aIsNull ? -1 : 1;
			}

			if (a is bool boolA && b is bool boolB)
			{
				// Of course, false < true
				return boolA.CompareTo(boolB);
			}

			// Follow C# implicit numeric conversion rules, that is:
			//
			//  sbyte*  => short        int      long       float double decimal
			//  byte    => short ushort int uint long ulong float double decimal
			//  short   =>              int      long       float double decimal
			//  ushort* =>              int uint long ulong float double decimal
			//  int     =>                       long       float double decimal
			//  uint*   =>                       long ulong float double decimal
			//  long    =>                                  float double decimal
			//  ulong*  =>                                  float double decimal
			//  float   =>                                        double
			//  double  => (nix)
			//  char    =>       ushort int uint long ulong float double decimal
			//  decimal => (nix)
			//
			// The asterisk marks types that are not CLS-compliant.
			// There's no implicit conversion from float/double to decimal.
			// See also: http://msdn.microsoft.com/en-us/library/y5b434w4.aspx
			//
			// Beware:
			// Implicit conversion is a C# feature, not a .NET feature!
			// Therefore, for example, "1.0F is double" is false and
			// so is typeof(double).IsAssignableFrom(typeof(float)).

			TypeCode ac = Widen(ref a);
			TypeCode bc = Widen(ref b);

			if (a is long longA && b is long longB)
			{
				return longA.CompareTo(longB);
			}

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (a is double && (b is double || b is long) ||
			    b is double && (a is double || a is long))
			{
				double aa = System.Convert.ToDouble(a);
				double bb = System.Convert.ToDouble(b);

				//Treat insignificant differences as being equal
				// see https://issuetracker04.eggits.net/browse/GEN-2746
				return MathUtils.AreSignificantDigitsEqual(aa, bb) ? 0 : aa.CompareTo(bb);
			}

			// ReSharper disable once ConditionIsAlwaysTrueOrFalse
			if (a is decimal && (b is decimal || b is double || b is long) ||
			    b is decimal && (a is decimal || a is double || a is long))
			{
				decimal aa = System.Convert.ToDecimal(a);
				decimal bb = System.Convert.ToDecimal(b);

				return aa.CompareTo(bb);
			}

			if (a is char charA && b is char charB)
			{
				return charA.CompareTo(charB);
			}

			if (a is string sA && b is string sB)
			{
				return string.CompareOrdinal(sA, sB);
				// TODO Consider overloads with ignoreCase and culture stuff
			}

			if (a is char && b is string || a is string && b is char)
			{
				string aa = System.Convert.ToString(a);
				string bb = System.Convert.ToString(b);

				return string.CompareOrdinal(aa, bb);
				// TODO Consider overloads with ignoreCase and culture stuff
			}

			throw new ApplicationException(
				string.Format("Cannot compare {0} and {1}", ac, bc));
		}

		private static TypeCode Widen(ref object value)
		{
			TypeCode typeCode = System.Convert.GetTypeCode(value);

			switch (typeCode)
			{
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
					value = System.Convert.ToInt64(value);
					return TypeCode.Int64;

				case TypeCode.Single:
					value = System.Convert.ToDouble(value);
					return TypeCode.Double;
			}

			return typeCode;
		}

		#region Alternative approach (test & decide for one or the other)

		public static int Compare2(object a, object b)
		{
			bool aIsNull = a == null || System.Convert.IsDBNull(a);
			bool bIsNull = b == null || System.Convert.IsDBNull(b);

			if (aIsNull && bIsNull)
			{
				// Notice!
				// In database logic NULL does NOT equal NULL,
				// but here, for ordering, NULL does equal NULL.
				return 0;
			}

			if (aIsNull || bIsNull)
			{
				// Null sorts before any other value
				return aIsNull ? -1 : 1;
			}

			TypeCode ac = System.Convert.GetTypeCode(a);
			TypeCode bc = System.Convert.GetTypeCode(b);
			TypeCode targetType = GetCompareType(ac, bc);

			a = System.Convert.ChangeType(a, targetType);
			b = System.Convert.ChangeType(b, targetType);

			if (a is IComparable aa)
			{
				return aa.CompareTo(b);
			}

			throw new InvalidOperationException($"Cannot compare {ac} and {bc}");
		}

		private static TypeCode GetCompareType(TypeCode ac, TypeCode bc)
		{
			int aRank = _typeCodes.IndexOf(ac);
			int aCat = GetTypeCat(ac);

			int bRank = _typeCodes.IndexOf(bc);
			int bCat = GetTypeCat(bc);

			if (aCat != bCat || aRank < 0 || bRank < 0)
			{
				throw new InvalidOperationException($"Cannot compare {ac} and {bc}");
			}

			int rank = Math.Max(aRank, bRank);
			return _typeCodes[rank];
		}

		private static int GetTypeCat(TypeCode typeCode)
		{
			switch (typeCode)
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					return 0;

				case TypeCode.Boolean:
					return 1;

				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return 2;

				case TypeCode.DateTime:
					return 3;

				case TypeCode.Char:
				case TypeCode.String:
					return 4;

				case TypeCode.Object:
					return 9;

				default:
					throw new ArgumentOutOfRangeException(nameof(typeCode));
			}
		}

		private static readonly IList<TypeCode> _typeCodes = new[]
		                                                     {
			                                                     // Ordering is crucial!
			                                                     TypeCode.Empty,
			                                                     TypeCode.DBNull,
			                                                     TypeCode.Boolean,
			                                                     TypeCode.SByte,
			                                                     TypeCode.Byte,
			                                                     TypeCode.Int16,
			                                                     TypeCode.UInt16,
			                                                     TypeCode.Int32,
			                                                     TypeCode.UInt32,
			                                                     TypeCode.Int64,
			                                                     TypeCode.UInt64,
			                                                     TypeCode.Single,
			                                                     TypeCode.Double,
			                                                     TypeCode.Decimal,
			                                                     TypeCode.DateTime,
			                                                     TypeCode.Char,
			                                                     TypeCode.String,
			                                                     TypeCode.Object
		                                                     };

		#endregion

		#region Export Mappings

		public static void ExportMappingsTxt([NotNull] AttributeDependency dependency,
		                                     [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(writer, nameof(writer));

			writer.WriteLine("# Lines starting with a hash are ignored.");
			writer.WriteLine("#");
			writer.WriteLine("# When writing numbers, use \"InvariantCulture\", that is:");
			writer.WriteLine("#    period (.) as decimal separator and no thousands separator.");
			writer.WriteLine("#    Correct example: 1234.5; bad example: 1'234,5.");
			writer.WriteLine("#");

			var sb = new StringBuilder();

			// # one, two, three => four, five # description
			writer.WriteLine(GetHeaderText(dependency, sb));

			foreach (AttributeValueMapping mapping in dependency.AttributeValueMappings)
			{
				sb.Length = 0; // clear
				sb.Append(mapping.SourceText);
				sb.Append(" => ");
				sb.Append(mapping.TargetText);
				if (! string.IsNullOrEmpty(mapping.Description))
				{
					sb.AppendFormat(" # {0}", mapping.Description);
				}

				writer.WriteLine(sb.ToString());
			}
		}

		private static string GetHeaderText(AttributeDependency dependency,
		                                    StringBuilder sb)
		{
			const char delimiter = ',';
			Assert.True(_delimiters.IndexOf(delimiter) >= 0,
			            "delimiter must be one of: {0}", _delimiters);

			sb.Length = 0; // clear
			sb.Append("# ");
			sb.Append(StringUtils.Concatenate(
				          dependency.SourceAttributes.Select(attribute => attribute.Name),
				          delimiter.ToString()));
			sb.Append(" => ");
			sb.Append(StringUtils.Concatenate(
				          dependency.TargetAttributes.Select(attribute => attribute.Name),
				          delimiter.ToString()));
			sb.Append(" # description");

			return sb.ToString();
		}

		public static void ExportMappingsCsv(
			[NotNull] AttributeDependency dependency, [NotNull] TextWriter writer)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(writer, nameof(writer));

			const char separator = ';';
			using (var csv = new CsvWriter(writer, separator))
			{
				var values = new List<object>();

				// Write row header line:
				values.AddRange(dependency.SourceAttributes.Select(attribute => attribute.Name)
				                          .Cast<object>());
				values.AddRange(dependency.TargetAttributes.Select(attribute => attribute.Name)
				                          .Cast<object>());
				values.Add("description");

				csv.WriteRecord(values);

				foreach (AttributeValueMapping mapping in dependency.AttributeValueMappings)
				{
					values.Clear();

					values.AddRange(mapping.SourceValues);
					values.AddRange(mapping.TargetValues);
					values.Add(mapping.Description);

					csv.WriteRecord(values);
				}
			}
		}

		#endregion

		#region Import Mappings

		public static void ImportMappingsTxt([NotNull] AttributeDependency dependency,
		                                     [NotNull] TextReader reader)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(reader, nameof(reader));

			dependency.AttributeValueMappings.Clear();
			// drop all existing mappings (ensure HBM is cascading)

			int sourceCount = dependency.SourceAttributes.Count;
			int targetCount = dependency.TargetAttributes.Count;

			var lineno = 0;
			string line;
			while ((line = reader.ReadLine()) != null)
			{
				lineno += 1;
				line = line.Trim();
				if (line.Length < 1)
				{
					continue; // skip blank line
				}

				if (line[0] == '#')
				{
					continue; // skip comment line
				}

				string sourceText, targetText, description;
				if (SplitLine(line, out sourceText, out targetText, out description))
				{
					var mapping = new AttributeValueMapping(sourceText, targetText,
					                                        description);

					Assert.True(sourceCount == mapping.SourceValues.Count,
					            "Line {0}: expected {1} source values, but got {2}",
					            lineno, sourceCount, mapping.SourceValues.Count);

					Assert.True(targetCount == mapping.TargetValues.Count,
					            "Line {0}: expected {1} target values, but got {2}",
					            lineno, targetCount, mapping.TargetValues.Count);

					dependency.AttributeValueMappings.Add(mapping);
				}
				else
				{
					Assert.Fail("Line {0}: invalid syntax", lineno);
				}
			}
		}

		private static bool SplitLine(string line, out string sourceText,
		                              out string targetText, out string description)
		{
			// Line format: "source => target [# description]"

			sourceText = null;
			targetText = null;
			description = null;

			int mapsToIndex = line.IndexOf("=>", StringComparison.Ordinal);

			if (mapsToIndex < 0)
			{
				return false;
			}

			sourceText = line.Substring(0, mapsToIndex).Trim();

			int commentIndex = line.IndexOf("#", mapsToIndex, StringComparison.Ordinal);

			if (commentIndex < 0)
			{
				targetText = line.Substring(mapsToIndex + 2).Trim();
			}
			else
			{
				int start = mapsToIndex + 2;
				int length = commentIndex - start;
				targetText = line.Substring(start, length).Trim();

				description = line.Substring(commentIndex + 1).Trim();
			}

			return true;
		}

		public static void ImportMappingsCsv(
			[NotNull] AttributeDependency dependency,
			[NotNull] TextReader reader)
		{
			Assert.ArgumentNotNull(dependency, nameof(dependency));
			Assert.ArgumentNotNull(reader, nameof(reader));

			const char delimiter = ',';
			Assert.True(_delimiters.IndexOf(delimiter) >= 0,
			            "delimiter must be one of: {0}", _delimiters);

			dependency.AttributeValueMappings.Clear();
			// drop all existing mappings (ensure HBM is cascading)

			int sourceCount = dependency.SourceAttributes.Count;
			int targetCount = dependency.TargetAttributes.Count;
			int columnCount = sourceCount + targetCount + 1;

			const char fieldSeparator = ';';
			using (var csv = new CsvReader(reader, fieldSeparator))
			{
				csv.SkipBlankLines = true;
				csv.SkipCommentLines = true;

				// Skip first (non-comment) line which declares row headers:
				if (! csv.ReadRecord())
				{
					throw new Exception("Need at least two records (but found none)");
				}

				try
				{
					while (csv.ReadRecord())
					{
						IList<string> values = csv.Values;

						if (values.Count != columnCount)
						{
							throw new FormatException(
								string.Format("Expect {0} fields but found {1}", columnCount,
								              values.Count));
						}

						var sourceValues = new List<object>();
						var targetValues = new List<object>();
						string description = string.Empty;

						for (var i = 0; i < values.Count; i++)
						{
							if (i < sourceCount)
							{
								string value = string.IsNullOrEmpty(values[i]) ? null : values[i];
								FieldType fieldType = dependency.SourceAttributes[i].FieldType;
								object typedValue = Convert(value, fieldType, null);
								sourceValues.Add(typedValue);
							}
							else if (i < sourceCount + targetCount)
							{
								string value = string.IsNullOrEmpty(values[i]) ? null : values[i];
								FieldType fieldType =
									dependency.TargetAttributes[i - sourceCount].FieldType;
								object typedValue = Convert(value, fieldType, null);
								targetValues.Add(typedValue);
							}
							else
							{
								description = values[i];
							}
						}

						var sb = new StringBuilder();
						string sourceText = Format(sourceValues, sb);
						string targetText = Format(targetValues, sb);

						var mapping = new AttributeValueMapping(sourceText, targetText,
						                                        description);

						dependency.AttributeValueMappings.Add(mapping);
					}
				}
				catch (Exception ex)
				{
					throw new FormatException(
						string.Format("{0} (line {1})", ex.Message, csv.LineNumber - 1), ex);
				}
			}
		}

		#endregion

		[NotNull]
		public static string Format([NotNull] IList<object> values,
		                            [CanBeNull] StringBuilder sb = null)
		{
			Assert.ArgumentNotNull(values, nameof(values));

			const char delimiter = ',';
			Assert.True(_delimiters.IndexOf(delimiter) >= 0,
			            "delimiter must be one of: {0}", _delimiters);

			if (sb == null)
			{
				sb = new StringBuilder();
			}
			else
			{
				sb.Length = 0; // clear
			}

			foreach (object value in values)
			{
				if (sb.Length > 0)
				{
					sb.Append(delimiter);
				}

				TypeCode typeCode = System.Convert.GetTypeCode(value);

				switch (typeCode)
				{
					case TypeCode.Empty: // value is null
					case TypeCode.DBNull: // value is DBNull
						sb.Append("null");
						break;

					case TypeCode.Boolean: // false or true
						sb.Append((bool) value);
						break;

					case TypeCode.Char: // interpret as single-char string
						sb.Append('"');
						Escape((char) value, sb);
						sb.Append('"');
						break;

					case TypeCode.String:
						sb.Append('"');
						Escape((string) value, sb);
						sb.Append('"');
						break;

					case TypeCode.SByte:
					case TypeCode.Byte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
					case TypeCode.Single:
					case TypeCode.Double:
						sb.Append(System.Convert.ToString(value,
						                                  CultureInfo.InvariantCulture));
						break;

					case TypeCode.Decimal:
					case TypeCode.DateTime:
					case TypeCode.Object: // none of the other type codes
						if (value == Wildcard.Value)
						{
							sb.Append(Wildcard.ValueString);
							break;
						}

						throw new InvalidOperationException(
							string.Format("Type not supported: {0}", typeCode));

					default:
						throw new Exception(string.Format(
							                    "Bug: Unknown TypeCode: {0}", typeCode));
				}
			}

			return sb.ToString();
		}

		/// <summary>
		/// See <see cref="ParseValues(string,System.Collections.Generic.IList{object})"/>
		/// </summary>
		public static List<object> ParseValues([NotNull] string text)
		{
			var values = new List<object>();
			ParseValues(text, values);
			values.TrimExcess();
			return values;
		}

		/// <summary>Parse the given text into a list of values</summary>
		/// <returns>Number of values parsed</returns>
		/// <remarks>Grammar:<code>
		/// ValueList => Value { [';'|','] Value }
		///  Value => Integer | Float | String | Symbol
		///   Integer => [+|-][0-9]+        # type: Int32 or Int64
		///   Real => [+|-][0-9]*[.[0-9]+]  # type: Double
		///   String => '"' { Char } '"'    # type: String
		///    Char: any char except '"' and '\' and control characters
		///   Symbol => 'null' | 'false' | 'true'  # type: null or Boolean; not case sensitive
		/// </code></remarks>
		public static int ParseValues([NotNull] string text, [NotNull] IList<object> values)
		{
			Assert.ArgumentNotNull(text, nameof(text));
			Assert.ArgumentNotNull(values, nameof(values));

			var state = new ParseState(text);

			state.Advance();

			int count = ParseValues(state, values);

			if (! state.IsEnd)
			{
				throw SyntaxError(state.Index, "Expected end-of-text");
			}

			return count;
		}

		/// <summary>
		/// Returns true if two lists of values are considered equal
		/// (based on <see cref="Compare"/>), respecting <see cref="Wildcard"/>
		/// </summary>
		public static bool ValuesMatch([NotNull] IList<object> a, [NotNull] IList<object> b)
		{
			Assert.ArgumentNotNull(a, nameof(a));
			Assert.ArgumentNotNull(b, nameof(b));

			if (a.Count != b.Count)
			{
				return false;
			}

			int count = a.Count;
			for (var i = 0; i < count; i++)
			{
				object aa = a[i];
				object bb = b[i];

				if (aa == Wildcard.Value || bb == Wildcard.Value)
				{
					continue;
				}

				if (Compare(aa, bb) != 0)
				{
					return false;
				}
			}

			return true;
		}

		public static int GetAttributeIndex(AttributeDependency ad, string fieldName,
		                                    out bool source)
		{
			// TODO Instead of out bool source, consider an enum { None, Source, Target }
			var comparison = StringComparison.Ordinal;

			int index = GetAttributeIndex(ad.SourceAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = true;
				return index;
			}

			index = GetAttributeIndex(ad.TargetAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = false;
				return index;
			}

			comparison = StringComparison.OrdinalIgnoreCase;

			index = GetAttributeIndex(ad.SourceAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = true;
				return index;
			}

			index = GetAttributeIndex(ad.TargetAttributes, fieldName, comparison);
			if (index >= 0)
			{
				source = false;
				return index;
			}

			source = false; // ignored
			return -1; // not found
		}

		private static int GetAttributeIndex(IList<Attribute> attributes, string name,
		                                     StringComparison comparison)
		{
			int count = attributes.Count;

			for (var index = 0; index < count; index++)
			{
				if (attributes[index] is ObjectAttribute attribute &&
				    string.Equals(attribute.Name, name, comparison))
				{
					return index;
				}
			}

			return -1; // not found
		}

		#region Private methods

		#region Parser

		// Will be treated like white space in value lists:
		private const string _delimiters = ",;";

		private static int ParseValues(ParseState state, IList<object> values)
		{
			var valueCount = 0;

			while (! state.IsEnd)
			{
				values.Add(GetValue(state));
				state.Advance();
				valueCount += 1;
			}

			return valueCount;
		}

		private static object GetValue(ParseState state)
		{
			switch (state.CurrentToken)
			{
				case TokenType.Number:
				case TokenType.String:
					return state.TokenValue;

				case TokenType.Symbol:
					return GetSymbolValue(state);
			}

			throw SyntaxError(state.Index, "Expected value, got: {0}",
			                  state.CurrentToken);
		}

		private static object GetSymbolValue(ParseState state)
		{
			var symbol = (string) state.TokenValue;

			if (string.Equals(symbol, "null", StringComparison.OrdinalIgnoreCase))
			{
				return DBNull.Value;
			}

			if (string.Equals(symbol, "*"))
			{
				return Wildcard.Value;
			}

			if (string.Equals(symbol, "false", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			if (string.Equals(symbol, "true", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}

			throw SyntaxError(state.Index, "Unknown symbol: {0}", symbol);
		}

		private static Exception SyntaxError(int index, string format,
		                                     params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendFormat(format, args);
			sb.AppendFormat(" (near position {0})", index);
			return new FormatException(sb.ToString());
		}

		private class ParseState
		{
			private readonly string _text;

			private readonly StringBuilder _buffer;
			//private readonly StringComparison _comparison;

			private int _index;
			private TokenType _currentToken;
			private object _tokenValue;

			public ParseState(string text)
			{
				Assert.ArgumentNotNull(text, nameof(text));

				_text = text;
				_index = 0;
				_buffer = new StringBuilder();
				//_comparison = StringComparison.OrdinalIgnoreCase;
			}

			public void Advance()
			{
				_currentToken = NextToken(_text, ref _index, out _tokenValue, _buffer);
			}

			public int Index => _index;

			public TokenType CurrentToken => _currentToken;

			public object TokenValue => _tokenValue;

			//public void ExpectToken(TokenType expectedToken)
			//{
			//    if (_currentToken != expectedToken)
			//    {
			//        throw SyntaxError(Index, "Expected {0}, got {1}",
			//                          expectedToken, CurrentTokenString);
			//    }
			//}

			//public void ExpectSymbol(string expectedSymbol)
			//{
			//    if (_currentToken != TokenType.Symbol &&
			//        !string.Equals(_tokenValue as string, expectedSymbol, _comparison))
			//    {
			//        throw SyntaxError(Index, "Expected {0}, got {1}",
			//                          expectedSymbol, CurrentTokenString);
			//    }
			//}

			public bool IsEnd => _currentToken == TokenType.End;

			//public string CurrentTokenString
			//{
			//    get
			//    {
			//        switch (_currentToken)
			//        {
			//            case TokenType.Symbol:
			//                return (string)_tokenValue ?? "(null)";
			//            case TokenType.String:
			//                // Enclose in single quotes, use '' to escape apostrophes:
			//                var escaped = Convert.ToString(_tokenValue).Replace("'", "''");
			//                return string.Concat("'", escaped, "'");
			//            case TokenType.Number:
			//                return string.Format("{0}", _tokenValue);

			//            case TokenType.End:
			//                return "(end-of-input)";
			//        }

			//        throw new ArgumentOutOfRangeException();
			//    }
			//}

			public override string ToString()
			{
				return _tokenValue == null
					       ? string.Format("Token = {0}", _currentToken)
					       : string.Format("Token = {0}, Value = {1}", _currentToken,
					                       _tokenValue);
			}
		}

		private enum TokenType
		{
			Symbol,
			String,
			Number,
			End
		}

		#endregion

		#region Tokenizer

		private static TokenType NextToken(string text, ref int index,
		                                   out object value, StringBuilder sb)
		{
			// String: "abc\"def"
			// Number: +123, 0.345, -.567, 78.9
			// Symbol: abc AND Hello null
			// Other:  , ; =>

			while (index < text.Length && IsDelimiter(text, index))
			{
				index += 1; // skip white space and delimiters
			}

			if (index >= text.Length)
			{
				value = null;
				return TokenType.End;
			}

			char cc = text[index];

			if (char.IsLetter(cc))
			{
				value = ParseSymbol(text, ref index, sb);
				return TokenType.Symbol;
			}

			if (cc == '"')
			{
				value = ParseString(text, ref index, sb);
				return TokenType.String;
			}

			if (IsStartNumber(text, index))
			{
				value = ParseNumber(text, ref index, sb);
				return TokenType.Number;
			}

			//if (Char.IsDigit(cc))
			//{
			//    object number;
			//    if (TryParseNumber(text, ref index, out number))
			//    {
			//        value = number;
			//        return TokenType.Number;
			//    }

			//    throw SyntaxError(index, "Invalid number");
			//}

			//if (cc == '+' || cc == '-' || cc == '.')
			//{
			//    object number;
			//    if (TryParseNumber(text, ref index, out number))
			//    {
			//        value = number;
			//        return TokenType.Number;
			//    }
			//}

			//if (_delimiters.IndexOf(cc) >= 0)
			//{
			//    index += 1;
			//    value = null;
			//    return TokenType.Delimiter;
			//}

			if (cc == '*')
			{
				index += 1;
				value = Wildcard.ValueString;
				return TokenType.Symbol;
			}

			//if (cc == '=')
			//{
			//    if (Peek(text, index+1) == '>')
			//    {
			//        index += 2;
			//        value = null;
			//        return TokenType.MapsTo;
			//    }
			//}

			throw SyntaxError(index, "Unexpected input");
		}

		private static char Peek(string text, int index)
		{
			return index < text.Length
				       ? text[index]
				       : '\0';
		}

		private static bool IsDelimiter(string text, int index)
		{
			char cc = Peek(text, index);

			return char.IsWhiteSpace(cc) || _delimiters.IndexOf(cc) >= 0;
		}

		private static bool IsStartNumber(string text, int index)
		{
			char cc = Peek(text, index);

			if (cc == '+' || cc == '-')
			{
				// There may be a leading plus or minus:
				index += 1;
				cc = Peek(text, index);
			}

			if (cc == '.')
			{
				// Some like to write 0.3 as .3 (and -0.3 as -.3):
				index += 1;
				cc = Peek(text, index);
			}

			return char.IsDigit(cc);
		}

		private static object ParseNumber(string text, ref int index, StringBuilder sb)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(IsStartNumber(text, index), "Bug");

			sb.Length = 0; // clear
			char cc = text[index];
			var integer = true;

			// Optional sign:
			if (cc == '+' || cc == '-')
			{
				sb.Append(cc);
				index += 1;
			}

			Assert.True(index < text.Length, "Bug");

			// Integer part:
			while (index < text.Length && char.IsDigit(text, index))
			{
				sb.Append(text[index++]);
			}

			// Fraction part:
			if (index < text.Length && text[index] == '.')
			{
				sb.Append('.');
				index += 1;

				if (! char.IsDigit(Peek(text, index)))
				{
					throw SyntaxError(index,
					                  "Invalid number: expect digit after decimal point");
				}

				while (index < text.Length && char.IsDigit(text, index))
				{
					sb.Append(text[index++]);
				}

				integer = false;
			}

			// Exponent part:
			if (index < text.Length && (text[index] == 'e' || text[index] == 'E'))
			{
				sb.Append('e');
				index += 1;

				if (! char.IsDigit(Peek(text, index)))
				{
					throw SyntaxError(index,
					                  "Invalid number: exponent needs at least one digit");
				}

				while (index < text.Length && char.IsDigit(text, index))
				{
					sb.Append(text[index++]);
				}

				integer = false;
			}

			string s = sb.ToString();
			CultureInfo invariant = CultureInfo.InvariantCulture;

			if (integer)
			{
				long value = long.Parse(s, NumberStyles.Integer, invariant);
				if (int.MinValue <= value && value <= int.MaxValue)
				{
					return (int) value;
				}

				return value;
			}

			return double.Parse(s, NumberStyles.Float, invariant);
		}

		private static string ParseString(string text, ref int index, StringBuilder sb)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(text[index] == '\"', "Bug");

			sb.Length = 0; // clear
			int anchor = index;
			index += 1; // skip opening double quote

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc == '"') // closing double quote
				{
					return sb.ToString();
				}

				if (cc == '\\')
				{
					if (index < text.Length)
					{
						cc = text[index++];
						const string escapes = "0\0a\ab\bf\fn\nr\rt\tv\v";
						int escapeIndex = escapes.IndexOf(cc);
						if (escapeIndex >= 0)
						{
							sb.Append(escapes[escapeIndex + 1]);
						}
						else if (cc == 'u') // "foo\uXXXXbar"
						{
							uint u = 0;
							const string hex = "0123456789ABCDEF";
							int limit = index + 4;
							while (index < limit && index < text.Length)
							{
								cc = char.ToUpperInvariant(text[index++]);
								int h = hex.IndexOf(cc);
								if (h >= 0)
								{
									u *= 16;
									u += (uint) h;
								}
								else
								{
									throw SyntaxError(index,
									                  "Invalid \\u#### escape (need exactly four hex digits)");
								}
							}

							if (index < limit)
							{
								throw SyntaxError(index,
								                  "Invalid \\u#### escape (need exactly four hex digits)");
							}

							sb.Append((char) u);
						}
						else
						{
							throw SyntaxError(index,
							                  "Unknown escape in string literal");
						}
					}
					else
					{
						throw SyntaxError(index, "Lonely backslash in string literal");
					}
				}
				else
				{
					sb.Append(cc);
				}
			}

			throw SyntaxError(anchor, "Unterminated string");
		}

		private static string ParseSymbol(string text, ref int index, StringBuilder sb)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(char.IsLetter(text, index), "Bug");

			sb.Length = 0; // clear
			while (index < text.Length && char.IsLetterOrDigit(text, index))
			{
				sb.Append(text[index++]);
			}

			return sb.ToString();
		}

		#endregion

		#region Move to StringUtils?

		private static void Escape(string s, StringBuilder sb)
		{
			if (! string.IsNullOrEmpty(s))
			{
				foreach (char c in s)
				{
					Escape(c, sb);
				}
			}
		}

		private static void Escape(char c, StringBuilder sb)
		{
			const char backslash = '\\';

			switch (c)
			{
				case '"':
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\\':
					sb.Append(backslash);
					sb.Append(backslash);
					break;

				case '\0': // null
					sb.Append(backslash);
					sb.Append('0');
					break;

				case '\a': // alarm
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\b': // backspace
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\f': // formfeed
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\n': // newline
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\r': // carriage return
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\t': // horizontal tab
					sb.Append(backslash);
					sb.Append('"');
					break;

				case '\v': // vertical tab
					sb.Append(backslash);
					sb.Append('"');
					break;

				default:
					if (char.IsControl(c))
					{
						sb.Append(backslash);
						sb.Append('u');
						sb.Append(HexChar((c >> 12) & 0xf));
						sb.Append(HexChar((c >> 8) & 0xf));
						sb.Append(HexChar((c >> 4) & 0xf));
						sb.Append(HexChar(c & 0xf));
					}
					else
					{
						sb.Append(c);
					}

					break;
			}
		}

		private static char HexChar(int value)
		{
			if (0 <= value && value < 16)
			{
				return "0123456789abcdef"[value];
			}

			throw new ArgumentException("value out of range");
		}

		#endregion

		#endregion
	}

	public sealed class Wildcard
	{
		public static readonly Wildcard Value = new Wildcard();
		public static readonly string ValueString = "*";

		// Private constructor to enforce singleton behaviour
		private Wildcard() { }

		public override string ToString()
		{
			return ValueString;
		}
	}
}
