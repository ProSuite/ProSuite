using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.Processing.Reporting;
using ProSuite.DomainModel.Core.Processing;

namespace ProSuite.DomainModel.AO.Processing
{
	public static class GdbProcessUtils
	{
		public static bool IsGdbProcessType([NotNull] Type candidateType)
		{
			Assert.ArgumentNotNull(candidateType, nameof(candidateType));

			Type testType = typeof(IGdbProcess);

			return testType.IsAssignableFrom(candidateType) &&
			       candidateType.IsPublic && ! candidateType.IsAbstract;
		}

		public static bool IsObsolete([NotNull] Type processType, out string message)
		{
			return ReflectionUtils.IsObsolete(processType, out message);
		}

		/// <summary>
		/// Get the process's description (as specified with the Doc attribute).
		/// </summary>
		[NotNull]
		public static string GetProcessDescription(Type processType)
		{
			return ReflectionUtils.GetDescription(processType) ?? string.Empty;
		}

		#region Process parameters

		[NotNull]
		public static IList<PropertyInfo> GetProcessParameters([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (! IsGdbProcessType(type))
			{
				throw new InvalidOperationException(
					$"Type {type.Name} is not a GdbProcess type");
			}

			return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
			           .Where(IsProcessParameter)
			           .OrderBy(GetParameterOrder)
			           .ToList();
		}

		public static bool IsProcessParameter([NotNull] PropertyInfo property)
		{
			Assert.ArgumentNotNull(property, nameof(property));

			// Process parameters are public get/set properties with the Parameter attribute:
			// TODO - How to check for public?
			return property.CanRead && property.CanWrite &&
			       property.IsDefined(typeof(ParameterAttribute), false);
		}

		public static int GetParameterOrder([CanBeNull] ICustomAttributeProvider property)
		{
			ParameterAttribute attr = GetParameterAttribute(property);
			return attr?.Order ?? int.MaxValue;
		}

		[CanBeNull]
		public static string GetParameterGroup([CanBeNull] ICustomAttributeProvider provider)
		{
			ParameterAttribute attr = GetParameterAttribute(provider);
			return attr?.Group;
		}

		[CanBeNull]
		private static ParameterAttribute GetParameterAttribute(
			[CanBeNull] ICustomAttributeProvider provider)
		{
			if (provider == null)
			{
				return null;
			}

			const bool inherit = true;
			object[] attributes = provider.GetCustomAttributes(
				typeof(ParameterAttribute), inherit);

			// ParameterAttribute has AttributeUsage AllowMultiple=false,
			// so the first ParameterAttribute we find will be the only one.
			return attributes.OfType<ParameterAttribute>().FirstOrDefault();
		}

		public static string GetParameterDescription([NotNull] IGdbProcess process)
		{
			Assert.ArgumentNotNull(process, nameof(process));

			var sb = new StringBuilder();

			Type type = process.GetType();

			sb.AppendFormat("GdbProcess Parameters (Name: {0}, Type: {1}):", process.Name,
			                type.Name);

			foreach (PropertyInfo property in GetProcessParameters(type))
			{
				sb.AppendLine();
				sb.AppendFormat("  {0} = {1}", property.Name, property.GetValue(process, null));
			}

			return sb.ToString();
		}

		[NotNull]
		public static string GetParameterInfoRTF([NotNull] PropertyInfo property)
		{
			Assert.ArgumentNotNull(property, nameof(property));

			string displayType = GetParameterDisplayType(property);
			string description = GetParameterDescription(property);

			var rtf = new RichTextBuilder();
			rtf.FontSize(8);
			rtf.Bold(property.Name).Text(" (").Text(displayType).Text(")");
			rtf.LineBreak();
			rtf.Text(description);

			return rtf.ToRtf();
		}

		/// <summary>
		/// Get the parameter's description (as specified with the Doc attribute).
		/// </summary>
		[NotNull]
		public static string GetParameterDescription([NotNull] PropertyInfo property)
		{
			string raw = ReflectionUtils.GetDescription(property) ?? string.Empty;

			return DescriptionPlaceholderRegex.Replace(
				raw, m => ExpandDescriptionPlaceholder(m, property));
		}

		private static readonly Regex DescriptionPlaceholderRegex =
			new Regex(@"{{\s*([A-Za-z0-9_. ]+)\s*}}");

		private static string ExpandDescriptionPlaceholder(Match match,
		                                                   PropertyInfo property)
		{
			string text = match.Groups[1].Value;

			// recognize: [parameter.]Name, [parameter.]Type, [parameter.]Values

			string[] parts = Regex.Split(text, @"\s*\.\s*");
			string key = null;

			if (parts.Length == 2 && parts[0] == "parameter")
				key = parts[1].Trim();
			else if (parts.Length == 1)
				key = parts[0].Trim();

			switch (key)
			{
				case "Name":
					return property.Name;
				case "Type":
					return property.PropertyType.Name;
				case "Values":
					return GetParameterDisplayValues(property) ?? match.Value;
			}

			return match.Value; // do not expand
		}

		[NotNull]
		public static string GetParameterDisplayType([NotNull] PropertyInfo property)
		{
			ParameterAttribute attr = GetParameterAttribute(property);
			if (attr != null && ! string.IsNullOrEmpty(attr.DisplayType))
			{
				return attr.DisplayType;
			}

			// Translate a few common types to "user friendly" names:

			if (property.PropertyType == typeof(bool))
			{
				return "Boolean";
			}

			if (property.PropertyType == typeof(int))
			{
				return "Integer";
			}

			if (property.PropertyType == typeof(double))
			{
				return "Number";
			}

			if (property.PropertyType == typeof(string))
			{
				return "String";
			}

			// All other types use their technical name:

			return property.PropertyType.Name;
		}

		[CanBeNull]
		public static string GetParameterDisplayValues([NotNull] PropertyInfo property)
		{
			if (property.PropertyType == typeof(bool))
			{
				return "False, True";
			}

			if (property.PropertyType.IsEnum)
			{
				return string.Join(", ", Enum.GetNames(property.PropertyType));
			}

			return null; // cannot enumerate values
		}

		#endregion

		public static void WriteProcessReport([NotNull] IList<Assembly> assemblies,
		                                      [NotNull] string htmlFileName)
		{
			Assert.ArgumentNotNull(assemblies, nameof(assemblies));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			using (Stream stream = new FileStream(htmlFileName, FileMode.Create))
			{
				IProcessReportBuilder builder =
					new HtmlProcessReportBuilder("ProSuite Carto Process Documentation");

				builder.AddHeaderItem("ProSuite Version",
				                      ReflectionUtils.GetAssemblyVersionString(
					                      Assembly.GetExecutingAssembly()));

				builder.IncludeObsolete = true;
				builder.IncludeAssemblyInfo = true;

				foreach (Assembly assembly in assemblies)
				{
					foreach (Type type in assembly.GetTypes())
					{
						if (IsGdbProcessType(type))
						{
							builder.AddProcessType(type);
						}
					}
				}

				builder.WriteReport(stream);
			}
		}

		public static bool Execute([NotNull] IProcessingContext context,
		                           [NotNull] IProcessingFeedback feedback,
		                           [NotNull] IEnumerable<IGdbProcess> processes,
		                           [CanBeNull] string actionName)
		{
			Assert.ArgumentNotNull(context, nameof(context));
			Assert.ArgumentNotNull(feedback, nameof(feedback));
			Assert.ArgumentNotNull(processes, nameof(processes));

			IGdbTransaction transaction = context.GetTransaction();
			IWorkspace workspace = context.GetWorkspace();

			// TODO - Consider: transient processes: execute a list of ProcessDescriptors;
			// TODO - for each descriptor, instantiate and configure a GdbProcess.

			return transaction.Execute(
				workspace,
				() => Execute(context, feedback, processes),
				actionName ?? GetActionName(processes));
		}

		public static string FormatProcessingStopped()
		{
			return "Processing stopped. If in an edit session " +
			       "use 'Undo' to discard all processed results.";
		}

		public static string FormatProcessingCompleted(int errors, int warnings,
		                                               TimeSpan? duration = null)
		{
			var sb = new StringBuilder("Processing completed");

			if (duration.HasValue)
			{
				sb.Append(" in ");
				sb.AppendDuration(duration.Value);
			}

			if (errors > 0)
			{
				bool one = errors == 1;
				sb.AppendFormat(" - there {0} been {1} error{2}",
				                one ? "has" : "have", errors, one ? "" : "s");
			}
			else if (warnings > 0)
			{
				bool one = warnings == 1;
				sb.AppendFormat(" - there {0} been {1} warning{2}",
				                one ? "has" : "have", warnings, one ? "" : "s");
			}

			return sb.ToString();
		}

		#region Private utils

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static void Execute([NotNull] IProcessingContext context,
		                            [NotNull] IProcessingFeedback feedback,
		                            [NotNull] IEnumerable<IGdbProcess> processes)
		{
			int current = 0, total = processes.Count();

			try
			{
				foreach (IGdbProcess process in processes)
				{
					current += 1;

					if (feedback.CancellationPending)
					{
						throw new OperationCanceledException();
					}

					ReportProcessStarting(feedback, process, current, total);

					if (process is IGroupGdbProcess)
					{
						feedback.CurrentGroup = process.Name;
						feedback.CurrentProcess = null;
					}
					else
					{
						feedback.CurrentGroup = null;
						feedback.CurrentProcess = process.Name;

						_msg.Debug(GetParameterDescription(process));
					}

					try
					{
						DateTime startTime = DateTime.Now;

						using (_msg.IncrementIndentation())
						{
							process.Execute(context, feedback);
						}

						TimeSpan duration = DateTime.Now - startTime;
						ReportProcessCompleted(feedback, process, duration);
					}
					catch (OperationCanceledException)
					{
						throw; // rethrow (but catch all other exceptions)
					}
					catch (Exception ex)
					{
						feedback.ReportError(
							string.Format("Error executing {0} {1}: {2}",
							              process is IGroupGdbProcess
								              ? "Process Group"
								              : "GdbProcess", process.Name,
							              ex.Message), ex);
					}
				}

				feedback.ReportCompleted();
			}
			catch (OperationCanceledException)
			{
				feedback.ReportStopped();
			}
		}

		private static string GetActionName([NotNull] IEnumerable<IGdbProcess> processes)
		{
			List<IGdbProcess> processList = processes.ToList();

			if (processList.Count == 1)
			{
				IGdbProcess process = processList[0];
				return string.Format("Execute {0} '{1}'", process is IGroupGdbProcess
					                                          ? "Process Group"
					                                          : "GdbProcess", process.Name);
			}

			return string.Format("Execute {0} GdbProcesses", processList.Count);
		}

		private static void ReportProcessStarting([NotNull] IProcessingFeedback feedback,
		                                          [NotNull] IGdbProcess process,
		                                          int current, int total)
		{
			var sb = new StringBuilder();

			sb.Append("Executing ");
			sb.Append(process is IGroupGdbProcess ? "Process Group" : "GdbProcess");
			sb.AppendFormat(" '{0}'", process.Name);

			if (total > 1 && 0 < current && current <= total)
			{
				sb.AppendFormat(" ({0} of {1})", current, total);
			}

			feedback.ReportInfo(sb.ToString());
		}

		private static void ReportProcessCompleted([NotNull] IProcessingFeedback feedback,
		                                           [NotNull] IGdbProcess process,
		                                           TimeSpan duration)
		{
			var sb = new StringBuilder();

			string what = process is IGroupGdbProcess ? "Process Group" : "GdbProcess";

			sb.AppendFormat("{0} '{1}' completed in ", what, process.Name);
			sb.AppendDuration(duration);

			feedback.ReportInfo(sb.ToString());
		}

		private static void AppendDuration(this StringBuilder sb, TimeSpan duration)
		{
			if (duration.Days > 1)
			{
				sb.AppendFormat("{0} days ", duration.Days);
			}
			else if (duration.Days == 1)
			{
				sb.Append("1 day ");
			}

			sb.AppendFormat("{0:00}:{1:00}:{2:00}",
			                duration.Hours, duration.Minutes, duration.Seconds);
		}

		#endregion
	}
}
