using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.Commands;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.Processing.Xml
{
	public static class XmlCartoProcessUtils
	{
		public static XmlCartoProcessesDocument ReadFile([NotNull] string xmlFilePath)
		{
			Assert.ArgumentNotNullOrEmpty(xmlFilePath, nameof(xmlFilePath));
			Assert.ArgumentCondition(File.Exists(xmlFilePath),
			                         "File does not exist: {0}", xmlFilePath);

			using (var stream = new FileStream(xmlFilePath, FileMode.Open, FileAccess.Read))
			{
				var serializer = new XmlSerializer(typeof(XmlCartoProcessesDocument));

				return (XmlCartoProcessesDocument) serializer.Deserialize(stream);
			}
		}

		[NotNull]
		public static CartoProcessType CreateCartoProcessType(
			[NotNull] XmlCartoProcessType xmlProcessType)
		{
			Assert.ArgumentNotNull(xmlProcessType, nameof(xmlProcessType));

			ClassDescriptor classDescriptor = CreateClassDescriptor(xmlProcessType.ClassDescriptor);
			var processType = new CartoProcessType(xmlProcessType.Name,
			                                       xmlProcessType.Description,
			                                       classDescriptor);

			return processType;
		}

		[NotNull]
		private static ClassDescriptor CreateClassDescriptor(
			[NotNull] XmlClassDescriptor xmlClassDescriptor)
		{
			Assert.ArgumentNotNull(xmlClassDescriptor, nameof(xmlClassDescriptor));

			var classDescriptor = new ClassDescriptor(xmlClassDescriptor.TypeName,
			                                          xmlClassDescriptor.AssemblyName,
			                                          xmlClassDescriptor.Description);
			return classDescriptor;
		}

		public static void TransferProperties([NotNull] CartoProcessType from,
		                                      [NotNull] CartoProcessType to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			to.Name = from.Name;
			to.Description = from.Description;
			to.CartoProcessClassDescriptor = from.CartoProcessClassDescriptor;
		}

		[NotNull]
		public static CartoProcess CreateCartoProcess([NotNull] XmlCartoProcess xmlProcess,
		                                              [NotNull] CartoProcessType processType,
		                                              [NotNull] DdxModel model)
		{
			Assert.ArgumentNotNull(xmlProcess.ModelReference, nameof(xmlProcess));
			Assert.ArgumentNotNull(processType, nameof(processType));
			Assert.ArgumentNotNull(model, nameof(model));

			//Maybe if xmlProcess.ModelReference is not null make sure it matches model?

			var result = new CartoProcess(xmlProcess.Name, xmlProcess.Description,
			                              model, processType);

			foreach (XmlCartoProcessParameter parameter in xmlProcess.Parameters)
			{
				result.AddParameter(
					new CartoProcessParameter(parameter.Name, parameter.Value, result));
			}

			return result;
		}

		public static void TransferProperties([NotNull] CartoProcess from,
		                                      [NotNull] CartoProcess to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			to.Name = from.Name;
			to.Description = from.Description;
			to.Model = from.Model; //really allow this?
			to.CartoProcessType = from.CartoProcessType;

			to.ClearParameters();
			foreach (CartoProcessParameter parameter in from.Parameters)
			{
				to.AddParameter(new CartoProcessParameter(parameter.Name, parameter.Value, to));
			}
		}

		[NotNull]
		public static CartoProcessGroup CreateCartoProcessGroup(
			[NotNull] XmlCartoProcessGroup xmlGroup,
			[NotNull] IList<CartoProcess> groupProcesses,
			[CanBeNull] ICommandDescriptor groupCommandDescriptor,
			[CanBeNull] CartoProcessType groupProcessType)
		{
			Assert.ArgumentNotNull(xmlGroup, nameof(xmlGroup));
			Assert.ArgumentNotNull(groupProcesses, nameof(groupProcesses));

			var group = new CartoProcessGroup(xmlGroup.Name);

			group.Description = xmlGroup.Description;

			group.AssociatedCommand = groupCommandDescriptor;
			group.AssociatedCommandIcon = xmlGroup.AssociatedCommandIcon;

			group.AssociatedGroupProcessType = groupProcessType;

			foreach (CartoProcess groupProcess in groupProcesses)
			{
				group.AddProcess(groupProcess);
			}

			return group;
		}

		public static void TransferProperties([NotNull] CartoProcessGroup from,
		                                      [NotNull] CartoProcessGroup to)
		{
			Assert.ArgumentNotNull(from, nameof(from));
			Assert.ArgumentNotNull(to, nameof(to));

			foreach (CartoProcess process in GetProcessesToRemove(from, to))
			{
				to.RemoveProcess(process);
			}

			foreach (CartoProcess process in from.Processes)
			{
				if (! to.Processes.Contains(process))
				{
					to.AddProcess(process);
				}
			}

			to.Description = from.Description;

			to.AssociatedCommand = from.AssociatedCommand;
			to.AssociatedCommandIcon = from.AssociatedCommandIcon;

			to.AssociatedGroupProcessType = from.AssociatedGroupProcessType;
		}

		[NotNull]
		private static IEnumerable<CartoProcess> GetProcessesToRemove(
			[NotNull] CartoProcessGroup from, [NotNull] CartoProcessGroup to)
		{
			IList<DdxModel> modelsToUpdate = new List<DdxModel>();
			foreach (CartoProcess process in from.Processes)
			{
				if (! modelsToUpdate.Contains(process.Model))
				{
					modelsToUpdate.Add(process.Model);
				}
			}

			return modelsToUpdate.SelectMany(to.GetProcesses).ToList();
		}
	}
}
