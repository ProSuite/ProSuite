using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Container;
using ProSuite.QA.Container.Geometry;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	public abstract class QaAngleFactory : TestFactory
	{
		private IList<TestParameter> _parameters;

		protected override IList<TestParameter> CreateParameters()
		{
			if (_parameters == null)
			{
				var list = new List<TestParameter>
				           {
					           new TestParameter(
						           "featureClasses", typeof(IFeatureClass[]),
						           DocStrings.QaAngleFactory_featureClasses),
					           new TestParameter("limit", typeof(double),
					                             DocStrings.QaAngleFactory_limit),
					           new TestParameter("is3D", typeof(bool),
					                             DocStrings.QaAngleFactory_is3D)
				           };

				_parameters = new ReadOnlyCollection<TestParameter>(list);
			}

			return _parameters;
		}

		protected void SetParameters(IList<TestParameter> parameters)
		{
			_parameters = new ReadOnlyCollection<TestParameter>(parameters);
		}

		protected override object[] Args(IOpenDataset datasetContext,
		                                 IList<TestParameter> testParameters,
		                                 out List<TableConstraint> tableParameters)
		{
			object[] objParams = base.Args(datasetContext, testParameters, out tableParameters);
			if (objParams.Length != 3)
			{
				throw new ArgumentException(string.Format("expected 3 parameter, got {0}",
				                                          objParams.Length));
			}

			// TODO revise: the base class declares IFeatureClass[], but the subclass (QaMinSegAngleFactory) changes that to IFeatureClass.
			//if (objParams[0] is IFeatureClass == false)
			//{
			//    throw new ArgumentException(string.Format("expected IFeatureClass, got {0}",
			//                                              objParams[0].GetType()));
			//}
			if (objParams[1] is double == false)
			{
				throw new ArgumentException(string.Format("expected double, got {0}",
				                                          objParams[1].GetType()));
			}

			if (objParams[2] is bool == false)
			{
				throw new ArgumentException(string.Format("expected bool, got {0}",
				                                          objParams[2].GetType()));
			}

			var objects = new object[3];
			objects[0] = objParams[0];
			objects[1] = (double) objParams[1] * Math.PI / 180.0;
			objects[2] = objParams[2];

			return objects;
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			ContainerTest containerTest = CreateAngleTest(args);
			containerTest.AngleUnit = AngleUnit.Degree;
			return containerTest;
		}

		protected abstract ContainerTest CreateAngleTest(object[] args);
	}
}
