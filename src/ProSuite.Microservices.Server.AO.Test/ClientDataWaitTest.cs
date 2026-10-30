using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using ProSuite.Microservices.Server.AO.QA;

namespace ProSuite.Microservices.Server.AO.Test
{
	public class ClientDataWaitTest
	{
		[Test]
		public void AwaitClientDataTimesOutInWallClockTime()
		{
			// Counting Sleep(20) iterations took ~1.5x the configured time at the default timer
			// resolution (3.1 s instead of 2 s here).
			var neverAnswered = new TaskCompletionSource<string>();
			var watch = Stopwatch.StartNew();

			Assert.Throws<TimeoutException>(() => QualityVerificationGrpcImpl.AwaitClientData(
				                                neverAnswered.Task, 2000));

			Assert.That(watch.ElapsedMilliseconds, Is.InRange(2000, 2500));
		}

		[Test]
		public void AwaitClientDataReturnsAnswer()
		{
			Assert.AreEqual("data",
			                QualityVerificationGrpcImpl.AwaitClientData(
				                Task.FromResult("data"), 2000));
		}

		[Test]
		public void CanClassifyClientDataUnavailable()
		{
			Assert.IsTrue(QualityVerificationGrpcImpl.IsClientDataUnavailable(new IOException()));
			Assert.IsTrue(
				QualityVerificationGrpcImpl.IsClientDataUnavailable(new TimeoutException()));
			Assert.IsTrue(QualityVerificationGrpcImpl.IsClientDataUnavailable(
				              new AggregateException(new IOException())));

			Assert.IsFalse(QualityVerificationGrpcImpl.IsClientDataUnavailable(
				               new InvalidOperationException()));
			Assert.IsFalse(QualityVerificationGrpcImpl.IsClientDataUnavailable(
				               new AggregateException(new IOException(),
				                                      new InvalidOperationException())));
		}
	}
}
