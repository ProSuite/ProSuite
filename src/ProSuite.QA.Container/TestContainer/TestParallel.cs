using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProSuite.QA.Container.TestContainer
{
	public class TestParallel
	{
		public void Execute(IEnumerable<ContainerTest> tests)
		{
			TestContainer cnt = new TestContainer();
			foreach (ContainerTest test in tests)
			{
				cnt.AddTest(test);
				test.SetDataContainer(null);
			}

			int maxTasks = 10;
			List<Task> tasks = new List<Task>();
			foreach (var tile in EnumTiles())
			{
				Task<int> t = new Task<int>(() =>
				{
					LoadData(tile);
					cnt.QaError += Cnt_QaError; // handled duplicate errors
					cnt.Execute(tile.CurrentEnvelope);
					//foreach (TestRow testRow in new List<TestRow>())
					//{
					//	foreach (ContainerTest te in testRow.ApplicableTests)
					//	{
					//		testRow.DataReference.Execute(te, 0, out bool applicable);
					//	}
					//}
					cnt.QaError -= Cnt_QaError;
					return 0;
				});
				tasks.Add(t);

				if (tasks.Count >= maxTasks)
				{
					Task<Task> cmplTask = Task.WhenAny(tasks);

					Task toRemove = cmplTask.Result;
					List<Task> remainingTasks = new List<Task>();
					foreach (Task task in tasks)
					{
						if (task != toRemove)
						{
							remainingTasks.Add(task);
						}
					}

					tasks = remainingTasks;
				}
			}

			Task.WhenAll(tasks);
		}

		private void Cnt_QaError(object sender, QaErrorEventArgs e)
		{
			throw new System.NotImplementedException();
		}

		private IEnumerable<TileInfo> EnumTiles()
		{
			yield break;
		}

		private void LoadData(TileInfo tile)
		{

		}
	}
}
