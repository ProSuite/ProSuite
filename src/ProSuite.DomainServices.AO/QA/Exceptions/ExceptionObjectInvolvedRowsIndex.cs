using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionObjectInvolvedRowsIndex
	{
		private readonly Predicate<string> _excludeTableFromKey;

		[NotNull] private readonly IDictionary<string, List<ExceptionObject>> _index =
			new Dictionary<string, List<ExceptionObject>>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly IList<ExceptionObject> _emptyList =
			new List<ExceptionObject>();

		public ExceptionObjectInvolvedRowsIndex(
			[CanBeNull] Predicate<string> excludeTableFromKey)
		{
			_excludeTableFromKey = excludeTableFromKey;
		}

		public void Add([NotNull] ExceptionObject exceptionObject)
		{
			string key = ExceptionObjectUtils.GetKey(exceptionObject.InvolvedTables,
			                                         _excludeTableFromKey);

			List<ExceptionObject> exceptionObjects;
			if (! _index.TryGetValue(key, out exceptionObjects))
			{
				exceptionObjects = new List<ExceptionObject>();
				_index.Add(key, exceptionObjects);
			}

			exceptionObjects.Add(exceptionObject);
		}

		[NotNull]
		public IEnumerable<ExceptionObject> Search([NotNull] QaError qaError)
		{
			string key = ExceptionObjectUtils.GetKey(qaError.InvolvedRows,
			                                         _excludeTableFromKey);

			List<ExceptionObject> exceptionObjects;
			return _index.TryGetValue(key, out exceptionObjects)
				       ? exceptionObjects
				       : _emptyList;
		}

		[NotNull]
		public IEnumerable<ExceptionObject> Search(
			[NotNull] ExceptionObject exceptionObject)
		{
			string key = ExceptionObjectUtils.GetKey(exceptionObject.InvolvedTables,
			                                         _excludeTableFromKey);

			List<ExceptionObject> exceptionObjects;
			return _index.TryGetValue(key, out exceptionObjects)
				       ? exceptionObjects
				       : _emptyList;
		}
	}
}
