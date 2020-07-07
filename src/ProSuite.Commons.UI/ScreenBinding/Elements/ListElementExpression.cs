using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Configuration;
using ProSuite.Commons.UI.ScreenBinding.Lists;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public class ListElementExpression : ScreenElementExpression<ListElementExpression>
	{
		private readonly IListElement _element;

		/// <summary>
		/// Initializes a new instance of the <see cref="ListElementExpression"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		public ListElementExpression([NotNull] IListElement element) : base(element)
		{
			_element = element;
		}

		protected override ListElementExpression ThisExpression()
		{
			return this;
		}

		public ListElementExpression FillWithEnum<T>()
			where T : struct, IComparable, IFormattable
		{
			_element.FillWithEnum<T>();
			return this;
		}

		public ListElementExpression FillWithValues<T>(params T[] array)
			where T : IComparable
		{
			_element.FillWith(array);
			return this;
		}

		public ListElementExpression FillWith([NotNull] IPicklist list)
		{
			_element.FillWithList(list);
			return this;
		}

		public ListElementExpression FillWithList(string name)
		{
			IPicklist list = ListRepository.GetList(name);
			return FillWith(list);
		}

		public ListElementExpression FillWithList<T>()
		{
			IPicklist list = ListRepository.GetList<T>();
			return FillWith(list);
		}
	}
}
