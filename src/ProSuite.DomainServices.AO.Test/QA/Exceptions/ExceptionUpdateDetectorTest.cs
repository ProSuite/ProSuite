using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions;

namespace ProSuite.DomainServices.AO.Test.QA.Exceptions
{
	[TestFixture]
	public class ExceptionUpdateDetectorTest
	{
		[Test]
		public void CanDetectUnchanged()
		{
			var detector = new ExceptionUpdateDetector(
				new[]
				{
					IssueAttribute.ExceptionStatus,
					IssueAttribute.ExceptionNotes
				});

			Guid lineage = Guid.NewGuid();
			var dt1 = new DateTime(2018, 1, 1);
			var dt2 = new DateTime(2018, 1, 2);

			var ex1 = new ManagedExceptionVersion(1, lineage, Guid.NewGuid(),
			                                      "AA", "[AA]", dt1, dt2);
			ex1.SetValue(IssueAttribute.ExceptionStatus, "a");
			ex1.SetValue(IssueAttribute.ExceptionNotes, "aa");

			var ex2 = new ManagedExceptionVersion(2, lineage, Guid.NewGuid(),
			                                      "BB", "[AA],[BB]", dt2, null);
			ex2.SetValue(IssueAttribute.ExceptionStatus, "a");
			ex2.SetValue(IssueAttribute.ExceptionNotes, "bb");

			detector.AddExistingException(ex1);
			detector.AddExistingException(ex2);

			ManagedExceptionVersion update = ex1.Clone(); // no change

			ManagedExceptionVersion merged;
			ManagedExceptionVersion replaced;
			IList<ExceptionAttributeConflict> conflicts;
			Assert.False(detector.HasChange(update, out merged, out replaced, out conflicts));
		}

		[Test]
		public void CanDetectAttributeChange()
		{
			var detector = new ExceptionUpdateDetector(
				new[]
				{
					IssueAttribute.ExceptionStatus,
					IssueAttribute.ExceptionNotes,
					IssueAttribute.ExceptionCategory
				});

			Guid lineage = Guid.NewGuid();
			var dt1 = new DateTime(2018, 1, 1);
			var dt2 = new DateTime(2018, 1, 2);

			var ex1 = new ManagedExceptionVersion(1, lineage, Guid.NewGuid(),
			                                      "AA", "[AA]", dt1, dt2);
			ex1.SetValue(IssueAttribute.ExceptionStatus, "a");
			ex1.SetValue(IssueAttribute.ExceptionNotes, "aa");
			ex1.SetValue(IssueAttribute.ExceptionCategory, "F");

			var ex2 = new ManagedExceptionVersion(2, lineage, Guid.NewGuid(),
			                                      "BB", "[AA],[BB]", dt2, null);
			ex2.SetValue(IssueAttribute.ExceptionStatus, "a");
			ex2.SetValue(IssueAttribute.ExceptionNotes, "bb");
			ex2.SetValue(IssueAttribute.ExceptionCategory, "F");

			detector.AddExistingException(ex1);
			detector.AddExistingException(ex2);

			ManagedExceptionVersion update = ex1.Clone();
			update.SetValue(IssueAttribute.ExceptionCategory, "E"); // no conflict

			ManagedExceptionVersion merged;
			ManagedExceptionVersion replaced;
			IList<ExceptionAttributeConflict> conflicts;
			Assert.True(detector.HasChange(update, out merged, out replaced, out conflicts));
			Assert.AreEqual("E", merged.GetValue(IssueAttribute.ExceptionCategory));
			Assert.AreEqual("bb", merged.GetValue(IssueAttribute.ExceptionNotes));
			Assert.NotNull(replaced);
			Assert.AreEqual("F", replaced.GetValue(IssueAttribute.ExceptionCategory));
			Assert.AreEqual("bb", replaced.GetValue(IssueAttribute.ExceptionNotes));
			Assert.AreEqual(0, conflicts.Count);
		}

