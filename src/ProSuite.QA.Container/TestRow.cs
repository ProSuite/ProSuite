using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geometry;

namespace ProSuite.QA.Container
{
	public class TestRow
	{
		private readonly bool[] _success;

		/// <summary>
		/// Initializes a new instance of the <see cref="TestRow"/> class.
		/// </summary>
		/// <param name="dataReference">The row.</param>
		/// <param name="box">The box.</param>
		/// <param name="applicableTests">The applicable tests.</param>
		internal TestRow([NotNull] IDataReference dataReference,
		                 [CanBeNull] IBox box,
		                 [NotNull] IList<ContainerTest> applicableTests)
		{
			Assert.ArgumentNotNull(dataReference, nameof(dataReference));
			Assert.ArgumentNotNull(applicableTests, nameof(applicableTests));

			DataReference = dataReference;
			Extent = box;
			ApplicableTests = applicableTests;

			_success = new bool[ApplicableTests.Count];
		}

		[NotNull]
		[CLSCompliant(false)]
		public IList<ContainerTest> ApplicableTests { get; }

		[CLSCompliant(false)]
		[NotNull]
		public IDataReference DataReference { get; }

		[CanBeNull]
		internal IBox Extent { get; }

		[CLSCompliant(false)]
		public void SetSuccess(ContainerTest containerTest, bool success)
		{
			_success[ApplicableTests.IndexOf(containerTest)] = success;
		}
	}
}
