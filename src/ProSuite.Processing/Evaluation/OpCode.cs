namespace ProSuite.Processing.Evaluation
{
	/// <summary>
	/// The instruction set of the <see cref="EvaluatorEngine"/>.
	/// <para/>
	/// When adding opcodes here, be sure to handle them in the
	/// <see cref="EvaluatorEngine"/>. And notice that, by design
	/// of the engine, there cannot be more than 256 opcodes.
	/// </summary>
	public enum OpCode : byte
	{
		Nop = 0, // no operation
		Null, False, True, Zero, Unit, Empty, Pool, // push null/false/true/0/1/""/pool
		Dup, Dup1, Dup2, Pop, Swap, // stack operations
		Jmp, Jin, Jif, Jit, // jump always, if null/false/true
		Jeq, Jne, Jlt, Jle, Jgt, Jge, // jump if eq/ne/etc.
		Ceq, Cne, Clt, Cle, Cgt, Cge, Cin, // comparison (Cin = is null)
		Add, Sub, Mul, Div, Rem, // arithmetic
		Pos, Neg, // positive and negative, short for 0+x and 0-x
		Not, And, Or, // logical not, and, or
		Is, // type check
		Get, GetQualified, // get the value of a (qualified) variable
		Call, // function invocation (stack: ... func arg1 .. argN; N: GetInt24)
		End // stop execution
	}
}