		[Test]
		public void CanDeactivate()
		{
			var detector = new ExceptionUpdateDetector(
				new[]
				{
					IssueAttribute.ExceptionStatus,
					IssueAttribute.ExceptionNotes
				});

			Guid lineage = Guid.NewGuid();
			var dt1 = new DateTime(2018, 1, 1);
			var dt2 = new DateTime(2018, 1, 2);

			var ex1 = new ManagedExceptionVersion(1, lineage, Guid.NewGuid(),
			                                      "AA", "[AA]", dt1, dt2);
			ex1.SetValue(IssueAttribute.ExceptionStatus, "a");
			ex1.SetValue(IssueAttribute.ExceptionNotes, "aa");

			var ex2 = new ManagedExceptionVersion(2, lineage, Guid.NewGuid(),
			                                      "BB", "[AA],[BB]", dt2, null);
			ex2.SetValue(IssueAttribute.ExceptionStatus, "a");
			ex2.SetValue(IssueAttribute.ExceptionNotes, "bb");

			detector.AddExistingException(ex1);
			detector.AddExistingException(ex2);

			ManagedExceptionVersion update = ex1.Clone();
			update.SetValue(IssueAttribute.ExceptionStatus, "i");

			ManagedExceptionVersion merged;
			ManagedExceptionVersion replaced;
			IList<ExceptionAttributeConflict> conflicts;
			Assert.True(detector.HasChange(update, out merged, out replaced, out conflicts));

			Assert.NotNull(replaced);
			Assert.AreEqual("Inactive", merged.GetValue(IssueAttribute.ExceptionStatus));
			Assert.AreEqual("bb", merged.GetValue(IssueAttribute.ExceptionNotes));
			Assert.AreEqual("Active", replaced.GetValue(IssueAttribute.ExceptionStatus));
			Assert.AreEqual("bb", replaced.GetValue(IssueAttribute.ExceptionNotes));

			Assert.AreEqual(0, conflicts.Count);
		}

		[Test]
		public void CanDetectConflict()
		{
			var detector = new ExceptionUpdateDetector(
				new[] {IssueAttribute.ExceptionCategory, IssueAttribute.ExceptionNotes});

			Guid lineage = Guid.NewGuid();
			var dt1 = new DateTime(2018, 1, 1);
			var dt2 = new DateTime(2018, 1, 2);
			var dt3 = new DateTime(2018, 1, 3);

			var ex1 =
				new ManagedExceptionVersion(1, lineage, Guid.NewGuid(), "AA", "[AA]", dt1, dt2);
			ex1.SetValue(IssueAttribute.ExceptionCategory, "X");
			ex1.SetValue(IssueAttribute.ExceptionNotes, "aa");

			var ex2 =
				new ManagedExceptionVersion(2, lineage, Guid.NewGuid(), "BB", "[AA],[BB]", dt2,
				                            dt3);
			ex2.SetValue(IssueAttribute.ExceptionCategory, "Y");
			ex2.SetValue(IssueAttribute.ExceptionNotes, "bb");

			var ex3 =
				new ManagedExceptionVersion(3, lineage, Guid.NewGuid(), "BB", "[AA]#[BB]", dt3,
				                            null);
			ex3.SetValue(IssueAttribute.ExceptionCategory, "Y");
			ex3.SetValue(IssueAttribute.ExceptionNotes, "cc");

			detector.AddExistingException(ex1);
			detector.AddExistingException(ex2);
			detector.AddExistingException(ex3);

			ManagedExceptionVersion update = ex1.Clone();
			update.SetValue(IssueAttribute.ExceptionCategory, "UPDATE");

			ManagedExceptionVersion merged;
			ManagedExceptionVersion replaced;
			IList<ExceptionAttributeConflict> conflicts;
			Assert.True(detector.HasChange(update, out merged, out replaced, out conflicts));

			Assert.NotNull(replaced);
			Assert.AreEqual("UPDATE", merged.GetValue(IssueAttribute.ExceptionCategory));
			Assert.AreEqual("cc", merged.GetValue(IssueAttribute.ExceptionNotes));

			Assert.AreEqual("Y", replaced.GetValue(IssueAttribute.ExceptionCategory));
			Assert.AreEqual("cc", replaced.GetValue(IssueAttribute.ExceptionNotes));

			Assert.AreEqual(1, conflicts.Count);
			Assert.AreEqual("Y", conflicts[0].CurrentValue);
			Assert.AreEqual("X", conflicts[0].OriginalValue);
			Assert.AreEqual("BB", conflicts[0].CurrentValueOrigin);
			Assert.AreEqual(dt2, conflicts[0].CurrentValueImportDate);
		}
	}
}
