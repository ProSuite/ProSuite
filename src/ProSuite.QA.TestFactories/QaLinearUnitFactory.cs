using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geometry;
using ProSuite.QA.Container;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	public abstract class QaLinearUnitFactory : DefaultTestFactory
	{
		#region AreaUnits enum

		public enum AreaUnits
		{
			km2,
			m2,
			dm2,
			cm2,
			mm2
		}

		#endregion

		#region LengthUnits enum

		public enum LengthUnits
		{
			km,
			m,
			dm,
			cm,
			mm
		}

		#endregion

		private static IUnitConverter _unitConverter;
		private readonly Type _unitsType;
		private IList<TestParameter> _parameters;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="testType"></param>
		/// <param name="constructorId"></param>
		/// <param name="units">LengthUnits or AreaUnits</param>
		protected QaLinearUnitFactory([NotNull] Type testType,
		                              int constructorId,
		                              [NotNull] Type units)
			: base(testType, constructorId)
		{
			if (units != typeof(LengthUnits) && units != typeof(AreaUnits))
			{
				throw new ArgumentException("units must be " + typeof(LengthUnits) + " or " +
				                            typeof(AreaUnits));
			}

			_unitsType = units;
		}

		protected override IList<TestParameter> CreateParameters()
		{
			if (_parameters == null)
			{
				var list = new List<TestParameter>(base.CreateParameters())
				           {
					           new TestParameter("units", _unitsType,
					                             DocStrings.QaLinearUnitFactory_units),
					           new TestParameter("referenceScale", typeof(Scale),
					                             DocStrings.QaLinearUnitFactory_referenceScale)
				           };

				_parameters = new ReadOnlyCollection<TestParameter>(list);
			}

			return _parameters;
		}

		protected void SetParameters(IList<TestParameter> parameters)
		{
			_parameters = new ReadOnlyCollection<TestParameter>(parameters);
		}

		protected override ITest CreateTestInstance(object[] args)
		{
			Type testType = TestType;

			int l = args.Length - 2;
			var constrArgs = new object[l];
			for (var i = 0; i < l; i++)
			{
				constrArgs[i] = args[i];
			}

			var containerTest =
				(ContainerTest) Activator.CreateInstance(testType, constrArgs);
			containerTest.LinearUnits = (esriUnits) args[l];
			containerTest.ReferenceScale = (double) args[l + 1];

			return containerTest;
		}

		protected double UnitFactor(ISpatialReference sr, esriUnits units,
		                            double referenceScale)
		{
			var pc = sr as IProjectedCoordinateSystem;

			if (pc == null)
			{
				return 1;
			}

			if (_unitConverter == null)
			{
				_unitConverter = new UnitConverterClass();
			}

			double f = _unitConverter.ConvertUnits(1, esriUnits.esriMeters, units);

			f = f * pc.CoordinateUnit.MetersPerUnit;
			f = f * referenceScale;

			if (_unitsType == typeof(LengthUnits)) { }
			else if (_unitsType == typeof(AreaUnits))
			{
				f = f * f;
			}
			else
			{
				throw new ArgumentException("Unhandled Unit type " + _unitsType);
			}

			return f;
		}

		protected esriUnits EsriUnits(LengthUnits unit)
		{
			switch (unit)
			{
				case LengthUnits.km:
					return esriUnits.esriKilometers;

				case LengthUnits.m:
					return esriUnits.esriMeters;

				case LengthUnits.dm:
					return esriUnits.esriDecimeters;

				case LengthUnits.cm:
					return esriUnits.esriCentimeters;

				case LengthUnits.mm:
					return esriUnits.esriMillimeters;

				default:
					throw new ArgumentOutOfRangeException("unit", unit,
					                                      string.Format("Unknown length units: {0}",
					                                                    unit));
			}
		}

		protected esriUnits EsriUnits(AreaUnits unit)
		{
			switch (unit)
			{
				case AreaUnits.km2:
					return esriUnits.esriKilometers;

				case AreaUnits.m2:
					return esriUnits.esriMeters;

				case AreaUnits.dm2:
					return esriUnits.esriDecimeters;

				case AreaUnits.cm2:
					return esriUnits.esriCentimeters;

				case AreaUnits.mm2:
					return esriUnits.esriMillimeters;

				default:
					throw new ArgumentOutOfRangeException("unit", unit,
					                                      string.Format("Unknown area units: {0}",
					                                                    unit));
			}
		}

		#region Nested type: Scale

		public class Scale : IFormattable
		{
			private readonly double _scale;

			public Scale()
			{
				_scale = 1;
			}

			public Scale(string name, IFormatProvider provider)
			{
				name = name.Trim();
				if (name.StartsWith("1:"))
				{
					name = name.Substring(2);
					_scale = 1.0 / double.Parse(name, NumberStyles.Any, provider);
				}
				else
				{
					_scale = double.Parse(name, NumberStyles.Any, provider);
					if (_scale > 1.0)
					{
						_scale = 1.0 / _scale;
					}
				}
			}

			public double ScaleValue
			{
				get { return _scale; }
			}

			public override string ToString()
			{
				return "1:" + (1.0 / _scale).ToString("N0");
			}

			public string ToString(string format, IFormatProvider formatProvider)
			{
				string scaleFormat =
					StringUtils.IsNullOrEmptyOrBlank(format)
						? "1:{0}"
						: "1:{0:" + format + "}";
				return string.Format(formatProvider, scaleFormat, 1.0 / _scale);
			}
		}

		#endregion
	}
}
