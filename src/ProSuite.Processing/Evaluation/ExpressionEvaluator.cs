using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// Compile an expression clause into an engine
	/// that can be evaluated in an environment to
	/// produce a value.
	/// </summary>
	public class ExpressionEvaluator
	{
		// TODO
		// Compile in two steps: parse into an AST (probably using
		// System.Linq.Expressions.Expression nodes); then compile
		// the tree. This allows more validation and optimization.
		//
		// Compile into a System.Reflection.Emit.DynamicMethod by
		// emitting IL opcodes? This would save our virtual engine, but:
		// the ve is not complicated, and: IL is strictly typed, whereas
		// our expression language is very loosely typed...

		private readonly EvaluatorEngine _engine;

		// The relational operators:
		private static readonly string[] _relops = {"=", "<>", "<", "<=", ">", ">="};

		private ExpressionEvaluator(EvaluatorEngine engine, string clause = null)
		{
			Clause = clause ?? string.Empty;
			_engine = engine;
		}

		public string Clause { get; }

		/// <summary>
		/// Create an evaluator for the expression in <paramref name="text"/>,
		/// which must be a complete expression according to the evaluator's
		/// syntax rules. It is an error if the <paramref name="text"/>
		/// contains extra characters before or after the expression
		/// (except for white space, which will be ignored).
		/// </summary>
		public static ExpressionEvaluator Create(string text, bool ignoreCase = true)
		{
			var comparison = ignoreCase
				                 ? StringComparison.OrdinalIgnoreCase
				                 : StringComparison.Ordinal;

			int length;
			var engine = Compile(text, 0, out length, comparison);

			if (length < text.Length)
			{
				throw SyntaxError(length, "Extra input at end of expression");
			}

			string clause = GetClause(text, 0, length);

			return new ExpressionEvaluator(engine, clause);
		}

		/// <summary>
		/// Create an evaluator for the expression clause starting at
		/// <paramref name="index"/> in <paramref name="text"/>.
		/// Characters in <paramref name="text"/> before <paramref name="index"/>
		/// and after the expression clause will be ignored. The parameter
		/// <paramref name="length"/> contains on return the number of characters
		/// that constitute the expression clause (including leading and trailing
		/// white space, if any).
		/// </summary>
		public static ExpressionEvaluator Create(string text, int index, out int length,
		                                         bool ignoreCase = true)
		{
			var comparison = ignoreCase
				                 ? StringComparison.OrdinalIgnoreCase
				                 : StringComparison.Ordinal;

			var engine = Compile(text, index, out length, comparison);

			string clause = GetClause(text, index, length);

			return new ExpressionEvaluator(engine, clause);
		}

		/// <summary>
		/// Create an evaluator that always evaluates to the given
		/// constant <paramref name="value"/>. This is useful to
		/// substitute default values where no expression is provided.
		/// </summary>
		/// <param name="value">The constant value (may be null).</param>
		/// <param name="clause">The value for the evaluator's <see cref="Clause"/>
		///  property (optional).</param>
		public static ExpressionEvaluator CreateConstant(string value, string clause = null)
		{
			var engine = new EvaluatorEngine();
			engine.EmitValue(value);
			engine.EmitCode(OpCode.End);
			engine.Commit();
			return new ExpressionEvaluator(engine, clause ?? ToString(value));
		}

		/// <summary>
		/// Create an evaluator that always evaluates to the given
		/// constant <paramref name="value"/>. This is useful to
		/// substitute default values where no expression is provided.
		/// </summary>
		/// <param name="value">The constant value.</param>
		/// <param name="clause">The value for the evaluator's <see cref="Clause"/>
		///  property (optional).</param>
		public static ExpressionEvaluator CreateConstant(double value, string clause = null)
		{
			var engine = new EvaluatorEngine();
			engine.EmitValue(value);
			engine.EmitCode(OpCode.End);
			engine.Commit();
			return new ExpressionEvaluator(engine, clause ?? ToString(value));
		}

		/// <summary>
		/// Create an evaluator that always evaluates to the given
		/// constant <paramref name="value"/>. This is useful to
		/// substitute default values where no expression is provided.
		/// </summary>
		/// <param name="value">The constant value.</param>
		/// <param name="clause">The value for the evaluator's <see cref="Clause"/>
		///  property (optional).</param>
		public static ExpressionEvaluator CreateConstant(bool value, string clause = null)
		{
			var engine = new EvaluatorEngine();
			engine.EmitValue(value);
			engine.EmitCode(OpCode.End);
			engine.Commit();
			return new ExpressionEvaluator(engine, clause ?? ToString(value));
		}

		/// <summary>
		/// Create an evaluator that always evaluates to the given
		/// constant <paramref name="value"/>. This is useful to
		/// substitute default values where no expression is provided.
		/// </summary>
		/// <param name="value">The constant value.</param>
		/// <param name="clause">The value for the evaluator's <see cref="Clause"/>
		///  property (optional).</param>
		public static ExpressionEvaluator CreateConstant(object value, string clause = null)
		{
			var engine = new EvaluatorEngine();
			engine.EmitValue(value);
			engine.EmitCode(OpCode.End);
			engine.Commit();
			return new ExpressionEvaluator(engine, clause ?? ToString(value));
		}

		private static string GetClause(string text, int index, int length)
		{
			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1;
				length -= 1;
			}

			while (length > 0 && char.IsWhiteSpace(text, index + length - 1))
			{
				length -= 1;
			}

			return text.Substring(index, length);
		}

		private static string ToString(object value)
		{
			var sb = new StringBuilder();
			EvaluatorEngine.FormatLiteral(value, sb);
			return sb.ToString();
		}

		/// <summary>
		/// Evaluate the expression passed to the constructor
		/// in the <paramref name="environment"/> given and
		/// return the resulting value.
		/// <para/>
		/// The <paramref name="environment"/> parameter is optional;
		/// if it is missing (ie, <c>null</c>), the expression will
		/// be evaluated in the empty environment.
		/// <para/>
		/// You may pass in a <paramref name="stack"/>. If present,
		/// it will be cleared and used for the evaluation. If not,
		/// a new stack will be allocated internally.
		/// </summary>
		/// <param name="environment">The evaluation environment (optional).</param>
		/// <param name="stack">The evaluation stack to (re)use (optional).</param>
		/// <returns>The value of the expression</returns>
		public object Evaluate([CanBeNull] IEvaluationEnvironment environment,
		                       [CanBeNull] Stack<object> stack = null)
		{
			if (environment == null)
			{
				environment = new NullEnvironment();
			}

			if (stack == null)
			{
				stack = new Stack<object>();
			}
			else
			{
				stack.Clear();
			}

			_engine.Execute(environment, stack);

			Assert.AreEqual(1, stack.Count,
			                "Oops, {0} items on evaluation stack; expect exactly one",
			                stack.Count);

			return stack.Pop();
		}

		/// <summary>
		/// Write a human-readable representation of the stack engine
		/// that has been generated for evaluating the expression.
		/// <para/>
		/// This is only useful for debugging the evaluator.
		/// </summary>
		/// <param name="writer">The writer (required).</param>
		public void DumpEngine([NotNull] TextWriter writer)
		{
			_engine.Dump(writer);
		}

		public override string ToString()
		{
			return Clause;
		}

		#region Expression Parser

		private class ParseState
		{
			private readonly string _text;
			private int _index;
			private readonly StringBuilder _buffer;

			public ParseState(string text, int index, EvaluatorEngine target,
			                  StringComparison comparison)
			{
				_text = text ?? string.Empty;
				Position = _index = index;
				Token = TokenType.None;
				Value = null;
				Target = target ?? new EvaluatorEngine();
				Comparison = comparison;
				_buffer = new StringBuilder();
			}

			public EvaluatorEngine Target { get; }

			public StringComparison Comparison { get; }

			public int Position { get; private set; }

			public TokenType Token { get; private set; }

			public object Value { get; private set; }

			public bool IsEnd => Token == TokenType.End;

			public bool IsName => Token == TokenType.Name;

			public bool IsNumber => Token == TokenType.Number;

			public bool IsString => Token == TokenType.String;

			public bool IsSymbol => Token == TokenType.Name || Token == TokenType.Other;

			public bool IsOp(string op)
			{
				return IsSymbol && string.Equals(Value as string, op, Comparison);
			}

			public bool IsOp(string op1, string op2)
			{
				if (! IsSymbol)
				{
					return false;
				}

				return string.Equals(Value as string, op1, Comparison) ||
				       string.Equals(Value as string, op2, Comparison);
			}

			public bool IsOp(string op1, string op2, string op3)
			{
				if (! IsSymbol)
				{
					return false;
				}

				return string.Equals(Value as string, op1, Comparison) ||
				       string.Equals(Value as string, op2, Comparison) ||
				       string.Equals(Value as string, op3, Comparison);
			}

			public bool IsOp(params string[] ops)
			{
				if (! IsSymbol)
				{
					return false;
				}

				var s = Value as string;
				return ops.Any(op => string.Equals(s, op));
			}

			public void Advance()
			{
				do
				{
					Position = _index; // record token's start position

					object value;
					Token = ScanToken(_text, ref _index, out value, _buffer);
					Value = value;
					// The scanner returns white space, but we're not interested
				} while (Token == TokenType.White);
			}

			public void Advance(string op)
			{
				if (! string.Equals(Value as string, op, Comparison))
				{
					throw SyntaxError(Position, "Expected '{0}', but got '{1}'", op, Value);
				}

				Advance();
			}

			public override string ToString()
			{
				return string.Format("Position = {0}, Token = {1}, Value = {2}",
				                     Position, Token, Value ?? "(null)");
			}
		}

		private static EvaluatorEngine Compile(string text, int index, out int length,
		                                       StringComparison comparison)
		{
			int anchor = index;
			var state = new ParseState(text, index, new EvaluatorEngine(), comparison);

			state.Advance();

			ParseExpression(state);

			state.Target.EmitCode(OpCode.End);
			state.Target.Commit();

			length = state.Position - anchor;
			return state.Target;
		}

		private static void ParseExpression(ParseState state)
		{
			// Operator Precedence:
			//  1.  .  []  ()      primary: refinement, invocation
			//  2.  +  -  !        unary: positive, negative, logical not
			//  3.  *  /  %        multiplicative binops
			//  4.  +  -           additive binops
			//  5.  <  <=  >  >=   relational binops
			//      is  in         type check, member check
			//      =  <>          equality binops
			//  6.  &&             logical and (guard)
			//  7.  ||             logical or (default)
			//  8.  ??             null coalescing
			//  9.  ?:             conditional operator
			// Null coalescing has low precedence because null
			// propagates through the higher-precedence operators.

			ParseConditionalExpr(state);
		}

		private static void ParseConditionalExpr(ParseState state)
		{
			ParseNullCoalesceExpr(state);

			if (state.IsOp("?"))
			{
				state.Advance();

				// With 2VL, the code for P ? A : B is as simple as:
				//
				// [P]  Jif@1  [A]  Jmp@end  [B]
				//
				// With 3VL we want null ? A : B to evaluate to null.
				// There are now three cases, and the code becomes:
				//
				// [P]  Dup  Jit@1  Jif@2  Null  Jmp@end  @1: Pop  [A]  Jmp@end  @2: [B]

				var consequent = state.Target.ObtainLabel();
				var alternative = state.Target.ObtainLabel();
				var end = state.Target.ObtainLabel();

				state.Target.EmitCode(OpCode.Dup);
				state.Target.EmitJump(OpCode.Jit, consequent);
				state.Target.EmitJump(OpCode.Jif, alternative);
				state.Target.EmitCode(OpCode.Null);
				state.Target.EmitJump(OpCode.Jmp, end);

				state.Target.DefineLabel(consequent);
				state.Target.EmitCode(OpCode.Pop);

				ParseConditionalExpr(state); // consequent

				state.Advance(":");

				state.Target.EmitJump(OpCode.Jmp, end);

				state.Target.DefineLabel(alternative);

				ParseConditionalExpr(state); // alternative

				state.Target.DefineLabel(end);
			}
		}

		private static void ParseNullCoalesceExpr(ParseState state)
		{
			ParseLogicalOrExpr(state);

			if (state.IsOp("??"))
			{
				var end = state.Target.ObtainLabel();
				var alt = state.Target.ObtainLabel();

				state.Advance();

				state.Target.EmitCode(OpCode.Dup);
				state.Target.EmitJump(OpCode.Jin, alt);
				state.Target.EmitJump(OpCode.Jmp, end);
				state.Target.DefineLabel(alt);
				state.Target.EmitCode(OpCode.Pop);

				ParseUnaryExpr(state);

				state.Target.DefineLabel(end);
			}
		}

		#region Shortcutting logical AND and OR

		//private static void ParseLogicalOrExpr(ParseState state)
		//{
		//	// LogicalOrExpr ::= LogicalAndExpr { "||" LogicalAndExpr }
		//	//
		//	// Semantics: A || B = if A then A else B (like JavaScript)
		//	//
		//	// For the code generated: see ParseLogicalAndExpr and s/JIF/JIT/g

		//	ParseLogicalAndExpr(state);

		//	if (state.IsOp("||")) // Could drop this if; unused labels are harmless
		//	{
		//		var end = state.Target.ObtainLabel();

		//		while (state.IsOp("||"))
		//		{
		//			state.Advance();

		//			state.Target.EmitCode(OpCode.Dup);
		//			state.Target.EmitJump(OpCode.Jit, end);
		//			state.Target.EmitCode(OpCode.Pop);

		//			ParseLogicalAndExpr(state);
		//		}

		//		state.Target.DefineLabel(end);
		//	}
		//}

		//private static void ParseLogicalAndExpr(ParseState state)
		//{
		//	// LogicalAndExpr ::= RelationalExpr { "&&" RelationalExpr }
		//	//
		//	// Semantics: A && B = if A then B else A (like JavaScript)
		//	//
		//	// Expression:  A && B
		//	// Stack:  A // A A // A      // _   // B // *
		//	// Code:   A // DUP // JIF @1 // POP // B // @1: (end)
		//	//              --------------------
		//	//
		//	// Expression:  A && B && C
		//	// Stack:  A // A A // A      // _   // B // B B // B      // _   // C // *
		//	// Code:   A // DUP // JIF @1 // POP // B // DUP // JIF @1 // POP // C // @1: (end)
		//	//              --------------------         --------------------

		//	ParseRelationalExpr(state);

		//	if (state.IsOp("&&")) // Could drop this if; unused labels are harmless
		//	{
		//		var end = state.Target.ObtainLabel();

		//		while (state.IsOp("&&"))
		//		{
		//			state.Advance();

		//			state.Target.EmitCode(OpCode.Dup);
		//			state.Target.EmitJump(OpCode.Jif, end);
		//			state.Target.EmitCode(OpCode.Pop);

		//			ParseRelationalExpr(state);
		//		}

		//		state.Target.DefineLabel(end);
		//	}
		//}

		#endregion

		private static void ParseLogicalOrExpr(ParseState state)
		{
			ParseLogicalAndExpr(state);

			while (state.IsOp("or"))
			{
				state.Advance();

				ParseLogicalAndExpr(state);

				state.Target.EmitCode(OpCode.Or);
			}
		}

		private static void ParseLogicalAndExpr(ParseState state)
		{
			ParsePredicateExpr(state);

			while (state.IsOp("and"))
			{
				state.Advance();

				ParsePredicateExpr(state);

				state.Target.EmitCode(OpCode.And);
			}
		}

		private static void ParsePredicateExpr(ParseState state)
		{
			ParseAdditiveExpr(state);

			if (state.IsOp("is"))
			{
				state.Advance();
				ParseTypeCheck(state);
			}
			else if (state.IsOp("not"))
			{
				state.Advance();
				if (state.IsOp("in"))
				{
					state.Advance();
					ParseMemberCheck(state, true);
				}
				else
				{
					object o = state.IsSymbol ? state.Value : state.Token;
					throw SyntaxError(
						state.Position,
						"Expected 'not in', but got 'not {0}'", o);
				}
			}
			else if (state.IsOp("in"))
			{
				state.Advance();
				ParseMemberCheck(state, false);
			}
			else if (state.IsOp(_relops))
			{
				ParseRelationalExpr(state);
			}
		}

		private static void ParseRelationalExpr(ParseState state)
		{
			// We support chained comparison, eg, A < B = C <= D < E
			// (like Python, but different from C, JavaScript, et al.)
			//
			// Notice that A op B op C says nothing about how A relates to C.
			//
			// The code generated evaluates each operand at most once.
			// For example, in "1 < 1+1 < 1-1 < 2*3", the sum is evaluated
			// only once and the multiplication is not evaluated at all.
			//
			// Expression: A op B op C etc.
			// Stack:  A   A     B     B      B     B     C     C      C
			//             B     A    F/T           C     B    F/T
			//                   B                        C
			// Code:   A ; B ; Dup1 ; Cop ; Jif@1 ; C ; Dup1 ; Cop ; Jif@1 ; Pop ; True ; Jmp@end ; @1: Pop ; False
			//                 ------------------       ------------------
			//
			// Notice that emitting Jop' in place of Cop+Jif where op' is the negation of op
			// (that is, ne for eq, ge for lt, etc.) is a valid optimization for 2VL, but
			// it is not correct for 3VL because the "excluded third" doesn't hold.
			//
			// The binary case "A op B " is simplified to:  A ; B ; Cop (end)
			//
			// The initial AdditiveExpr has already been consumed, and
			// the current token is known to be a relop (see calling code).

			int operatorCount = 0;
			var falsum = state.Target.ObtainLabel();
			var end = state.Target.ObtainLabel();

			while (state.IsOp(_relops))
			{
				operatorCount += 1;

				var op = (string) state.Value;
				state.Advance();
				ParseAdditiveExpr(state);

				if (operatorCount == 1 && ! state.IsOp(_relops))
				{
					// Code for the (frequent) binary case:
					state.Target.EmitCode(GetComp(op));
					return;
				}

				// Code for the (general) chained case:
				state.Target.EmitCode(OpCode.Dup1);
				state.Target.EmitCode(GetComp(op));
				state.Target.EmitJump(OpCode.Jif, falsum);
			}

			state.Target.EmitCode(OpCode.Pop);
			state.Target.EmitValue(true);
			state.Target.EmitJump(OpCode.Jmp, end);
			state.Target.DefineLabel(falsum);
			state.Target.EmitCode(OpCode.Pop);
			state.Target.EmitValue(false);
			state.Target.DefineLabel(end);
		}

		private static void ParseTypeCheck(ParseState state)
		{
			// Syntax: ... is [not] type (the 'is' is already consumed)
			bool negate = false;
			if (state.IsOp("not"))
			{
				negate = true;
				state.Advance();
			}

			if (state.IsOp("null"))
			{
				state.Advance();
				state.Target.EmitCode(OpCode.Cin);
				if (negate)
				{
					state.Target.EmitCode(OpCode.Not);
				}
			}
			else if (state.IsName)
			{
				// TODO parse Name {'.' Name}, useful for: ... is ESRI.ArcGIS.Display.IColor
				var type = (string) state.Value;
				state.Advance();
				state.Target.EmitValue(type);
				state.Target.EmitCode(OpCode.Is);
				if (negate)
				{
					state.Target.EmitCode(OpCode.Not);
				}
			}
			else
			{
				object o = state.IsSymbol ? state.Value : state.Token;
				throw SyntaxError(
					state.Position,
					"Expected 'is null' or 'is Type', but got 'is {0}'", o);
			}
		}

		private static void ParseMemberCheck(ParseState state, bool negate)
		{
			// Syntax: ... [not] in (AdditiveExpr {, AdditiveExpr})
			// The 'in' and optional 'not' is already consumed

			// Notice that A in (B, C) is just a convenient abbreviation
			// for A = B or A = C, so we can generate code like this:
			//             B                       C
			//         A   A    T/F            A   A    T/F
			//   A     A   A     A       A     A   A     A       A             F                         T
			// A . Dup . B . Ceq . Jit@1 . Dup . C . Ceq . Jit@1 . Pop . False . Jmp@end . @1:Pop . True .
			//     ---------------------   ---------------------
			// and when negated ("not in") we emit the same code as above
			// but with the opcodes True and False exchanged.

			state.Advance("(");

			if (state.IsOp(")"))
			{
				// Special case: "foo in ()" is always false
				state.Advance();
				state.Target.EmitCode(OpCode.Pop);
				state.Target.EmitValue(false);
			}
			else
			{
				var mid = state.Target.ObtainLabel();
				var end = state.Target.ObtainLabel();

				state.Target.EmitCode(OpCode.Dup);
				ParseAdditiveExpr(state);
				state.Target.EmitCode(OpCode.Ceq);
				state.Target.EmitJump(OpCode.Jit, mid);

				while (state.IsOp(","))
				{
					state.Advance();
					state.Target.EmitCode(OpCode.Dup);
					ParseAdditiveExpr(state);
					state.Target.EmitCode(OpCode.Ceq);
					state.Target.EmitJump(OpCode.Jit, mid);
				}

				state.Advance(")");

				state.Target.EmitCode(OpCode.Pop);
				state.Target.EmitValue(negate);
				state.Target.EmitJump(OpCode.Jmp, end);
				state.Target.DefineLabel(mid);
				state.Target.EmitCode(OpCode.Pop);
				state.Target.EmitValue(! negate);
				state.Target.DefineLabel(end);
			}
		}

		private static void ParseAdditiveExpr(ParseState state)
		{
			ParseMultiplicativeExpr(state);

			while (state.IsOp("+", "-"))
			{
				var op = (string) state.Value;
				state.Advance();
				ParseMultiplicativeExpr(state);

				switch (op)
				{
					case "+":
						state.Target.EmitCode(OpCode.Add);
						break;
					case "-":
						state.Target.EmitCode(OpCode.Sub);
						break;
					default:
						throw ParserBug("Unexpected operator: {0}", op);
				}
			}
		}

		private static void ParseMultiplicativeExpr(ParseState state)
		{
			ParseUnaryExpr(state);

			while (state.IsOp("*", "/", "%"))
			{
				var op = (string) state.Value;
				state.Advance();
				ParseUnaryExpr(state);

				switch (op)
				{
					case "*":
						state.Target.EmitCode(OpCode.Mul);
						break;
					case "/":
						state.Target.EmitCode(OpCode.Div);
						break;
					case "%":
						state.Target.EmitCode(OpCode.Rem);
						break;
					default:
						throw ParserBug("Unexpected operator: {0}", op);
				}
			}
		}

		private static void ParseUnaryExpr(ParseState state)
		{
			if (state.IsOp("-"))
			{
				state.Advance();
				ParseUnaryExpr(state);

				state.Target.EmitCode(OpCode.Neg);
			}
			else if (state.IsOp("+"))
			{
				state.Advance();
				ParseUnaryExpr(state);

				state.Target.EmitCode(OpCode.Pos);
			}
			else if (state.IsOp("not"))
			{
				state.Advance();
				ParseUnaryExpr(state);

				state.Target.EmitCode(OpCode.Not);
			}
			else
			{
				ParsePostfixExpr(state);
			}
		}

		private static void ParsePostfixExpr(ParseState state)
		{
			// What we do is optional qualification of names:
			// PostfixExpr ::= PrimaryExpr
			//               | PrimaryExpr "(" ArgumentList ")"
			//
			// ArgumentList ::= <empty> | Expr { "," Expr }
			//
			// How a full C-like grammar looks like:
			// PostfixExpr ::= PrimaryExpr
			//               | PostfixExpr "." Name 
			//               | PostfixExpr "[" Expr "]" 
			//               | PostfixExpr "(" ArgumentList ")"
			//
			// And how that would be parsed:
			// ParsePrimaryExpr(state);
			// while (state.IsOp(".", "[", "(")) {
			//   var op = (string) state.Value;
			//   state.Advance();
			//   switch (op) { etc... }

			ParsePrimaryExpr(state);

			if (state.IsOp("("))
			{
				// TODO Optimization: if invocation target is syntactically known: resolve now (and check param types)
				state.Advance();
				int n = ParseArgumentList(state);
				state.Advance(")");
				state.Target.EmitCall(n);
			}
		}

		private static int ParseArgumentList(ParseState state)
		{
			if (state.IsOp(")"))
			{
				return 0; // the empty list
			}

			int count = 1;
			ParseExpression(state);

			while (state.IsOp(","))
			{
				state.Advance();
				count += 1;
				ParseExpression(state);
			}

			return count; // #args in list
		}

		private static void ParsePrimaryExpr(ParseState state)
		{
			// PrimaryExpr ::= Number | String | Name | Name "." Name | "(" Expr ")"

			if (state.IsNumber)
			{
				var value = (double) state.Value;
				state.Advance();

				state.Target.EmitValue(value);
			}
			else if (state.IsString)
			{
				var value = (string) state.Value;
				state.Advance();

				state.Target.EmitValue(value);
			}
			else if (state.IsName)
			{
				var name = (string) state.Value;
				state.Advance();

				if (string.Equals(name, "null", state.Comparison))
				{
					state.Target.EmitValue(null);
				}
				else if (string.Equals(name, "false", state.Comparison))
				{
					state.Target.EmitValue(false);
				}
				else if (string.Equals(name, "true", state.Comparison))
				{
					state.Target.EmitValue(true);
				}
				else
				{
					if (state.IsOp("."))
					{
						string qualifier = name;
						state.Advance();
						if (! state.IsName)
						{
							var o = state.IsSymbol ? state.Value : state.Token;
							throw SyntaxError(
								state.Position,
								"Expected '{0}.Name', but got '{0}.{1}'",
								qualifier, o);
						}

						name = (string) state.Value;
						state.Advance();
						state.Target.EmitValue(qualifier);
						state.Target.EmitValue(name);
						state.Target.EmitCode(OpCode.GetQualified); // Stack: ... qual name => value
					}
					else
					{
						state.Target.EmitValue(name);
						state.Target.EmitCode(OpCode.Get); // Stack: ... name => value
					}
				}
			}
			else if (state.IsOp("("))
			{
				state.Advance();

				ParseExpression(state);

				state.Advance(")");
			}
			else
			{
				throw SyntaxError(
					state.Position,
					"Expected a number, a string, a name, or '(', but got '{0}'",
					state.Value);
			}
		}

		private static OpCode GetComp(string op)
		{
			switch (op)
			{
				case "==":
				case "=":
					return OpCode.Ceq;
				case "!=":
				case "<>":
					return OpCode.Cne;
				case "<":
					return OpCode.Clt;
				case "<=":
					return OpCode.Cle;
				case ">":
					return OpCode.Cgt;
				case ">=":
					return OpCode.Cge;
				default:
					throw ParserBug("Unexpected operator: {0}", op);
			}
		}

		private static Exception ParserBug(string format, params object[] args)
		{
			var sb = new StringBuilder("Bug in expression parser");
			if (! string.IsNullOrEmpty(format))
			{
				sb.Append(": ");
				sb.AppendFormat(format, args);
			}

			return new AssertionException(sb.ToString());
		}

		#endregion

		#region Lexical Scanner

		public static TokenType ScanToken(string text, ref int index, out object value,
		                                  StringBuilder sb)
		{
			// Name: _foo AND Hello null
			// Number: +123, 0.345, -.567, 78.9
			// String: 'abc''def' (like SQL) or "foo\nbar" (like C)
			// Other: = <> < > <= >=
			// White: runs of char.IsWhiteSpace()

			if (index >= text.Length)
			{
				value = null;
				return TokenType.End;
			}

			char cc = text[index];

			if (char.IsWhiteSpace(cc))
			{
				int length = ScanWhite(text, index);
				value = null;
				index += length;
				return TokenType.White;
			}

			if (char.IsLetter(cc) || cc == '_')
			{
				int length = ScanName(text, index);
				value = text.Substring(index, length);
				index += length;
				return TokenType.Name;
			}

			if (cc == '\'')
			{
				sb.Length = 0; // clear
				int length = ScanSqlString(text, index, sb);
				value = sb.ToString();
				index += length;
				return TokenType.String;
			}

			if (cc == '"')
			{
				sb.Length = 0; // clear
				int length = ScanString(text, index, sb);
				value = sb.ToString();
				index += length;
				return TokenType.String;
			}

			if (char.IsDigit(cc))
			{
				int length = ScanNumber(text, index);
				string s = text.Substring(index, length);

				double number;
				if (! double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture,
				                      out number))
				{
					throw SyntaxError(index, "Invalid number");
				}

				value = number;
				index += length;
				return TokenType.Number;
			}

			int len = 0;

			switch (cc)
			{
				case '!': // ! or !=
					len = ScanOperator(text, index, "=");
					break;
				case '&': // & or &&
					len = ScanOperator(text, index, "&");
					break;
				case '+': // + or ++
					len = ScanOperator(text, index, "+");
					break;
				case '-':
					len = ScanOperator(text, index, "-");
					break;
				case '<': // < or <= or << or <>
					len = ScanOperator(text, index, "=<>");
					break;
				case '=': // = or == or =>
					len = ScanOperator(text, index, "=>");
					break;
				case '>': // > or >= or >>
					len = ScanOperator(text, index, "=>");
					break;
				case '?': // ? or ??
					len = ScanOperator(text, index, "?");
					break;
				case '|': // | or ||
					len = ScanOperator(text, index, "|");
					break;
			}

			if (len > 0)
			{
				value = text.Substring(index, len);
				index += len;
				return TokenType.Other;
			}

			// Notice that some of the operators here have already
			// been handled above. Never mind. Also notice that there
			// are a few more ASCII symbols that do not occur here.
			const string ops = "!#$%&()*+,-./:;<=>?@[]^_{|}~";

			if (ops.IndexOf(cc) >= 0)
			{
				value = text.Substring(index, 1);
				index += 1;
				return TokenType.Other;
			}

			throw 32 < cc && cc < 127
				      ? SyntaxError(index, "Invalid input character: '{0}'", cc)
				      : SyntaxError(index, "Invalid input character: U+{0:D4}", (int) cc);
		}

		private static int ScanOperator(string text, int index, string next)
		{
			index += 1;

			return index < text.Length && next.IndexOf(text[index]) >= 0 ? 2 : 1;
		}

		private static int ScanWhite(string text, int index)
		{
			int anchor = index;
			while (index < text.Length && char.IsWhiteSpace(text, index))
			{
				index += 1;
			}

			return index - anchor;
		}

		private static int ScanName(string text, int index)
		{
			int anchor = index;
			while (index < text.Length && (char.IsLetterOrDigit(text, index) || text[index] == '_'))
			{
				index += 1;
			}

			return index - anchor;
		}

		private static int ScanSqlString(string text, int index, StringBuilder sb)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(text[index] == '\'', "Bug");

			sb.Length = 0; // clear
			char quote = text[index];
			int anchor = index++; // skip opening apostrophe

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc == quote)
				{
					if (index < text.Length && text[index] == quote)
					{
						sb.Append(quote); // un-escape
						index += 1; // skip 2nd apostrophe
					}
					else
					{
						return index - anchor;
					}
				}
				else
				{
					sb.Append(cc);
				}
			}

			throw SyntaxError(anchor, "Unterminated string");
		}

		private static int ScanString(string text, int index, StringBuilder sb)
		{
			Assert.True(index < text.Length, "Bug");
			Assert.True(text[index] == '"', "Bug");

			sb.Length = 0; // clear
			char quote = text[index];
			int anchor = index++; // skip opening quote

			while (index < text.Length)
			{
				char cc = text[index++];

				if (cc < ' ')
				{
					throw SyntaxError(index - 1, "Control character in string");
				}

				if (cc == quote)
				{
					return index - anchor;
				}

				if (cc == '\\')
				{
					if (index >= text.Length)
					{
						break;
					}

					switch (cc = text[index++])
					{
						case '"':
						case '\'':
						case '\\':
						case '/':
							sb.Append(cc);
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case 'u':
							throw SyntaxError(index, "\\u#### is not yet implemented");
						default:
							throw SyntaxError(index, "Invalid escape '\\{0}' in string", cc);
					}
				}
				else
				{
					sb.Append(cc);
				}
			}

			throw SyntaxError(anchor, "Unterminated string");
		}

		private static int ScanNumber(string text, int index)
		{
			char cc;
			int anchor = index;

			//if (index < text.Length && ((cc = text[index]) == '-' || cc == '+'))
			//{
			//    index += 1; // optional - or + sign (handled by parser/eval)
			//}

			while (index < text.Length && char.IsDigit(text, index))
			{
				index += 1;
			}

			if (index < text.Length && text[index] == '.')
			{
				index += 1;

				while (index < text.Length && char.IsDigit(text, index))
				{
					index += 1;
				}
			}

			if (index < text.Length && ((cc = text[index]) == 'e' || cc == 'E'))
			{
				index += 1;

				if (index < text.Length && ((cc = text[index]) == '-' || cc == '+'))
				{
					index += 1;
				}

				while (index < text.Length && char.IsDigit(text, index))
				{
					index += 1;
				}
			}

			if (index < text.Length && (char.IsLetter(text, index) || text[index] == '_'))
			{
				throw SyntaxError(anchor, "Unterminated numeric token");
			}

			return index - anchor;
		}

		private static Exception SyntaxError(int index, string format, params object[] args)
		{
			var sb = new StringBuilder();
			sb.AppendFormat(format, args);
			sb.AppendFormat(" (at position {0})", index);
			return new FormatException(sb.ToString());
		}

		#endregion

		#region Nested type: TokenType

		public enum TokenType
		{
			None = -1,
			End = 0,
			White,
			Name,
			Number,
			String,
			Other
		}

		#endregion
	}
}
