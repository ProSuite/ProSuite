using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Misc
{
	/// <summary>
	/// Represents a value of one of two possible types (disjoint union). 
	/// </summary>
	/// <typeparam name="TL"></typeparam>
	/// <typeparam name="TR"></typeparam>
	public class Either<TL, TR>
	{
		private readonly TL _left;
		private readonly TR _right;
		private readonly bool _isLeft;

		public Either(TL left)
		{
			_left = left;
			_isLeft = true;
		}

		public Either(TR right)
		{
			_right = right;
			_isLeft = false;
		}

		public Either<TL, TRM> Select<TRM>([NotNull] Func<TR, TRM> f) =>
			_isLeft
				? new Either<TL, TRM>(_left)
				: new Either<TL, TRM>(f(_right));

		/*
		 * TODO implement the needed combinators in C# idiomatic way
		 * (using https://github.com/scala/scala/blob/2.13.x/src/library/scala/util/Either.scala as inspiration)
		 *
		*  e.g. flatMap() -> SelectMany()
			
			public Either<TL, TRM> SelectMany<TRM>([NotNull] Func<TR, Either<TL, TRM>> f) =>
				_isLeft
					? new Either<TL, TRM>(_left)
					: f(_right);
	    */

		// poor man's pattern matching
		public T Match<T>([NotNull] Func<TL, T> leftFunc,
		                  [NotNull] Func<TR, T> rightFunc) =>
			_isLeft
				? leftFunc(_left)
				: rightFunc(_right);

		public static implicit operator Either<TL, TR>(TL left) =>
			new Either<TL, TR>(left);

		public static implicit operator Either<TL, TR>(TR right) =>
			new Either<TL, TR>(right);
	}
}
