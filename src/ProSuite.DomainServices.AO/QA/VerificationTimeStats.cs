using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Surface;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.QA.Container;
using ProSuite.QA.Container.TestContainer;

namespace ProSuite.DomainServices.AO.QA
{
	public class VerificationTimeStats
	{
		private readonly Dictionary<IDataset, long> _datasetLoadTimes =
			new Dictionary<IDataset, long>();

		private readonly Dictionary<ContainerTest, ContainerTestTimes> _containerTestTimes
			= new Dictionary<ContainerTest, ContainerTestTimes>();

		private readonly Dictionary<ITest, long> _testTicks =
			new Dictionary<ITest, long>();

		private readonly double _ticksPerMilliseconds;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public VerificationTimeStats()
		{
			_ticksPerMilliseconds = Stopwatch.Frequency / 1000.0;
		}

		public void Update([NotNull] ProgressArgs args)
		{
			Assert.ArgumentNotNull(args, nameof(args));

			long dt = args.NettoTicks;

			switch (args.CurrentStep)
			{
				case Step.DataLoaded:
				{
					var ds = (IDataset) args.Tag;
					Add(_datasetLoadTimes, ds, dt);
				}
					break;
				case Step.RasterLoaded:
				{
					IDataset ds = ((RasterReference) args.Tag).RasterDataset;
					Add(_datasetLoadTimes, ds, dt);
				}
					break;
				case Step.TinLoaded:
				{
					var dsr = (TerrainReference) args.Tag;
					var ds = (IDataset) dsr.Dataset;
					Add(_datasetLoadTimes, ds, dt);
				}
					break;
				case Step.ITestProcessed:
					var test = (ITest) args.Tag;
					if (_testTicks.ContainsKey(test))
					{
						_msg.WarnFormat("Test processing time already registered for {0}", test);
					}
					else
					{
						_testTicks.Add(test, dt);
					}

					break;
				case Step.RowProcessed:
				{
					var containerTest = (ContainerTest) args.Tag;
					GetContainerTestTimes(containerTest).RowTicks += dt;
				}
					break;
				case Step.TileCompleted:
				{
					var containerTest = (ContainerTest) args.Tag;
					GetContainerTestTimes(containerTest).TileCompletionTicks += dt;
				}
					break;
			}
		}

		private static void Add<T>(Dictionary<T, long> dictionary, T key, long ticks)
		{
			if (! dictionary.ContainsKey(key))
			{
				dictionary.Add(key, ticks);
			}
			else
			{
				dictionary[key] += ticks;
			}
		}

		[CLSCompliant(false)]
		public bool TryGetTestTime([NotNull] ITest test,
		                           out double milliseconds)
		{
			long ticks;
			if (_testTicks.TryGetValue(test, out ticks))
			{
				milliseconds = GetMilliseconds(ticks);
				return true;
			}

			milliseconds = 0;
			return false;
		}

		private double GetMilliseconds(long ticks)
		{
			return ticks / _ticksPerMilliseconds;
		}

		[CLSCompliant(false)]
		public bool TryGetContainerTestTimes([NotNull] ContainerTest test,
		                                     out double rowMilliseconds,
		                                     out double tileCompletionMilliseconds)
		{
			ContainerTestTimes times;

			if (! _containerTestTimes.TryGetValue(test, out times))
			{
				rowMilliseconds = 0;
				tileCompletionMilliseconds = 0;

				return false;
			}

			rowMilliseconds = GetMilliseconds(times.RowTicks);
			tileCompletionMilliseconds = GetMilliseconds(times.TileCompletionTicks);

			return true;
		}

		[NotNull]
		[CLSCompliant(false)]
		public IEnumerable<KeyValuePair<IDataset, double>> DatasetLoadTimes
		{
			get
			{
				return _datasetLoadTimes.Select(
					pair => new KeyValuePair<IDataset, double>(
						pair.Key, GetMilliseconds(pair.Value)));
			}
		}

		[NotNull]
		private ContainerTestTimes GetContainerTestTimes(
			[NotNull] ContainerTest containerTest)
		{
			ContainerTestTimes result;

			if (! _containerTestTimes.TryGetValue(containerTest, out result))
			{
				result = new ContainerTestTimes();
				_containerTestTimes.Add(containerTest, result);
			}

			return result;
		}

		private class ContainerTestTimes
		{
			public long RowTicks { get; set; }

			public long TileCompletionTicks { get; set; }
		}
	}
}
