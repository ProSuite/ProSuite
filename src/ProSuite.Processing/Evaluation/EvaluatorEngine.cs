using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// A simple stack-based virtual engine for evaluating
	/// the expressions understood by <see cref="ExpressionEvaluator"/>.
	/// </summary>
	public class EvaluatorEngine
	{
		private readonly List<byte> _program;
		private readonly LiteralPool _literalPool;
		private IList<int> _labels;
		private const int UndefinedLabel = -1;
		private IList<Fixup> _fixups;
		private bool _committed;

		public EvaluatorEngine()
		{
			_program = new List<byte>();
			_literalPool = new LiteralPool();
			_committed = false;
		}

		public int ProgramSize => _program.Count;

		/// <summary>
		/// Execute the program that was created previously by calls to
		/// the Emit<i>Foo</i>() methods. You must provide an
		/// <paramref name="env"/> that provides variable bindings,
		/// and an evaluation <paramref name="stack"/>, which <em>must</em>
		/// be empty on entry and will hold exactly one item, the result,
		/// on exit.
		/// </summary>
		/// <param name="env">The environment (required).</param>
		/// <param name="stack">The evaluation stack (required; must be empty on entry).</param>
		public void Execute([NotNull] IEvaluationEnvironment env, [NotNull] Stack<object> stack)
		{
			if (env is null)
				throw new ArgumentNullException(nameof(env));
			if (stack is null)
				throw new ArgumentNullException(nameof(stack));

			if (! _committed)
			{
				throw new InvalidOperationException("Must first commit; cannot execute");
			}

			int pc = 0; // execution starts at address zero

			while (true)
			{
				object op1, op2;
				int extra, next = pc + 1;
				var op = (OpCode) _program[pc];

				switch (op)
				{
					case OpCode.Nop:
						break;

					#region Constants

					case OpCode.Null:
						stack.Push(null);
						break;

					case OpCode.False:
						stack.Push(false);
						break;

					case OpCode.True:
						stack.Push(true);
						break;

					case OpCode.Zero:
						stack.Push(0.0);
						break;

					case OpCode.Unit:
						stack.Push(1.0);
						break;

					case OpCode.Empty:
						stack.Push(string.Empty);
						break;

					case OpCode.Pool:
						extra = GetInt24(_program, pc + 1);
						next = pc + 4;
						stack.Push(_literalPool.Get(extra));
						break;

					#endregion

					#region Stack

					case OpCode.Dup:
					case OpCode.Dup1:
					case OpCode.Dup2:
					case OpCode.Pop:
					case OpCode.Swap:
						DoStack(stack, op);
						break;

					#endregion

					#region Jumps

					case OpCode.Jmp:
						extra = GetInt24(_program, pc + 1);
						next = pc + extra;
						break;

					case OpCode.Jin:
						extra = GetInt24(_program, pc + 1);
						op1 = stack.Pop();
						next = pc + (op1 == null ? extra : 4);
						break;
					case OpCode.Jif:
						extra = GetInt24(_program, pc + 1);
						op1 = stack.Pop();
						next = pc + (env.IsFalse(op1) ? extra : 4);
						break;
					case OpCode.Jit:
						extra = GetInt24(_program, pc + 1);
						op1 = stack.Pop();
						next = pc + (env.IsTrue(op1) ? extra : 4);
						break;

					case OpCode.Jeq:
					case OpCode.Jne:
					case OpCode.Jlt:
					case OpCode.Jle:
					case OpCode.Jgt:
					case OpCode.Jge:
						extra = GetInt24(_program, pc + 1);
						op2 = stack.Pop();
						op1 = stack.Pop();
						next = pc + (Compare(op1, op2, op, env) ? extra : 4);
						break;

					#endregion

					#region Comparison

					case OpCode.Cin:
						op1 = stack.Pop();
						stack.Push(op1 == null);
						break;

					case OpCode.Ceq:
					case OpCode.Cne:
					case OpCode.Clt:
					case OpCode.Cle:
					case OpCode.Cgt:
					case OpCode.Cge:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(Compare(op1, op2, op, env));
						break;

					#endregion

					#region Arithmetic

					case OpCode.Add:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.Add(op1, op2));
						break;
					case OpCode.Sub:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.Sub(op1, op2));
						break;
					case OpCode.Mul:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.Mul(op1, op2));
						break;
					case OpCode.Div:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.Div(op1, op2));
						break;
					case OpCode.Rem:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.Rem(op1, op2));
						break;

					case OpCode.Pos:
						op1 = stack.Pop();
						stack.Push(env.Pos(op1));
						break;

					case OpCode.Neg:
						op1 = stack.Pop();
						stack.Push(env.Neg(op1));
						break;

					#endregion

					#region Logic

					case OpCode.Not:
						op1 = stack.Pop();
						stack.Push(env.Not(op1));
						break;

					case OpCode.And:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.And(op1, op2));
						break;

					case OpCode.Or:
						op2 = stack.Pop();
						op1 = stack.Pop();
						stack.Push(env.Or(op1, op2));
						break;

					#endregion

					case OpCode.Is:
						op2 = stack.Pop(); // type
						op1 = stack.Pop(); // value
						stack.Push(env.IsType(op1, (string) op2));
						break;

					case OpCode.Get:
						op1 = stack.Pop();
						stack.Push(env.Lookup((string) op1, null));
						break;

					case OpCode.GetQualified:
						op1 = stack.Pop(); // name
						op2 = stack.Pop(); // qualifier
						stack.Push(env.Lookup((string) op1, (string) op2));
						break;

					case OpCode.Call:
						extra = GetInt24(_program, pc + 1);
						Invoke(extra, stack, env);
						next = pc + 4;
						break;

					case OpCode.End:
						return;

					default:
						throw EngineBug($"Unknown OpCode: {op}");
				}

				pc = next;
			}
		}

		/// <summary>
		/// Output a human-readable representation of the engine's
		/// program to the <paramref name="writer"/> provided.
		/// </summary>
		/// <param name="writer">The writer (required).</param>
		public void Dump([NotNull] TextWriter writer)
		{
			if (writer is null)
				throw new ArgumentNullException(nameof(writer));

			var sb = new StringBuilder();
			int pc = 0, count = _program.Count;

			while (pc < count)
			{
				var op = (OpCode) _program[pc++];

				sb.Length = 0; // clear
				sb.AppendFormat("{0:X4}  {1}", pc - 1, op);

				if (op == OpCode.Pool)
				{
					int index = GetInt24(_program, pc);
					pc += 3;
					sb.AppendFormat(" #{0}", index);
					while (sb.Length < 20) sb.Append(' ');
					sb.Append("// ");
					object value = _literalPool.Get(index);
					FormatLiteral(value, sb);
				}
				else if (op == OpCode.Call)
				{
					int nargs = GetInt24(_program, pc);
					pc += 3;
					sb.AppendFormat(" #{0}", nargs);
					while (sb.Length < 20) sb.Append(' ');
					sb.AppendFormat("// arity = {0}", nargs);
				}
				else if (IsJumpInstruction(op))
				{
					int offset = GetInt24(_program, pc);
					int target = pc + offset - 1;
					pc += 3;
					sb.AppendFormat(" {0:X4}", target);
				}

				writer.WriteLine(sb.ToString());
			}
		}

		#region Code generation

		public void Commit()
		{
			CheckUncommitted();

			ApplyFixups();

			_literalPool.Commit();
			_committed = true;
		}

		public int EmitCode(OpCode op)
		{
			CheckUncommitted();

			int addr = _program.Count;
			_program.Add((byte) op);
			return addr;
		}

		public void EmitValue(bool value)
		{
			CheckUncommitted();

			_program.Add(value ? (byte) OpCode.True : (byte) OpCode.False);
		}

		public void EmitValue(double value)
		{
			CheckUncommitted();

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value == 0.0)
			{
				_program.Add((byte) OpCode.Zero);
				return;
			}

			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (value == 1.0)
			{
				_program.Add((byte) OpCode.Unit);
				return;
			}

			int index = _literalPool.Put(value);
			_program.Add((byte) OpCode.Pool);
			int addr = _program.Count;
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			PutInt24(_program, addr, index);
		}

		public void EmitValue(string value)
		{
			CheckUncommitted();

			if (value == null)
			{
				_program.Add((byte) OpCode.Null);
				return;
			}

			if (value.Length == 0)
			{
				_program.Add((byte) OpCode.Empty);
				return;
			}

			int index = _literalPool.Put(value);
			_program.Add((byte) OpCode.Pool);
			int addr = _program.Count;
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			PutInt24(_program, addr, index);
		}

		public void EmitValue(object value)
		{
			CheckUncommitted();

			if (value == null)
			{
				_program.Add((byte) OpCode.Null);
				return;
			}

			int index = _literalPool.Put(value);
			_program.Add((byte) OpCode.Pool);
			int addr = _program.Count;
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			PutInt24(_program, addr, index);
		}

		public void EmitCall(int argCount)
		{
			CheckUncommitted();

			_program.Add((byte) OpCode.Call);
			int addr = _program.Count;
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			_program.Add((byte) OpCode.Nop);
			PutInt24(_program, addr, argCount);
		}

		public void EmitJump(OpCode op, Label label)
		{
			CheckUncommitted();

			if (! IsJumpInstruction(op))
			{
				throw new ArgumentException(@"Expect a jump instruction", nameof(op));
			}

			AddFixup(_program.Count, label);

			_program.Add((byte) op);
			// Reserve space for the jump offset:
			_program.Add(0);
			_program.Add(0);
			_program.Add(0);
		}

		public Label ObtainLabel()
		{
			if (_labels == null)
			{
				_labels = new List<int>();
			}

			int index = _labels.Count;
			_labels.Add(UndefinedLabel);
			return new Label(index);
		}

		public void DefineLabel(Label label)
		{
			int index = label.Value;
			if (index < 0 || _labels == null || index >= _labels.Count)
			{
				throw new ArgumentException("Invalid label");
			}

			if (_labels[index] != UndefinedLabel)
			{
				throw new ArgumentException("Redefined label");
			}

			_labels[index] = _program.Count;
		}

		#endregion

		#region Private methods

		private void CheckUncommitted()
		{
			if (_committed)
			{
				throw new InvalidOperationException("Already committed; cannot emit code");
			}
		}

		private static void DoStack(Stack<object> stack, OpCode op)
		{
			object op1, op2;

			switch (op)
			{
				case OpCode.Dup: // ... x => ... x x
					stack.Push(stack.Peek());
					break;

				case OpCode.Dup1: // ... x y => ... y x y  (like JVM's dup_x1)
					op1 = stack.Pop();
					op2 = stack.Pop();
					stack.Push(op1);
					stack.Push(op2);
					stack.Push(op1);
					break;

				case OpCode.Dup2:
					throw new NotImplementedException(); // do we really need that?

				case OpCode.Pop: // ... x => ...
					stack.Pop();
					break;

				case OpCode.Swap: // ... x y => ... y x
					op1 = stack.Pop();
					op2 = stack.Pop();
					stack.Push(op1);
					stack.Push(op2);
					break;

				default:
					throw EngineBug($"Not a stack operation: {op}");
			}
		}

		private static bool Compare(object a, object b, OpCode op, IEvaluationEnvironment env)
		{
			int? order = env.Compare(a, b);

			if (order == null)
			{
				// If a and b are uncomparable,
				// return false whatever the op is:
				return false;
			}

			switch (op)
			{
				case OpCode.Ceq:
				case OpCode.Jeq:
					return order == 0;
				case OpCode.Cne:
				case OpCode.Jne:
					return order != 0;
				case OpCode.Clt:
				case OpCode.Jlt:
					return order < 0;
				case OpCode.Cle:
				case OpCode.Jle:
					return order <= 0;
				case OpCode.Cgt:
				case OpCode.Jgt:
					return order > 0;
				case OpCode.Cge:
				case OpCode.Jge:
					return order >= 0;
			}

			throw EngineBug($"Invalid OpCode for Compare(a,b,op): {op}");
		}

		private static void Invoke(int argCount, Stack<object> stack,
		                           IEvaluationEnvironment environment)
		{
			// Stack: ... target arg1 arg2 ... argN
			// Direct access à la stack[i] would be nice!
			// For now, we copy stack items into an array...

			object[] args = null;

			if (argCount > 0)
			{
				args = new object[argCount];

				for (int i = argCount; --i >= 0;)
				{
					args[i] = stack.Pop();
				}
			}

			var target = stack.Pop();

			if (target == null)
			{
				throw InvocationError("Attempt to invoke null");
			}

			if (! (target is Function function))
			{
				throw InvocationError("Attempt to invoke non-function value");
			}

			object result = Invoke(environment, function, args);

			stack.Push(result);
		}

		private static object Invoke(IEvaluationEnvironment env, Function fun, object[] args)
		{
			try
			{
				return env.Invoke(fun, args);
			}
			catch (Exception ex)
			{
				if (ex is TargetInvocationException && ex.InnerException != null)
				{
					throw InvocationError(ex.InnerException,
					                      "Invocation of {0}({1}) returned an error: {2}",
					                      fun, Join(", ", args), ex.InnerException.Message);
				}

				throw InvocationError(ex, "Error calling {0} with args ({1}): {2}",
				                      fun, Join(", ", args), ex.Message);
			}
		}

		private static EvaluationException InvocationError(string message)
		{
			return new EvaluationException(message ?? "most illogical");
		}

		[StringFormatMethod("format")]
		private static EvaluationException InvocationError(Exception inner, string format,
		                                                   params object[] args)
		{
			throw new EvaluationException(string.Format(format, args), inner);
		}

		private static AssertionException EngineBug(string message)
		{
			throw new AssertionException(message ?? "most illogical");
		}

		private static bool IsJumpInstruction(OpCode op)
		{
			switch (op)
			{
				case OpCode.Jmp:
				case OpCode.Jin:
				case OpCode.Jif:
				case OpCode.Jit:
				case OpCode.Jeq:
				case OpCode.Jne:
				case OpCode.Jlt:
				case OpCode.Jle:
				case OpCode.Jgt:
				case OpCode.Jge:
					return true;

				default:
					return false;
			}
		}

		private static int GetInt24(IList<byte> memory, int index)
		{
			// Big Endian
			int value = memory[index] << 16 | memory[index + 1] << 8 | memory[index + 2];
			if ((value & 0x00800000) != 0)
			{
				value -= 0x01000000; // sign extension
			}

			return value;
		}

		private static void PutInt24(IList<byte> memory, int index, int value)
		{
			// -8388608 == 0xff800000
			//  8388607 == 0x007fffff
			if (value < -8388608 || value > 8388607)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value,
				                                      @"Ensure -8388608 <= value <= 8388607");
			}

			// Big Endian
			memory[index + 0] = (byte) ((value >> 16) & 255);
			memory[index + 1] = (byte) ((value >> 8) & 255);
			memory[index + 2] = (byte) (value & 255);
		}

		private void AddFixup(int address, Label label)
		{
			if (_fixups == null)
			{
				_fixups = new List<Fixup>();
			}

			_fixups.Add(new Fixup(address, label));
		}

		private void ApplyFixups()
		{
			if (_fixups == null)
			{
				return; // Nothing to fix up
			}

			foreach (var fixup in _fixups)
			{
				int index = fixup.Label.Value;
				if (index < 0 || _labels == null || index >= _labels.Count)
				{
					throw new InvalidOperationException("Invalid label");
				}

				int targetAddress = _labels[index];
				if (targetAddress == UndefinedLabel)
				{
					throw new InvalidOperationException("Undefined label");
				}

				if (! IsJumpInstruction((OpCode) _program[fixup.Address]))
				{
					throw new InvalidOperationException(
						string.Format(
							"Expect a jump instruction at {0:X4}, but found {1}",
							fixup.Address, (OpCode) _program[fixup.Address]));
				}

				PutInt24(_program, fixup.Address + 1, targetAddress - fixup.Address);
			}
		}

		private readonly struct Fixup
		{
			public readonly int Address;
			public readonly Label Label;

			public Fixup(int address, Label label)
			{
				Address = address;
				Label = label;
			}
		}

		#region Formatting values

		public static void FormatLiteral(object value, StringBuilder result)
		{
			if (value == null)
			{
				result.Append("null");
				return;
			}

			if (value is bool flag)
			{
				result.Append(flag ? "true" : "false");
				return;
			}

			if (value is string s)
			{
				FormatString(s, result);
				return;
			}

			result.AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
		}

		public static void FormatString(string value, StringBuilder result)
		{
			int len = value.Length;

			result.Append('"');

			for (int j = 0; j < len; j++)
			{
				char c = value[j];

				const string escapes = "\"\"\\\\\bb\ff\nn\rr\tt";
				int k = escapes.IndexOf(c);

				if (k >= 0 && k % 2 == 0)
				{
					result.Append('\\');
					result.Append(escapes[k + 1]);
				}
				else if (char.IsControl(c))
				{
					result.AppendFormat("\\u{0:x4}", (int) c);
				}
				else
				{
					result.Append(c);
				}
			}

			result.Append('"');
		}

		#endregion

		private static string Join(string separator, IEnumerable<object> args)
		{
			// Note: .NET 4 has such a method (but we're still using .NET 3.5)
			if (args == null)
			{
				return string.Empty;
			}

			var sb = new StringBuilder();

			foreach (object arg in args)
			{
				if (sb.Length > 0)
				{
					sb.Append(separator); // null is ok by MSDN docs
				}

				sb.Append(arg);
			}

			return sb.ToString();
		}

		#endregion
	}
}
