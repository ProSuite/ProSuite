using System.Collections.Generic;
using System.Collections.ObjectModel;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public class ReadOnlyList<T> : ReadOnlyCollection<T>
	{
		public ReadOnlyList([NotNull] IList<T> list) : base(list) { }

		public IList<T> Inner
		{
			get { return Items; }
		}
	}
}