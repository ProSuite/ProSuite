using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class IncludedInstanceClass : IncludedInstanceBase, IComparable<IncludedInstanceClass>
	{
		private readonly Type _instanceType;

		private readonly List<IncludedInstanceConstructor> _instanceConstructors =
			new List<IncludedInstanceConstructor>();

		public IncludedInstanceClass([NotNull] Type instanceType)
			: base(GetTitle(instanceType),
			       instanceType.Assembly,
			       ReflectionUtils.IsObsolete(instanceType),
			       TestFactoryUtils.IsInternallyUsed(instanceType),
			       ReflectionUtils.GetCategories(instanceType))
		{
			Assert.ArgumentNotNull(instanceType, nameof(instanceType));

			_instanceType = instanceType;
		}

		[NotNull]
		public IncludedInstanceConstructor CreateInstanceConstructor(int constructorIndex)
		{
			return IncludedInstanceConstructor.CreateInstance(_instanceType, constructorIndex);
		}

		public void IncludeConstructor([NotNull] IncludedInstanceConstructor instanceConstructor)
		{
			Assert.ArgumentNotNull(instanceConstructor, nameof(instanceConstructor));
			Assert.ArgumentCondition(_instanceType == instanceConstructor.InstanceType,
			                         "Constructor does not belong to this class");

			if (_instanceConstructors.Contains(instanceConstructor))
			{
				return;
			}

			_instanceConstructors.Add(instanceConstructor);
		}

		public ICollection<IncludedInstanceConstructor> InstanceConstructors =>
			_instanceConstructors;

		private static string GetTitle(Type instanceType)
		{
			return string.Format("{0}", instanceType.Name);
		}

		#region Overrides of IncludedInstanceBase

		public override string Key => _instanceConstructors.Count > 0
			                              ? _instanceConstructors[0].Key
			                              : string.Format("{0}", _instanceType.FullName);

		public override string IndexTooltip
		{
			get
			{
				string description = ReflectionUtils.GetDescription(_instanceType);
				if (StringUtils.IsNotEmpty(description))
				{
					return description;
				}

				return _instanceConstructors.Count > 0
					       ? _instanceConstructors[0].IndexTooltip
					       : string.Empty;
			}
		}

		public override Type InstanceType => _instanceType;

		public override IList<IssueCode> IssueCodes => IssueCodeUtils.GetIssueCodes(_instanceType);

		[CanBeNull]
		public override string Description =>
			ReflectionUtils.GetDescription(_instanceType, inherit: false);

		#endregion

		#region Implementation of IComparable<IncludedInstanceClass>

		public int CompareTo(IncludedInstanceClass other)
		{
			return base.CompareTo(other);
		}

		#endregion
	}
}
