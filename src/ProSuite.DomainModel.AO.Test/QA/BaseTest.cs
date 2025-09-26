using System;
using System.Collections.Generic;
using System.ComponentModel;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.Test.QA
{
	public class BaseTest : ContainerTest
	{
		private IList<int> _intList;
		private const double _defaultNumber = 2.5;
		private const double _defaultNumber2 = 1.2345;
		private const double _newPropertyDefault = 789.123;

		[UsedImplicitly]
		public BaseTest(IReadOnlyTable table) :
			base(table)
		{
			Number = _defaultNumber;
			Number2 = _defaultNumber2;
			NewProperty = _newPropertyDefault;
		}

		[Description("BaseTest Description")]
		[UsedImplicitly]
		public BaseTest([Description("Table Description")] IReadOnlyTable table,
		                [Description("Number Description")] double number) :
			this(table)
		{
			Number = number;
		}

		[Description("BaseTest Description - constructor 2")]
		[UsedImplicitly]
		public BaseTest([Description("Table Description")] IReadOnlyTable table,
		                [Description("Number Description")] double number,
		                [Description("Int list description")] IList<int> intList) :
			this(table, number)
		{
			_intList = intList;
		}

		[UsedImplicitly]
		public BaseTest(IReadOnlyTable table, string Format) :
			this(table) { }

		[UsedImplicitly]
		public BaseTest(IReadOnlyTable table, ITable Check) :
			this(table) { }

		[Description("Format Property Description")]
		[TestParameter]
		[UsedImplicitly]
		public string Format { get; set; }

		[Description("Number Property Description")]
		[TestParameter(_defaultNumber)]
		[UsedImplicitly]
		public double Number { get; set; }

		[Description("Number2 Property Description")]
		[TestParameter(_defaultNumber2)]
		[UsedImplicitly]
		public double Number2 { get; set; }

		[Description("Check Property Description")]
		[TestParameter]
		[UsedImplicitly]
		public ITable Check { get; set; }

		[Description("Integer List Description")]
		[TestParameter]
		[UsedImplicitly]
		public IList<int> IntList
		{
			get { return _intList; }
			set { _intList = value; }
		}

		[Obsolete]
		[Description("Obsolete Property Description")]
		[TestParameter]
		[UsedImplicitly]
		public string Obsolete { get; set; }

		[Description("New double property")]
		[TestParameter(_newPropertyDefault)]
		[UsedImplicitly]
		public double NewProperty { get; set; }

		protected override int ExecuteCore(IReadOnlyRow row, int tableIndex)
		{
			return 0;
		}
	}
}
