using System;
using Moq;
using NHibernate;
using NUnit.Framework;

namespace ProSuite.Commons.Orm.NHibernate.Test
{
	[TestFixture]
	public class SessionWrapperDisposeTest
	{
		private Mock<ITransaction> _mockTransaction;
		private Mock<ISession> _mockSession;

		[SetUp]
		public void SetUp()
		{
			_mockTransaction = new Mock<ITransaction>();
			_mockTransaction.Setup(t => t.IsActive).Returns(true);

			_mockSession = new Mock<ISession>();
			_mockSession.Setup(s => s.BeginTransaction()).Returns(_mockTransaction.Object);
		}

		[Test]
		public void Dispose_WhenNotOutermost_IsNoOp()
		{
			var wrapper = new SessionWrapper(_mockSession.Object, isOutermost: false);

			Assert.DoesNotThrow(() => wrapper.Dispose());

			_mockSession.Verify(s => s.BeginTransaction(), Times.Never);
			_mockSession.Verify(s => s.Dispose(), Times.Never);
			_mockSession.Verify(s => s.Clear(), Times.Never);
			_mockTransaction.Verify(t => t.Commit(), Times.Never);
		}

		[Test]
		public void Dispose_WhenTransactionAlreadyInactive_SkipsCommitAndCleansUp()
		{
			_mockTransaction.Setup(t => t.IsActive).Returns(false);
			var wrapper = new SessionWrapper(_mockSession.Object, isOutermost: true);

			Assert.DoesNotThrow(() => wrapper.Dispose());

			_mockTransaction.Verify(t => t.Commit(), Times.Never);
			_mockTransaction.Verify(t => t.Dispose(), Times.Once);
			_mockSession.Verify(s => s.Dispose(), Times.Once);
		}

		/// <summary>
		/// When business logic returns normally but the deferred SQL flush
		/// fails during Commit() (e.g. ORA-00942), the exception must propagate so
		/// callers can react and clean up.
		/// </summary>
		[Test]
		public void Dispose_WhenCommitFails_NoPendingException_PropagatesCommitException()
		{
			var commitException = new Exception("ORA-00942: table or view does not exist");
			_mockTransaction.Setup(t => t.Commit()).Throws(commitException);

			var wrapper = new SessionWrapper(_mockSession.Object, isOutermost: true);

			Exception thrown = Assert.Throws<Exception>(() => wrapper.Dispose());

			Assert.That(thrown, Is.SameAs(commitException));
			_mockSession.Verify(s => s.Clear(), Times.Once);
			// Cleanup in finally must still run despite the exception
			_mockTransaction.Verify(t => t.Dispose(), Times.Once);
			_mockSession.Verify(s => s.Dispose(), Times.Once);
		}

		/// <summary>
		/// Known limitation of the current approach: when an exception is already
		/// in flight when Dispose() is called and Commit() also throws, C# replaces the
		/// in-flight exception with the one from the finally block — the original is lost.
		///
		/// This covers three cases from NHibernateUnitOfWork.Do():
		///   1. Procedure throws + Rollback() also fails → rollback exception in flight,
		///      IsActive still true → Commit attempted → rollback exception masked.
		///   2. AssertRequiredContext throws (before try/catch) → InvalidOperationException
		///      in flight, transaction active with no work → Commit masked.
		///   3. ReattachState throws (before try/catch) → NHibernate exception masked.
		/// </summary>
		[Test]
		public void Dispose_WhenPendingExceptionAndCommitAlsoFails_OriginalExceptionIsMasked()
		{
			var originalException = new InvalidOperationException(
				"Could be: failed Rollback / AssertRequiredContext / ReattachState");
			var commitException = new Exception("ORA-00942 during commit");
			_mockTransaction.Setup(t => t.Commit()).Throws(commitException);

			Exception observed;
			try
			{
				using (new SessionWrapper(_mockSession.Object, isOutermost: true))
				{
					throw originalException;
				}
			}
			catch (Exception e)
			{
				observed = e;
			}

			// Desired behaviour would be: original exception preserved.
			// Current behaviour: commit exception from Dispose() masks the original.
			// This test documents the known limitation — fix by not throwing from Dispose()
			// when an exception is already in flight (e.g. check Marshal.GetExceptionPointers()).
			Assert.That(observed, Is.Not.SameAs(originalException),
			            "Original in-flight exception is lost — known limitation");
			Assert.That(observed, Is.SameAs(commitException),
			            "Commit exception from Dispose() surfaces instead");
		}

		/// <summary>
		/// CURRENTLY FAILING — describes the desired behaviour.
		/// When an exception is already in flight and Commit() also throws,
		/// the original exception should be preserved and the commit exception suppressed.
		/// Requires Dispose() to detect a pending exception before attempting to throw.
		/// </summary>
		[Test]
		[Ignore("Currently fails due to C# exception handling semantics; requires code change to pass")]
		public void Dispose_WhenPendingExceptionAndCommitAlsoFails_ShouldPreserveOriginalException()
		{
			var originalException = new InvalidOperationException("Original business logic error");
			var commitException = new Exception("ORA-00942 during commit");
			_mockTransaction.Setup(t => t.Commit()).Throws(commitException);

			Exception observed;
			try
			{
				using (new SessionWrapper(_mockSession.Object, isOutermost: true))
				{
					throw originalException;
				}
			}
			catch (Exception e)
			{
				observed = e;
			}

			// Currently FAILS: observed == commitException (original is masked by throw; in Dispose)
			// To pass: Dispose() must suppress the commit exception when an exception is in flight
			Assert.That(observed, Is.SameAs(originalException));
		}
	}
}
