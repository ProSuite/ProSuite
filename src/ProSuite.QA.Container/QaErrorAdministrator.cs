using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Container
{
	public class QaErrorAdministrator
	{
		private readonly bool _process = true; // variable for debugging

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private SortedDictionary<QaError, object> _sortedQaErrors;

		[CanBeNull] private Dictionary<ITest, int> _testIds;

		/// <summary>
		/// Initializes a new instance of the <see cref="QaErrorAdministrator"/> class.
		/// </summary>
		public QaErrorAdministrator()
		{
			_sortedQaErrors = new SortedDictionary<QaError, object>(new ErrorComparer(this));
		}

		public int Count => _sortedQaErrors.Count;

		public IEnumerable<QaError> Errors => _sortedQaErrors.Keys;

		public bool Exists(QaError args)
		{
			return _sortedQaErrors.ContainsKey(args);
		}

		public void Remove(QaError args)
		{
			_sortedQaErrors.Remove(args);
		}

		public bool IsDuplicate(QaError qaError)
		{
			if (! _process || _sortedQaErrors.ContainsKey(qaError))
			{
				qaError.Duplicate = true;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Add error to error list, if no identical exists
		/// </summary>
		/// <param name="qaError"></param>
		/// <param name="isKnonwnNotDuplicate"></param>
		/// <returns>true if added, otherwise false</returns>
		public bool Add([NotNull] QaError qaError, bool isKnonwnNotDuplicate = false)
		{
			if (! isKnonwnNotDuplicate)
			{
				if (IsDuplicate(qaError))
				{
					return false;
				}
			}

			_sortedQaErrors.Add(qaError, null);
			return true;
		}

		/// <summary>
		/// remove all entries from list where error has no geometry or
		/// bounding box of geometry is lower then (xMax, yMax)
		/// </summary>
		public void Clear(double xMax, double yMax)
		{
			Stopwatch watch = _msg.DebugStartTiming();

			int origCount = _sortedQaErrors.Count;

			var newList = new SortedDictionary<QaError, object>(new ErrorComparer(this));

			foreach (QaError qaError in _sortedQaErrors.Keys)
			{
				if (qaError.IsProcessed(xMax, yMax))
				{
					continue;
				}

				try
				{
					newList.Add(qaError, null);
				}
				catch (Exception e)
				{
					_msg.DebugFormat("Duplicate QaError: {0} ({1})", qaError, e.Message);
				}
			}

			int newCount = newList.Count;

			_sortedQaErrors = newList;

			_msg.DebugStopTiming(watch,
			                     "Cleared errors < {0},{1}: original count {2:N0}, remaining count {3:N0}",
			                     xMax, yMax, origCount, newCount);
		}

		public void Clear()
		{
			_sortedQaErrors.Clear();
		}

		private int GetTestId([NotNull] ITest test)
		{
			if (_testIds == null)
			{
				_testIds = new Dictionary<ITest, int>();
			}

			int testId;
			if (! _testIds.TryGetValue(test, out testId))
			{
				testId = _testIds.Count;

				_testIds.Add(test, testId);
			}

			return testId;
		}

		#region nested classes

		private class ErrorComparer : IComparer<QaError>
		{
			[NotNull] private readonly QaErrorAdministrator _qaErrorAdministrator;

			public ErrorComparer([NotNull] QaErrorAdministrator qaErrorAdministrator)
			{
				_qaErrorAdministrator = qaErrorAdministrator;
			}

			#region IComparer<QaError> Members

			public int Compare(QaError error0, QaError error1)
			{
				Assert.ArgumentNotNull(error0, nameof(error0));
				Assert.ArgumentNotNull(error1, nameof(error1));

				var test0 = error0.Test as ContainerTest;
				var test1 = error1.Test as ContainerTest;

				if (test0 == null)
				{
					if (test1 != null)
					{
						return -1;
					}

					// both are non-container tests
					const bool compareIndividualInvolvedRows = true;
					return error0.Test == error1.Test
						       ? TestUtils.CompareQaErrors(error0,
						                                   error1,
						                                   compareIndividualInvolvedRows)
						       : _qaErrorAdministrator.GetTestId(error0.Test) -
						         _qaErrorAdministrator.GetTestId(error1.Test);
				}

				if (test1 == null)
				{
					return 1;
				}

				if (test0 == test1)
				{
					return test0.Compare(error0, error1);
				}

				return _qaErrorAdministrator.GetTestId(error0.Test) -
				       _qaErrorAdministrator.GetTestId(error1.Test);
			}

			#endregion
		}

		#endregion
	}
}
