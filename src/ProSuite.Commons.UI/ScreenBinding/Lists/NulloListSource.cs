using System;

namespace ProSuite.Commons.UI.ScreenBinding.Lists
{
	public class NulloListSource : IListSource
	{
		#region IListSource Members

		public IPicklist GetList<T>()
		{
			return new Picklist<IComparable>();
		}

		public IPicklist GetList(string key)
		{
			return new Picklist<IComparable>();
		}

		#endregion
	}
}
