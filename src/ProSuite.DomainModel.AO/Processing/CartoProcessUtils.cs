using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.DomainModel.Core.Processing.Reporting;

namespace ProSuite.DomainModel.AO.Processing
{
	/// <summary>
	/// A container for static utility methods related to GdbProcesses.
	/// </summary>
	public static class CartoProcessUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static IList<PropertyInfo> GetProcessParameters(
			[NotNull] CartoProcessType cartoProcessType)
		{
			Assert.ArgumentNotNull(cartoProcessType, nameof(cartoProcessType));

			Type type = cartoProcessType.CartoProcessClassDescriptor.GetInstanceType();

			return GdbProcessUtils.GetProcessParameters(type);
		}

		[NotNull]
		public static IGdbProcess CreateGdbProcess([NotNull] CartoProcess template)
		{
			Assert.ArgumentNotNull(template, nameof(template));

			CartoProcessType cartoProcessType = template.CartoProcessType;
			Type t = cartoProcessType.CartoProcessClassDescriptor.GetInstanceType();
			var process = (IGdbProcess) Activator.CreateInstance(t);
			Assert.NotNull(process, "process");

			process.Name = template.Name;
			process.Description = template.Description;

			Type processType = process.GetType();
			foreach (CartoProcessParameter parameter in template.Parameters)
			{
				PropertyInfo propInfo = processType.GetProperty(parameter.Name);
				if (propInfo == null)
				{
					_msg.WarnFormat("GdbProcess '{0}' has no property '{1}'",
					                processType.Name, parameter.Name);
					continue;
				}

				// Parse numbers using invariant culture!
				CultureInfo invariant = CultureInfo.InvariantCulture;
				Type propertyType = propInfo.PropertyType;

				if (propertyType == typeof(bool))
				{
					bool value = GetBoolean(parameter);
					propInfo.SetValue(process, value, null);
				}
				else if (propertyType == typeof(int))
				{
					int value = GetInt32(parameter, invariant);
					propInfo.SetValue(process, value, null);
				}
				else if (propertyType == typeof(double))
				{
					double value = GetDouble(parameter, invariant);
					propInfo.SetValue(process, value, null);
				}
				else if (propertyType == typeof(string))
				{
					propInfo.SetValue(process, parameter.Value, null);
				}
				else if (typeof(Enum).IsAssignableFrom(propertyType))
				{
					object value = GetEnumValue(parameter, propertyType);
					propInfo.SetValue(process, value, null);
				}
				else if (propertyType == typeof(ProcessDatasetName))
				{
					ProcessDatasetName value = GetProcessDatasetName(parameter, template.Model);
					propInfo.SetValue(process, value, null);
				}

				// TODO Handle other types!
			}

			return process;
		}

		[NotNull]
		public static IGroupGdbProcess CreateGroupGdbProcess(
			[NotNull] CartoProcessGroup template,
			[NotNull] DdxModel model,
			[NotNull] IList<IGdbProcess> processList)
		{
			Assert.ArgumentNotNull(template, nameof(template));
			Assert.ArgumentNotNull(model, nameof(model));
			Assert.ArgumentNotNull(processList, nameof(processList));

			ClassDescriptor classDescriptor =
				template.AssociatedGroupProcessType.CartoProcessClassDescriptor;

			Type t = classDescriptor.GetInstanceType();
			Type groupGdbType = typeof(IGroupGdbProcess);

			bool isGroupGdbProcess = groupGdbType.IsAssignableFrom(t);
			Assert.True(isGroupGdbProcess,
			            "ClassDescriptor {0} is not for a IGroupGdbProcess",
			            classDescriptor);

			var ctorArgTypes = new[] {typeof(IList<IGdbProcess>)};
			ConstructorInfo ctor = t.GetConstructor(ctorArgTypes);
			Assert.True(ctor != null && ctor.IsPublic,
			            "Group GdbProcess must provide a public constructor with signature (IList<IGdbProcess>)");

			var process = (IGroupGdbProcess) Activator.CreateInstance(t, processList);

			process.Name = template.Name;
			process.Description = template.Description;

			return process;
		}

		public static void WriteProcessReport(
			[NotNull] IList<CartoProcessType> cartoProcessTypes,
			[NotNull] string htmlFileName)
		{
			Assert.ArgumentNotNull(cartoProcessTypes, nameof(cartoProcessTypes));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			using (Stream stream = new FileStream(htmlFileName, FileMode.Create))
			{
				IProcessReportBuilder builder =
					new HtmlProcessReportBuilder("Registered Process Types");

				builder.IncludeObsolete = true;
				builder.IncludeAssemblyInfo = true;

				foreach (CartoProcessType cartoProcessType in cartoProcessTypes)
				{
					Type processType =
						cartoProcessType.CartoProcessClassDescriptor.GetInstanceType();

					string name = cartoProcessType.Name;
					string description = cartoProcessType.Description;

					builder.AddProcessType(processType, name, description);
				}

				builder.WriteReport(stream);
			}
		}

		#region Private utils

		private static bool GetBoolean(CartoProcessParameter parameter)
		{
			if (bool.TryParse(parameter.Value, out var value))
			{
				return value;
			}

			throw new InvalidConfigurationException(
				$"Value \"{parameter.Value}\" is not valid for parameter {parameter.Name}");
		}

		private static int GetInt32(CartoProcessParameter parameter, CultureInfo culture)
		{
			if (int.TryParse(parameter.Value, NumberStyles.Integer, culture, out var value))
			{
				return value;
			}

			throw new InvalidConfigurationException(
				$"Value \"{parameter.Value}\" is not valid for parameter {parameter.Name}");
		}

		private static double GetDouble(CartoProcessParameter parameter, CultureInfo culture)
		{
			if (double.TryParse(parameter.Value, NumberStyles.Float, culture, out var value))
			{
				return value;
			}

			throw new InvalidConfigurationException(
				$"Value \"{parameter.Value}\" is not valid for parameter {parameter.Name}");
		}

		private static object GetEnumValue(CartoProcessParameter parameter, Type enumType)
		{
			try
			{
				const bool ignoreCase = true;
				return Enum.Parse(enumType, parameter.Value, ignoreCase);
			}
			catch (Exception ex)
			{
				throw new InvalidConfigurationException(
					$"Value \"{parameter.Value}\" is not valid for parameter {parameter.Name}", ex);
			}
		}

		private static ProcessDatasetName GetProcessDatasetName(
			CartoProcessParameter parameter, DdxModel model)
		{
			// TODO Presently, we cannot distinguish between optional and required parameters
			if (string.IsNullOrEmpty(parameter.Value))
			{
				return null;
			}

			var dataset = ProcessDatasetName.TryCreate(model, parameter.Value, out var message);

			if (dataset == null)
			{
				throw new InvalidConfigurationException(
					$"Parameter {parameter.Name} is invalid: {message}");
			}

			return dataset;
		}

		#endregion
	}
}
