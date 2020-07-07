using System;
using System.Collections;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.ScreenBinding.Lists;

namespace ProSuite.Commons.UI.ScreenBinding.Elements
{
	public interface IListElement : IScreenElement
	{
		string DisplayValue { get; }

		void FillWithList([NotNull] IPicklist list);

		void FillWithList([NotNull] string[] strings);

		void FillWith<T>(params T[] items) where T : IComparable;

		[NotNull]
		IList GetListOfItems();

		void SelectByDisplay(string display);

		void FillWithEnum<T>() where T : struct, IComparable, IFormattable;
	}
}
