using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Progress;

namespace ProSuite.Commons.Test.Progress
{
	[TestFixture]
	public class CompositeProgressFeedbackTest
	{
		[Test]
		public void Every_member_fans_out_to_all_children()
		{
			var spy1 = new SpyProgressFeedback();
			var spy2 = new SpyProgressFeedback();
			var composite = new CompositeProgressFeedback(spy1, spy2);

			composite.SetRange(0, 10);
			composite.Advance();
			composite.Advance("tile 1");
			composite.Advance("tile {0}", 2);
			composite.SetComplete();
			composite.SetComplete("done");
			composite.SetComplete("done {0}", "twice");
			composite.ShowMessage("hello", LogLevel.Warn);
			composite.ShowMessage("formatted {0}", "hello");
			composite.HideMessage();
			composite.Dispose();

			var expected = new[]
			               {
				               "SetRange(0, 10)",
				               "Advance()",
				               "Advance(tile 1)",
				               "Advance(tile 2)",
				               "SetComplete()",
				               "SetComplete(done)",
				               "SetComplete(done twice)",
				               "ShowMessage(hello, Warn)",
				               "ShowMessage(formatted hello, Info)",
				               "HideMessage()",
				               "Dispose()"
			               };

			Assert.That(spy1.Calls, Is.EqualTo(expected));
			Assert.That(spy2.Calls, Is.EqualTo(expected));
		}

		[Test]
		public void Children_are_called_in_registration_order()
		{
			var log = new List<string>();
			var first = new SpyProgressFeedback(log, "first");
			var second = new SpyProgressFeedback(log, "second");
			var composite = new CompositeProgressFeedback(first, second);

			composite.ShowMessage("hello");

			Assert.That(log, Is.EqualTo(new[]
			                            {
				                            "first:ShowMessage(hello, Info)",
				                            "second:ShowMessage(hello, Info)"
			                            }));
		}

		[Test]
		public void A_throwing_child_does_not_propagate_and_does_not_block_remaining_children()
		{
			var throwing = new ThrowingProgressFeedback();
			var spy = new SpyProgressFeedback();
			var composite = new CompositeProgressFeedback(throwing, spy);

			Assert.DoesNotThrow(() => composite.ShowMessage("hello"));

			Assert.That(spy.Calls, Is.EqualTo(new[] { "ShowMessage(hello, Info)" }));
		}

		[Test]
		public void A_throwing_child_does_not_block_the_remaining_children_from_being_disposed()
		{
			var throwing = new ThrowingProgressFeedback();
			var spy = new SpyProgressFeedback();
			var composite = new CompositeProgressFeedback(throwing, spy);

			Assert.DoesNotThrow(() => composite.Dispose());

			Assert.That(spy.Calls, Is.EqualTo(new[] { "Dispose()" }));
		}

		[Test]
		public void Property_getters_reflect_the_last_value_set_through_the_composite()
		{
			var composite = new CompositeProgressFeedback(new SpyProgressFeedback());

			composite.MinimumValue = 5;
			composite.MaximumValue = 50;
			composite.CurrentValue = 20;
			composite.StepSize = 2;

			Assert.That(composite.MinimumValue, Is.EqualTo(5));
			Assert.That(composite.MaximumValue, Is.EqualTo(50));
			Assert.That(composite.CurrentValue, Is.EqualTo(20));
			Assert.That(composite.StepSize, Is.EqualTo(2));
		}

		/// <summary>
		/// Records every call it receives, both locally (<see cref="Calls"/>) and, if given a
		/// shared log and a name, into that shared log — used to verify cross-child call order.
		/// </summary>
		private class SpyProgressFeedback : IProgressFeedback
		{
			private readonly List<string> _sharedLog;
			private readonly string _name;

			public SpyProgressFeedback(List<string> sharedLog = null, string name = null)
			{
				_sharedLog = sharedLog;
				_name = name;
			}

			public List<string> Calls { get; } = new List<string>();

			public void SetRange(int minimumValue, int maximumValue) =>
				Record($"SetRange({minimumValue}, {maximumValue})");

			public void Advance() => Record("Advance()");

			public void Advance(string message) => Record($"Advance({message})");

			public void Advance(string format, params object[] args) =>
				Advance(string.Format(format, args));

			public void SetComplete() => Record("SetComplete()");

			public void SetComplete(string message) => Record($"SetComplete({message})");

			public void SetComplete(string format, params object[] args) =>
				SetComplete(string.Format(format, args));

			public void ShowMessage(string message, LogLevel level = LogLevel.Info) =>
				Record($"ShowMessage({message}, {level})");

			public void ShowMessage(string format, params object[] args) =>
				ShowMessage(string.Format(format, args));

			public void HideMessage() => Record("HideMessage()");

			public int CurrentValue { get; set; }
			public int MinimumValue { get; set; }
			public int MaximumValue { get; set; }
			public int StepSize { get; set; }

			public void Dispose() => Record("Dispose()");

			private void Record(string call)
			{
				Calls.Add(call);
				_sharedLog?.Add($"{_name}:{call}");
			}
		}

		/// <summary>Every member throws, to verify the composite's fault isolation.</summary>
		private class ThrowingProgressFeedback : IProgressFeedback
		{
			public void SetRange(int minimumValue, int maximumValue) =>
				throw new InvalidOperationException();

			public void Advance() => throw new InvalidOperationException();

			public void Advance(string message) => throw new InvalidOperationException();

			public void Advance(string format, params object[] args) =>
				throw new InvalidOperationException();

			public void SetComplete() => throw new InvalidOperationException();

			public void SetComplete(string message) => throw new InvalidOperationException();

			public void SetComplete(string format, params object[] args) =>
				throw new InvalidOperationException();

			public void ShowMessage(string message, LogLevel level = LogLevel.Info) =>
				throw new InvalidOperationException();

			public void ShowMessage(string format, params object[] args) =>
				throw new InvalidOperationException();

			public void HideMessage() => throw new InvalidOperationException();

			public int CurrentValue { get; set; }
			public int MinimumValue { get; set; }
			public int MaximumValue { get; set; }
			public int StepSize { get; set; }

			public void Dispose() => throw new InvalidOperationException();
		}
	}
}
