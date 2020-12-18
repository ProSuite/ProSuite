using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Test.TestRunners
{
	public abstract class QaTestRunnerBase
	{
		private readonly List<QaError> _errors = new List<QaError>();
		private readonly List<IGeometry> _errorGeometries = new List<IGeometry>();

		protected QaTestRunnerBase() { }

		public bool LogErrors { get; set; } = true;

		public void ClearErrors()
		{
			_errors.Clear();
		}

		public bool KeepGeometry { get; set; }

		[NotNull]
		public IList<QaError> Errors => _errors;

		[NotNull]
		public IList<IGeometry> ErrorGeometries => _errorGeometries;

		protected void ProcessError(object sender, QaErrorEventArgs e)
		{
			QaError error = e.QaError;

			_errors.Add(error);

			if (KeepGeometry)
			{
				_errorGeometries.Add(error.Geometry);
			}

			if (LogErrors)
			{
				TestRunnerUtils.PrintError(error);
			}
		}

		public abstract int Execute();
	}
}
