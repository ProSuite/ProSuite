using System.Collections.Generic;
using ProSuite.Commons.AttributeDependencies;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.AttributeDependencies
{
	public class AttributeValueMapping : EntityWithMetadata
	{
		[UsedImplicitly] private readonly string _sourceText;
		[UsedImplicitly] private readonly string _targetText;
		[UsedImplicitly] private readonly string _description;

		private IList<object> _sourceValues;
		private IList<object> _targetValues;

		#region Constructors

		/// <remarks>Required for NHibernate</remarks>
		protected AttributeValueMapping() { }

		public AttributeValueMapping(string sourceText, string targetText,
		                             string description)
		{
			_sourceText = sourceText;
			_targetText = targetText;

			_description = description;
		}

		#endregion

		public string SourceText
		{
			get { return _sourceText; }
		}

		public string TargetText
		{
			get { return _targetText; }
		}

		public string Description
		{
			get { return _description; }
		}

		/// <remarks>Derived from <see cref="SourceText"/></remarks>
		public IList<object> SourceValues =>
			_sourceValues ??
			(_sourceValues =
				 AttributeDependencyUtils.ParseValues(_sourceText).AsReadOnly());

		/// <remarks>Derived from <see cref="TargetText"/></remarks>
		public IList<object> TargetValues =>
			_targetValues ??
			(_targetValues =
				 AttributeDependencyUtils.ParseValues(_targetText).AsReadOnly());

		public override string ToString()
		{
			return string.Concat(_sourceText, " => ", _targetText);
		}
	}
}
