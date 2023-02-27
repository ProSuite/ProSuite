using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Utils
{
	/// <summary>
	/// Match features against the SearchCondition of a SQL WhereClause.
	/// Supports a subset of what may go into the SearchCondition
	/// of a SQL WhereClause according to this grammar:
	/// <code>
	/// SearchCondition => BooleanTerm { 'OR' BooleanTerm }
	///  BooleanTerm => BooleanFactor { 'AND' BooleanFactor }
	///   BooleanFactor => [ 'NOT' ] BooleanPrimary
	///    BooleanPrimary => Predicate | '(' SearchCondition ')'
	/// 
	/// Predicate => ComparisonPredicate | InPredicate | NullPredicate
	///  ComparisonPredicate => Expression CompOp Expression
	///   CompOp => '=' | '&lt;&gt;' | '&lt;' | '&gt;' | '&lt;=' | '&gt;='
	///  InPredicate => Expression [ 'NOT' ] 'IN' '(' Expression { ',' Expression } ')'
	///  NullPredicate => Expression 'IS' [ 'NOT' ] 'NULL'
	/// 
	/// Expression => ColumnName | Literal            for now: a simple shortcut
	/// Expression => Term { ( '+' | '-' ) Term }     for later: allow arithmetic
	///  Term => Factor { ( '*' | '/' ) Factor }
	///   Factor => [ '+' | '-' ] Primary
	///    Primary => Identifier | Literal | '(' Expression ')'
	/// 
	/// Identifier => Letter { Digit | Letter }
	/// Literal => 'FALSE' | 'TRUE' | Number | String
	///  Number => [ '+' | '-' ] [ Digit { Digits } ] [ '.' ] Digit { Digits }
	/// </code>
	/// Example: "A = 3 AND NOT B IN (2,4,6) OR A = 4 AND B IS NULL"<br/>
	/// Meaning: (OR (AND (= A 3) (NOT (IN B 2 4 6))) (AND (= A 4) (NULL B)))
	/// <para/>
	/// For a complete SQL grammar, see:
	/// http://savage.net.au/SQL/sql-99.bnf.html#search+condition
	/// <para/>
	/// For the minimum ODBC subset, see:
	/// http://msdn.microsoft.com/en-us/library/windows/desktop/ms711725%28v=vs.85%29.aspx
	/// </summary>
	public class WhereClauseMatcher
	{
		private readonly Program _program;
		private readonly Stack<object> _stack;
		//private readonly IDictionary<string, int> _indexCache;
		//private readonly IDictionary<string, object> _valueCache;

		public WhereClauseMatcher([NotNull] string clause)
		{
			Clause = clause;
			_program = Parse(clause);

			_stack = new Stack<object>();
			//_indexCache = new Dictionary<string, int>();
			//_valueCache = new Dictionary<string, object>();
		}

		public string Clause { get; }

		/// <summary>
		/// Return true iff the where clause refers to no other
		/// fields than those listed in <paramref name="fields"/>.
		/// Optionally, append an error message that lists undefined fields.
		/// </summary>
		public bool Validate(IEnumerable<string> fields, StringBuilder messages = null)
		{
			var stack = new Stack<object>();
			var values = new NameValidator(fields ?? Enumerable.Empty<string>());

			Execute(_program, values, stack);

			return values.Validate(messages);
		}

		public bool Match([NotNull] INamedValues values)
		{
			Assert.ArgumentNotNull(values, nameof(values));

			_stack.Clear();
			//_valueCache.Clear();
			// do NOT clear the index cache

			Execute(_program, values, _stack);

			Assert.AreEqual(1, _stack.Count,
			                "Bug: Stack has {0} item(s), expected 1", _stack.Count);

			object result = _stack.Pop();

			Assert.True(result is bool, "Bug: Result on stack is not of type bool");

			return (bool) result;
		}

		public void DumpProgram(StringBuilder sb)
		{
			Assert.ArgumentNotNull(sb, nameof(sb));

			sb.Append('[');

			int count = _program.Count;
			for (int i = 0; i < count; i++)
			{
				if (i > 0) sb.Append(' ');

				object item = _program[i];

				if (item is Instruction)
				{
					sb.Append(item);
				}
				else if (item is string s)
				{
					int len = s.Length;

					sb.Append('"');

					for (int j = 0; j < len; j++)
					{
						char c = s[j];
						if (c == '"') sb.Append('\\');
						sb.Append(c);
						// TODO Escape non-printing chars...
					}

					sb.Append('"');
				}
				else if (item == null)
				{
					sb.Append("null");
				}
				else
				{
					sb.Append(item);
				}
			}

			sb.Append(']');
		}

		public override string ToString()
		{
			return Clause;
		}

		#region Filter clause parser

		private static Program Parse(string clause)
		{
			var program = new Program();
			var state = new ParseState(clause);

			state.Advance();

			if (state.IsEnd)
			{
				// Special case: empty clause is always true:
				program.EmitValue(true);
				return program;
			}

			ParseClause(state, program);

			if (! state.IsEnd)
			{
				throw SyntaxError(state.Index, "Expected end of input");
			}

			program.SqueezeNops();

			return program;
		}

		private static void ParseClause(ParseState state, Program program)
		{
			bool singleton = true;
			int pc = program.EmitNop();

			ParseTerm(state, program);

			while (state.IsSymbol("OR"))
			{
				singleton = false;
				state.Advance();
				ParseTerm(state, program);
			}

			if (! singleton)
			{
				program.Emit(pc, Instruction.Mark);
				program.Emit(Instruction.Or);
			}
		}

		private static void ParseTerm(ParseState state, Program program)
		{
			bool singleton = true;
			int pc = program.EmitNop();

			ParseFactor(state, program);

			while (state.IsSymbol("AND"))
			{
				singleton = false;
				state.Advance();
				ParseFactor(state, program);
			}

			if (! singleton)
			{
				program.Emit(pc, Instruction.Mark);
				program.Emit(Instruction.And);
			}
		}

		private static void ParseFactor(ParseState state, Program program)
		{
			bool negate = false;

			if (state.IsSymbol("NOT"))
			{
				negate = true;
				state.Advance();
			}

			ParsePrimary(state, program);

			if (negate)
			{
				program.Emit(Instruction.Neg);
			}
		}

		private static void ParsePrimary(ParseState state, Program program)
		{
			if (state.CurrentToken == TokenType.LParen)
			{
				state.Advance();

				ParseClause(state, program);

				state.ExpectToken(TokenType.RParen);
				state.Advance();
			}
			else
			{
				ParseExpression(state, program);

				if (state.IsCompOp) // expr op expr
				{
					var inst = GetInstruction(state.CurrentToken);

					state.Advance();

					ParseExpression(state, program);

					program.Emit(inst);
				}
				else if (state.IsSymbol("NOT")) // expr NOT IN (expr, expr, etc)
				{
					state.Advance();
					state.ExpectSymbol("IN");

					ParseInValueList(state, program, true);
				}
				else if (state.IsSymbol("IN")) // expr IN (expr, expr, etc)
				{
					ParseInValueList(state, program, false);
				}
				else if (state.IsSymbol("IS")) // expr IS [NOT] NULL
				{
					state.Advance();

					bool negate = false;

					if (state.IsSymbol("NOT"))
					{
						negate = true;
						state.Advance();
					}

					state.ExpectSymbol("NULL");
					state.Advance();

					program.Emit(negate ? Instruction.NotNull : Instruction.IsNull);
				}
				else
				{
					throw SyntaxError(state.Index, "Expected expr, IN, or IS; got {0}",
					                  state.CurrentTokenString);
				}
			}
		}

		private static void ParseInValueList(ParseState state, Program program, bool negate)
		{
			// Notice:
			// "x IN (a, b, c)" is equivalent to "x = a OR x = b OR x = c";
			// we compile this to: [expr Mark Exch {Dup expr IsEq Exch} Pop Or]
			// "x NOT IN (a,b,c)" is equivalent to "x <> a AND x <> b AND x <> c";
			// we compile this to: [expr Mark Exch {Dup expr NotEq Exch} Pop And]

			state.Advance(); // eat the "IN" token

			state.ExpectToken(TokenType.LParen);
			state.Advance();

			program.Emit(Instruction.Mark);
			program.Emit(Instruction.Exch);

			program.Emit(Instruction.Dup);
			ParseExpression(state, program);
			program.Emit(negate ? Instruction.NotEq : Instruction.IsEq);
			program.Emit(Instruction.Exch);

			while (state.IsToken(TokenType.Comma))
			{
				state.Advance();

				program.Emit(Instruction.Dup);
				ParseExpression(state, program);
				program.Emit(negate ? Instruction.NotEq : Instruction.IsEq);
				program.Emit(Instruction.Exch);
			}

			state.ExpectToken(TokenType.RParen);
			state.Advance();

			program.Emit(Instruction.Pop);
			program.Emit(negate ? Instruction.And : Instruction.Or);
		}

		private static void ParseExpression(ParseState state, Program program)
		{
			// TODO Allow richer expressions: arithmetic

			switch (state.CurrentToken)
			{
				case TokenType.Symbol:
					if (state.IsSymbol("FALSE"))
					{
						program.EmitValue(false);
					}
					else if (state.IsSymbol("TRUE"))
					{
						program.EmitValue(true);
					}
					else if (state.IsSymbol("NULL"))
					{
						program.EmitValue(null);
					}
					else // column reference to be looked up
					{
						program.EmitValue(state.TokenValue);
						program.Emit(Instruction.Get);
					}

					state.Advance();
					break;

				case TokenType.String:
				case TokenType.Number:
					program.EmitValue(state.TokenValue);
					state.Advance();
					break;

				default:
					throw SyntaxError(state.Index, "Expected Symbol, String, or Number; got {0}",
					                  state.CurrentTokenString);
			}
		}

		private static Instruction GetInstruction(TokenType token)
		{
			switch (token)
			{
				case TokenType.Eq:
					return Instruction.IsEq;
				case TokenType.Neq:
					return Instruction.NotEq;
				case TokenType.Lt:
					return Instruction.Lt;
				case TokenType.Le:
					return Instruction.Le;
				case TokenType.Gt:
					return Instruction.Gt;
				case TokenType.Ge:
					return Instruction.Ge;
			}

			throw new AssertionException(
				string.Format("Bug: No instruction for token: {0}", token));
		}

		private class ParseState
		{
			private readonly string _text;
			private readonly StringBuilder _buffer;
			private readonly StringComparison _comparison;

			private int _index;
			private TokenType _currentToken;
			private object _tokenValue;

			public ParseState(string text)
			{
				_text = text;
				_index = 0;
				_buffer = new StringBuilder();
				_comparison = StringComparison.OrdinalIgnoreCase;
			}

			public void Advance()
			{
				_currentToken = NextToken(_text, ref _index, out _tokenValue, _buffer);
			}

			public int Index => _index;

			public TokenType CurrentToken => _currentToken;

			public object TokenValue => _tokenValue;

			public void ExpectToken(TokenType expectedToken)
			{
				if (_currentToken != expectedToken)
				{
					throw SyntaxError(Index, "Expected {0}, got {1}",
					                  expectedToken, CurrentTokenString);
				}
			}

			public void ExpectSymbol(string expectedSymbol)
			{
				if (_currentToken != TokenType.Symbol &&
				    ! string.Equals(_tokenValue as string, expectedSymbol, _comparison))
				{
					throw SyntaxError(Index, "Expected {0}, got {1}",
					                  expectedSymbol, CurrentTokenString);
				}
			}

			public bool IsToken(TokenType token)
			{
				return _currentToken == token;
			}

			public bool IsSymbol(string symbol)
			{
				return _currentToken == TokenType.Symbol &&
				       string.Equals(_tokenValue as string, symbol, _comparison);
			}

			public bool IsCompOp
			{
				get
				{
					switch (_currentToken)
					{
						case TokenType.Eq:
						case TokenType.Neq:
						case TokenType.Lt:
						case TokenType.Le:
						case TokenType.Gt:
						case TokenType.Ge:
							return true;
					}

					return false;
				}
			}

			public bool IsEnd => _currentToken == TokenType.End;

			public string CurrentTokenString
			{
				get
				{
					switch (_currentToken)
					{
						case TokenType.Symbol:
							return (string) _tokenValue ?? "(null)";
						case TokenType.String:
							// Enclose in single quotes, use '' to escape apostrophes:
							var escaped = Convert.ToString(_tokenValue).Replace("'", "''");
							return string.Concat("'", escaped, "'");
						case TokenType.Number:
							return string.Format("{0}", _tokenValue);

						case TokenType.Eq:
							return "=";
						case TokenType.Neq:
							return "<>";
						case TokenType.Lt:
							return "<";
						case TokenType.Le:
							return "<=";
						case TokenType.Gt:
							return ">";
						case TokenType.Ge:
							return ">=";

						case TokenType.LParen:
							return "(";
						case TokenType.RParen:
							return ")";
						case TokenType.Comma:
							return ",";

						case TokenType.End:
							return "(end-of-input)";
					}

					throw new ArgumentOutOfRangeException();
				}
			}

			public override string ToString()
			{
				return _tokenValue == null
					       ? $"Token = {_currentToken}"
					       : $"Token = {_currentToken}, Value = {_tokenValue}";
			}
		}

		#region Tokenizer

		private static TokenType NextToken(string text, ref int index, out object value,
		                                   StringBuilder sb)
		{
			// String: 'abc''def'
			// Number: +123, 0.345, -.567, 78.9
			// Symbol: abc AND Hello null
			// Other: = <> < > <= >=

			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1; // skip white space
			}

			if (index >= text.Length)
			{
				value = null;
				return TokenType.End;
			}

			char cc = text[index];

			if (char.IsLetter(cc) || cc == '_')
			{
				value = ParseSymbol(text, ref index, sb);
				return TokenType.Symbol;
			}

			if (cc == '\'')
			{
				value = ParseString(text, ref index, sb);
				return TokenType.String;
			}

			if (char.IsDigit(cc))
			{
				if (TryParseNumber(text, ref index, out object number))
				{
					value = number;
					return TokenType.Number;
				}

				throw SyntaxError(index, "Invalid number");
			}

			if (cc == '=')
			{
				index += 1;
				value = null;
				return TokenType.Eq;
			}

			if (cc == '<')
			{
				index += 1;
				value = null;

				cc = Peek(text, index);
				switch (cc)
				{
					case '>':
						index += 1;
						return TokenType.Neq;
					case '=':
						index += 1;
						return TokenType.Le;
					default:
						return TokenType.Lt;
				}
			}

			if (cc == '>')
			{
				index += 1;
				value = null;

				cc = Peek(text, index);
				switch (cc)
				{
					case '=':
						index += 1;
						return TokenType.Ge;
					default:
						return TokenType.Gt;
				}
			}

			if (cc == '+' || cc == '-' || cc == '.')
			{
				if (TryParseNumber(text, ref index, out object number))
				{
					value = number;
					return TokenType.Number;
				}
			}

			if (cc == '(')
			{
				index += 1;
				value = null;
				return TokenType.LParen;
			}

			if (cc == ')')
			{
				index += 1;
				value = null;
				return TokenType.RParen;
			}

			if (cc == ',')
			{
				index += 1;
				value = null;
				return TokenType.Comma;
			}

			throw SyntaxError(index, "Unexpected input");
		}

		/// <summery>
		/// Try parsing a number from text[index] in one of three formats:
		/// (1) [sign] whole, (2) [sign] whole dot fractional, (3) [sign] dot fractional
		/// </summery>
		/// <remarks>Update <i>index</i> ONLY if number is successfully parsed</remarks>
		private static bool TryParseNumber(string text, ref int index, out object value)
		{
			int sign = 1;
			int i = index;
			char cc = text[i];

			switch (cc)
			{
				case '+':
					i += 1;
					break;
				case '-':
					i += 1;
					sign = -1;
					break;
			}

			if (i < text.Length && char.IsDigit(text, i))
			{
				long whole = ParseUnsigned(text, ref i);

				if (i < text.Length && text[i] == '.') // there's a fractional part
				{
					if (++i < text.Length && char.IsDigit(text, i))
					{
						int before = i;
						double fraction = ParseUnsigned(text, ref i);
						fraction /= Math.Pow(10, i - before);

						index = i;
						value = sign * (whole + fraction);
						return true; // ok, "[sign] whole dot fractional"
					}

					value = null;
					return false; // "[sign] whole dot" is not a valid number
				}

				index = i;
				value = sign * whole;
				return true; // ok, "[sign] whole"
			}

			if (i < text.Length && text[i] == '.') // there's ONLY a fractional part
			{
				if (++i < text.Length && char.IsDigit(text, i))
				{
					int before = i;
					double fraction = ParseUnsigned(text, ref i);
					fraction /= Math.Pow(10, i - before);

					index = i;
					value = sign * fraction;
					return true;
				}

				// Not a number: no digit(s) follow decimal dot
				value = null;
				return false; // "[sign] dot" is not a valid number
			}

			value = null;
			return false; // "[sign]" is not a valid number
		}

		private static long ParseUnsigned(string text, ref int index)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(char.IsDigit(text, index), "Bug");

			long number = 0;
			while (index < text.Length && char.IsDigit(text, index))
			{
				char cc = text[index++];
				int i = "0123456789".IndexOf(cc);
				Assert.True(0 <= i && i <= 9, "Bug");
				number *= 10;
				number += i;
			}

			return number;
		}

		private static string ParseString(string text, ref int index, StringBuilder sb)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(text[index] == '\'', "Bug");

			sb.Length = 0; // clear
			int anchor = index;
			index += 1; // skip opening apostrophe

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc == '\'')
				{
					if (index < text.Length && text[index] == '\'')
					{
						sb.Append("'"); // un-escape
						index += 1; // skip 2nd apostrophe
					}
					else
					{
						return sb.ToString();
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
			Assert.True(char.IsLetter(text, index) || text[index] == '_', "Bug");

			sb.Length = 0; // clear
			while (index < text.Length && (char.IsLetterOrDigit(text, index) || text[index] == '_'))
			{
				sb.Append(text[index++]);
			}

			return sb.ToString();
		}

		private static char Peek(string text, int index)
		{
			return index < text.Length
				       ? text[index]
				       : '\0';
		}

		#endregion

		private static Exception SyntaxError(int index, string format, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendFormat(format, args);
			sb.AppendFormat(" (near position {0})", index);
			return new FormatException(sb.ToString());
		}

		private enum TokenType
		{
			Symbol,
			String,
			Number,
			Eq,
			Neq,
			Lt,
			Le,
			Gt,
			Ge,
			LParen,
			RParen,
			Comma,
			End
		}

		#endregion

		#region Virtual engine

		private static void Execute(Program program, INamedValues values, Stack<object> stack)
		{
			int count = program.Count;
			for (int i = 0; i < count; i++)
			{
				object item = program[i];

				if (item is Instruction instruction)
				{
					object value;
					object other;
					object result;

					switch (instruction)
					{
						case Instruction.Nop:
							break;

						case Instruction.Mark:
							stack.Push(Instruction.Mark);
							break;

						case Instruction.Dup:
							value = stack.Peek();
							stack.Push(value);
							break;
						case Instruction.Exch:
							value = stack.Pop();
							other = stack.Pop();
							stack.Push(value);
							stack.Push(other);
							break;
						case Instruction.Pop:
							stack.Pop();
							break;

						case Instruction.Get:
							value = stack.Pop();
							Assert.True(value is string, "Bug: Get: bad type on stack");
							value = values.GetValue((string) value);
							stack.Push(value);
							break;

						case Instruction.IsNull:
							value = stack.Pop();
							result = IsNull(value);
							stack.Push(result);
							break;
						case Instruction.NotNull:
							value = stack.Pop();
							result = ! IsNull(value);
							stack.Push(result);
							break;

						case Instruction.IsEq:
						case Instruction.NotEq:
						case Instruction.Gt:
						case Instruction.Ge:
						case Instruction.Lt:
						case Instruction.Le:
							value = stack.Pop();
							other = stack.Pop();
							// Notice the ordering of other and value!
							result = Compare(other, value, instruction);
							stack.Push(result);
							break;

						case Instruction.Neg:
							value = stack.Pop();
							Assert.True(value is bool, "Bug: Neg: bad type on stack");
							result = ! (bool) value;
							stack.Push(result);
							break;

						case Instruction.And:
							result = true;
							value = stack.Pop();
							while (! Equals(value, Instruction.Mark))
							{
								Assert.True(value is bool, "Bug: And: bad type on stack");
								if (! (bool) value) result = false;
								value = stack.Pop();
							}

							stack.Push(result);
							break;

						case Instruction.Or:
							result = false;
							value = stack.Pop();
							while (! Equals(value, Instruction.Mark))
							{
								Assert.True(value is bool, "Bug: Or: bad type on stack");
								if ((bool) value) result = true;
								value = stack.Pop();
							}

							stack.Push(result);
							break;

						default:
							throw new AssertionException(
								string.Format("Bug: invalid instruction: {0}", instruction));
					}
				}
				else
				{
					stack.Push(item);
				}
			}
		}

		private static bool IsNull(object value)
		{
			return value == null || Equals(value, DBNull.Value);
		}

		private static bool Compare(object a, object b, Instruction comp)
		{
			// Treat DBNull the same as null:
			if (a == DBNull.Value) a = null;
			if (b == DBNull.Value) b = null;

			if (a == null && b == null)
			{
				// Notice that NULL <> NULL is true!
				return comp == Instruction.NotEq;
			}

			if (a == null || b == null)
			{
				// All other comparisons with NULL are false
				return false;
			}

			CompareKind aKind = GetCompareKind(a);
			CompareKind bKind = GetCompareKind(b);

			if (aKind == CompareKind.Integer && bKind == CompareKind.Integer)
			{
				long aa = Convert.ToInt64(a, CultureInfo.InvariantCulture);
				long bb = Convert.ToInt64(b, CultureInfo.InvariantCulture);
				return Compare(aa, bb, comp);
			}

			if (aKind == CompareKind.Floating && bKind == CompareKind.Floating)
			{
				double aa = Convert.ToDouble(a, CultureInfo.InvariantCulture);
				double bb = Convert.ToDouble(b, CultureInfo.InvariantCulture);
				return Compare(aa, bb, comp);
			}

			if (aKind == CompareKind.Boolean && bKind == CompareKind.Boolean)
			{
				int aa = Convert.ToInt32(a, CultureInfo.InvariantCulture);
				int bb = Convert.ToInt32(b, CultureInfo.InvariantCulture);
				return Compare(aa, bb, comp);
			}

			if (aKind == CompareKind.String && bKind == CompareKind.String)
			{
				string aa = Convert.ToString(a, CultureInfo.InvariantCulture);
				string bb = Convert.ToString(b, CultureInfo.InvariantCulture);
				return Compare(aa, bb, comp);
			}

			if (aKind == CompareKind.DateTime && bKind == CompareKind.DateTime)
			{
				var aa = (DateTime) a;
				var bb = (DateTime) b;
				return Compare(aa.Ticks, bb.Ticks, comp);
			}

			// Roughly follow C# implicit numeric conversion rules:
			// http://msdn.microsoft.com/en-us/library/y5b434w4.aspx
			// Beware: this is a C# feature, not a .NET feature!
			// Therefore, for example, "1.0F is double" is false and
			// so is typeof(double).IsAssignableFrom(typeof(float)).

			if (aKind == CompareKind.Floating && bKind == CompareKind.Integer ||
			    aKind == CompareKind.Integer && bKind == CompareKind.Floating)
			{
				double aa = Convert.ToDouble(a);
				double bb = Convert.ToDouble(b);
				return Compare(aa, bb, comp);
			}

			throw new ApplicationException(
				string.Format("Cannot compare {0} and {1}",
				              a.GetType().Name, b.GetType().Name));

			// Alternatively: convert both to string and compare
			//string aString = Convert.ToString(a, CultureInfo.InvariantCulture);
			//string bString = Convert.ToString(b, CultureInfo.InvariantCulture);
			//return Compare(aString, bString, comp);
		}

		private enum CompareKind
		{
			Null,
			Boolean,
			Integer,
			Floating,
			String,
			DateTime,
			Object
		}

		private static CompareKind GetCompareKind(object value)
		{
			var typeCode = Convert.GetTypeCode(value);

			switch (typeCode)
			{
				case TypeCode.Empty:
				case TypeCode.DBNull:
					return CompareKind.Null;

				case TypeCode.Boolean:
					return CompareKind.Boolean;

				case TypeCode.Char:
				case TypeCode.SByte:
				case TypeCode.Byte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
					return CompareKind.Integer;

				case TypeCode.Single:
				case TypeCode.Double:
					return CompareKind.Floating;

				case TypeCode.DateTime:
					return CompareKind.DateTime;

				case TypeCode.String:
					return CompareKind.String;

				//case TypeCode.Decimal:
				//case TypeCode.Object:
				default:
					return CompareKind.Object;
			}
		}

		private static bool Compare(double a, double b, Instruction comp)
		{
			switch (comp)
			{
				case Instruction.IsEq:
					return Math.Abs(a - b) < double.Epsilon; // a == b
				case Instruction.NotEq:
					return Math.Abs(a - b) > double.Epsilon; // a != b
				case Instruction.Gt:
					return a > b;
				case Instruction.Ge:
					return a >= b;
				case Instruction.Lt:
					return a < b;
				case Instruction.Le:
					return a <= b;
			}

			throw new ArgumentOutOfRangeException(nameof(comp));
		}

		private static bool Compare(long a, long b, Instruction comp)
		{
			switch (comp)
			{
				case Instruction.IsEq:
					return a == b;
				case Instruction.NotEq:
					return a != b;
				case Instruction.Gt:
					return a > b;
				case Instruction.Ge:
					return a >= b;
				case Instruction.Lt:
					return a < b;
				case Instruction.Le:
					return a <= b;
			}

			throw new ArgumentOutOfRangeException(nameof(comp));
		}

		private static bool Compare(string a, string b, Instruction comp)
		{
			switch (comp)
			{
				case Instruction.IsEq:
					return string.CompareOrdinal(a, b) == 0;
				case Instruction.NotEq:
					return string.CompareOrdinal(a, b) != 0;
				case Instruction.Gt:
					return string.CompareOrdinal(a, b) > 0;
				case Instruction.Ge:
					return string.CompareOrdinal(a, b) >= 0;
				case Instruction.Lt:
					return string.CompareOrdinal(a, b) < 0;
				case Instruction.Le:
					return string.CompareOrdinal(a, b) <= 0;
			}

			throw new ArgumentOutOfRangeException(nameof(comp));
		}

		private enum Instruction
		{
			Nop,
			Mark,
			Dup,
			Exch,
			Pop,
			Get,

			IsNull,
			NotNull,
			IsEq,
			NotEq,
			Gt,
			Ge,
			Lt,
			Le,

			Neg,
			And,
			Or
		}

		private class Program : List<object>
		{
			public void EmitValue(object value)
			{
				Add(value);
			}

			public void Emit(Instruction instruction)
			{
				Add(instruction);
			}

			public void Emit(int pc, Instruction instruction)
			{
				this[pc] = instruction;
			}

			public int EmitNop()
			{
				int pc = Count;
				Add(Instruction.Nop);
				return pc;
			}

			public void SqueezeNops()
			{
				int r = 0, w = 0;
				int count = Count;
				while (r < count)
				{
					var item = this[r++];
					if (! Equals(item, Instruction.Nop))
					{
						this[w++] = item;
					}
				}

				for (r = Count - 1; r >= w; r--)
				{
					RemoveAt(r);
				}

				TrimExcess();
			}
		}

		private class NameValidator : INamedValues
		{
			private readonly HashSet<string> _fields;
			private readonly HashSet<string> _badNames;

			public NameValidator(IEnumerable<string> fields)
			{
				if (fields == null) throw new ArgumentNullException();
				_fields = new HashSet<string>(fields, StringComparer.OrdinalIgnoreCase);
				_badNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			}

			public bool Exists(string name)
			{
				return _fields.Contains(name);
			}

			public object GetValue(string name)
			{
				if (!_fields.Contains(name) && !_badNames.Contains(name))
				{
					_badNames.Add(name);
				}

				return DBNull.Value;
			}

			public bool Validate(StringBuilder messages)
			{
				if (_badNames.Count > 0)
				{
					if (messages != null)
					{
						var s = _badNames.Count == 1 ? "" : "s";
						messages.AppendFormat("Unknown field name{0}: ", s);
						messages.Append(string.Join(", ", _badNames.OrderBy(n => n)));
					}

					return false;
				}

				return true;
			}
		}

		#endregion
	}
}
