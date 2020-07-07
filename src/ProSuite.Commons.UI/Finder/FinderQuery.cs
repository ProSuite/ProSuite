using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.Finder
{
	public class FinderQuery<T> where T : class
	{
		[NotNull] private readonly Func<IList<T>> _executeQuery;

		public FinderQuery([NotNull] string name, [NotNull] string id,
		                   [NotNull] Func<IList<T>> executeQuery)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNullOrEmpty(id, nameof(id));
			Assert.ArgumentNotNull(executeQuery, nameof(executeQuery));

			Name = name;
			Id = id;
			_executeQuery = executeQuery;
		}

		[NotNull]
		public string Name { get; private set; }

		[NotNull]
		public string Id { get; private set; }

		[NotNull]
		public IList<T> GetResult()
		{
			return Assert.NotNull(_executeQuery(), "query returned null");
		}
	}
}
