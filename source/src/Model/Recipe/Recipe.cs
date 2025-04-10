﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Xml;

namespace Ginger
{
	public class Recipe : IXmlLoadable, IXmlSaveable, ICloneable
	{
		public static readonly int FormatVersion = 1;
		public static readonly int MaxNameLength = 64;

		public enum Component
		{
			Invalid		= -1,
			System		= 0,
			Persona,
			UserPersona,
			Scenario,
			Example,
			Grammar,
			Greeting,
			System_PostHistory, // System/Important
			Greeting_Group,

			Count,
		}

		private static readonly string[] ComponentNames = new string[] 
		{ 
			"System", 
			"Persona",
			"User",
			"Scenario",
			"Example", 
			"Grammar",
			"Greeting", 
		};

		public class Template
		{
			[Flags]
			public enum Flags
			{
				None		= 0,
				Detached	= 1 << 0,
				Raw			= 1 << 1,
				Important	= 1 << 2,
				GroupOnly	= 1 << 3,
				PerActor	= 1 << 4,
			}

			public Component channel;
			public ICondition condition;
			public string text;
			public Flags flags;

			public bool isRaw { get { return flags.Contains(Flags.Raw); } }
			public bool isDetached { get { return flags.Contains(Flags.Detached); } }
			public bool isImportant { get { return flags.Contains(Flags.Important); } }
			public bool isGroupOnly { get { return flags.Contains(Flags.GroupOnly); } }
			public bool isPerActor { get { return flags.Contains(Flags.PerActor); } }

			public override int GetHashCode()
			{
				return Utility.MakeHashCode(
					channel,
					text,
					condition,
					flags);
			}
		}

		public class LoreItem
		{
			public string key;
			public string text;
			public ICondition condition;
			public int order = Lorebook.Entry.DefaultSortOrder;

			public override int GetHashCode()
			{
				return Utility.MakeHashCode(
					key,
					text,
					condition, 
					order);
			}
		}

		public enum Drawer
		{
			Undefined,

			Model,
			Character,
			Mind,
			Traits,
			Story,

			Components,
			Snippets,
			Lore,

			Default = Undefined,
		}

		public enum Type
		{
			Recipe,
			Component,
			Snippet,
			Lore,
		}
		public Type type = Type.Recipe;

		public enum Category
		{
			// Recipe categories
			Undefined = 0,

			Base	= 100,
			Model, // (default)
			Modifier,

			Archetype = 200,
			Character, // (default)

			Appearance = 300,
			Special,
			Trait, // (default)
			Body, 
			Feature, 
			Speech,

			Personality = 400,
			Mind, // (default)
			Behavior,
			Quirk,
			Emotion,
			Sexual,

			User = 500,
			Relationship,

			Story = 600, // (default)
			Role,
			Job, 
			Cast,

			World = 700,
			Scenario,
			Location, 
			Concept,

			Custom = 750,

			Chat = 800, // Undocumented
			Lore = 900, // Undocumented
		}

		public StringHandle id;		// Recipe name
		public int uid;				// Hash
		public int instanceIndex;	// Undo
		public StringHandle instanceID { get { return string.Format("{0}-{1:D4}", id.ToString(), instanceIndex); } }

		public VersionNumber version;
		public string filename;
		public string name;
		public string origName;
		private string title;
		public string description;
		public string author;
		public string[] path;

		public Drawer drawer = Drawer.Default;
		public Category category = Category.Undefined;
		public string categoryTag = null;
		public List<IParameter> parameters = new List<IParameter>();
		public List<Block> blocks = new List<Block>();
		public List<Template> templates = new List<Template>();
		public List<LoreItem> loreItems = new List<LoreItem>();
		public List<CharacterAdjective> adjectives = new List<CharacterAdjective>();
		public List<CharacterNoun> nouns = new List<CharacterNoun>();
		public ICondition requires = null;
		public HashSet<StringHandle> flags = new HashSet<StringHandle>();
		public List<StringHandle> includes = new List<StringHandle>();
		public Color color = Constants.DefaultColor;
		public bool hasCustomColor;
		public StringBank strings = new StringBank();
		public AllowMultiple allowMultiple = AllowMultiple.No;
		public bool isBase { get { return flags.Contains(Constants.Flag.Base) || category == Category.Base; } }
		public bool isInternal { get { return flags.Contains(Constants.Flag.Internal); } }
		public bool isExternal { get { return filename == Constants.Flag.External; } }
		public bool isSnippet { get { return type == Type.Snippet; } }
		public bool isComponent { get { return flags.Contains(Constants.Flag.Component); } }
		public bool isLorebook { get { return isComponent && id == Constants.Flag.Lorebook; } }
		public bool isGreeting { get { return isComponent && id == Constants.Flag.Greeting; } }
		public bool isGrammar { get { return isComponent && id == Constants.Flag.Grammar; } }
		public bool isNSFW { get { return flags.Contains(Constants.Flag.NSFW); } }
		public bool canBake { get { return !isComponent && flags.Contains(Constants.Flag.DontBake) == false; } }
		public bool isHidden { get { return flags.Contains(Constants.Flag.Hidden); } }
		public bool canToggleTextFormatting { get { return flags.Contains(Constants.Flag.ToggleFormatting); } }
		public int? order = null;

		// State variables (Saved in instance)
		public bool isEnabled = true;
		public bool isCollapsed = false;
		public bool enableTextFormatting { get; private set; } // Components only
		public enum DetailLevel
		{
			Default = 0,
			Less = 1,
			Normal = 2,
			More = 3,
		}
		public DetailLevel levelOfDetail = DetailLevel.Default;
		public bool enableNSFWContent = true;

		public enum AllowMultiple
		{ 
			No = 0,
			Yes,
			One,
		}

		private static ICondition BaseExclusivityRule = Rule.Parse("not base");

		public Recipe()
		{
			this.enableTextFormatting = true;
		}

		public Recipe(string filename)
		{
			this.filename = filename;
			this.enableTextFormatting = true;
		}

		public bool LoadFromXml(XmlNode xmlNode)
		{
			// Version
			int formatVersion = xmlNode.GetAttributeInt("format", 1);
			if (formatVersion > FormatVersion)
				return false; // Unsupported version

			// Path / Name / ID
			string strPath = origName = xmlNode.GetValueElement("Name", null).SingleLine();
			if (strPath == null)
				return false;
			strPath = strPath.Trim();
			if (strPath.Length == 0)
				return false;
			if (strPath.Length > 256)
				strPath = strPath.Substring(0, 256);

			// Path
			strPath = strPath.Replace("//", "%%SLASH%%");
			var lsPath = strPath.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(s => s.Trim())
				.Where(s => s.Length > 0)
				.Select(s => s.Replace("%%SLASH%%", "/"))
				.Select(s => s.Length > MaxNameLength ? s.Substring(0, MaxNameLength) : s)
				.ToList();

			name = strPath = lsPath[lsPath.Count - 1];
			path = lsPath.Take(lsPath.Count - 1).ToArray();
			id = xmlNode.GetAttribute("id", name);

			// Tags
			string sFlags = xmlNode.GetValueElement("Flags");
			flags = new HashSet<StringHandle>(Utility.ListFromCommaSeparatedString(sFlags).Select(s => new StringHandle(s)));

			// Category
			var sCategory = xmlNode.GetValueElement("Category", null).SingleLine();
			Category eCategory = EnumHelper.FromString(sCategory, Category.Undefined);
			if (eCategory != Category.Undefined)
			{
				category = eCategory;
				categoryTag = EnumHelper.ToString(eCategory);
			}
			else
			{
				category = Category.Undefined;
				categoryTag = null;
			}

			if (isBase)
			{
				category = Category.Base;
				categoryTag = EnumHelper.ToString(Category.Base);
				flags.Add(Constants.Flag.Base);
			}
			else
			{
				categoryTag = sCategory;
			}

			// Drawer
			drawer = Constants.DrawerFromCategory[category];
			var sDrawer = xmlNode.GetValueElement("Drawer", null);
			if (sDrawer != null) // Override
			{
				switch (sDrawer.ToLowerInvariant())
				{
				case "appearance":
					drawer = Drawer.Traits;
					break;
				default:
					drawer = EnumHelper.FromString(sDrawer, drawer);
					break;
				}
			}

			// Version
			version = VersionNumber.Parse(xmlNode.GetAttribute("version", null));

			// Label
			title = xmlNode.GetValueElement("Title", null).SingleLine();
			if (string.IsNullOrWhiteSpace(title))
				title = strPath;

			// Description
			description = xmlNode.GetValueElement("Description");
			
			// Author
			author = xmlNode.GetValueElement("Author").SingleLine();

			// Multiple
			string multiple = xmlNode.GetValueElement("Multiple", "No");
			if (EnumInfo<AllowMultiple>.Convert(multiple, out this.allowMultiple) == false)
				allowMultiple = Utility.StringToBool(multiple) ? AllowMultiple.Yes : AllowMultiple.No;

			// Order?
			var orderNode = xmlNode.GetFirstElement("Order");
			if (orderNode != null)
			{
				order = orderNode.GetTextValueInt(int.MinValue);
				if (order.Value == int.MinValue)
					order = null;
			}

			// Parameters
			parameters.Clear();

			var parameterNode = xmlNode.GetFirstElementAny();
			while (parameterNode != null)
			{
				IParameter parameter = Parameter.Create(parameterNode, this); 
				if (parameter != null && parameter.LoadFromXml(parameterNode))
					parameters.Add(parameter);

				parameterNode = parameterNode.GetNextSiblingAny();
			}

			// Blocks
			blocks.Clear();
			var anyNode = xmlNode.GetFirstElementAny();
			while (anyNode != null)
			{
				if (anyNode.Name == "Node")
				{
					var block = new Block();
					if (block.LoadFromXml(anyNode))
						blocks.Add(block);
				}
				else if (anyNode.Name == "Attribute")
				{
					var attribute = new AttributeBlock();
					if (attribute.LoadFromXml(anyNode))
						blocks.Add(attribute);
				}

				anyNode = anyNode.GetNextSiblingAny();
			}

			// Components
			for (int i = 0; i < ComponentNames.Length; ++i)
			{
				var componentNode = xmlNode.GetFirstElement(ComponentNames[i]);
				while (componentNode != null)
				{
					var text = componentNode.GetTextValue();
					bool detached = componentNode.GetAttributeBool("detached", false);
					bool raw = componentNode.GetAttributeBool("raw", false);

					bool important = false;
					if (componentNode.Name == "System")
						important = componentNode.GetAttributeBool("important", false);
					bool groupOnly = false;
					if (componentNode.Name == "Greeting")
						groupOnly = componentNode.GetAttributeBool("group", false);
					bool perActor = false;
					if (componentNode.Name != "Grammar")
						perActor = componentNode.GetAttributeBool("per-actor", false);

					ICondition condition = null;
					if (componentNode.HasAttribute("rule"))
						condition = Rule.Parse(componentNode.GetAttribute("rule"));

					templates.Add(new Template() {
						channel = EnumHelper.FromInt(i, Component.Invalid),
						condition = condition,
						text = text.ConvertLinebreaks(Linebreak.CRLF),
						flags = (detached ? Template.Flags.Detached : 0) 
							| (raw ? Template.Flags.Raw : 0)
							| (important ? Template.Flags.Important : 0)
							| (groupOnly ? Template.Flags.GroupOnly : 0)
							| (perActor ? Template.Flags.PerActor : 0),
					});
					componentNode = componentNode.GetNextSibling();
				}
			}

			// Lore items
			var loreNode = xmlNode.GetFirstElement("Lore");
			while (loreNode != null)
			{
				var key = loreNode.GetValueElement("Name").Trim();
				var text = loreNode.GetValueElement("Value").Trim();
				var order = loreNode.GetAttributeInt("order", Lorebook.Entry.DefaultSortOrder);

				if (!(string.IsNullOrEmpty(key) || string.IsNullOrEmpty(text)))
				{
					ICondition condition = null;
					if (loreNode.HasAttribute("rule"))
						condition = Rule.Parse(loreNode.GetAttribute("rule"));

					loreItems.Add(new LoreItem() {
						key = key,
						text = text,
						order = order,
						condition = condition,
					});
				}

				loreNode = loreNode.GetNextSibling();
			}

			// Requires
			string requires = xmlNode.GetValueElement("Requires");
			if (string.IsNullOrEmpty(requires) == false)
				this.requires = Rule.Parse(requires);

			if (isBase)
			{
				// Base exclusivity requirement
				if (this.requires != null)
					this.requires = Condition.And(BaseExclusivityRule, this.requires);
				else
					this.requires = BaseExclusivityRule;
			}

			// Includes
			var includeNode = xmlNode.GetFirstElement("Include");
			while (includeNode != null)
			{
				StringHandle include = includeNode.GetTextValue(null);
				if (StringHandle.IsNullOrEmpty(include) == false)
					includes.Add(include);
				includeNode = includeNode.GetNextSibling();
			}

			// Color
			string sColor = xmlNode.GetValueElement("Color", null);
			hasCustomColor = string.IsNullOrWhiteSpace(sColor) == false;
			if (hasCustomColor)
			{
				try
				{
					color = ColorTranslator.FromHtml(sColor);
				}
				catch
				{
					hasCustomColor = false;
				}
			}
			if (!hasCustomColor)
			{
				if (category != Category.Undefined && Constants.RecipeColorByCategory.ContainsKey(category))
					color = Constants.RecipeColorByCategory[category];
				else if (drawer != Drawer.Undefined && Constants.RecipeColorByDrawer.ContainsKey(drawer))
					color = Constants.RecipeColorByDrawer[drawer];
				else
					color = Constants.DefaultColor;
			}

			// Strings (and rules)
			strings.LoadFromXml(xmlNode);

			// Adjectives
			adjectives.Clear();
			var adjectiveNode = xmlNode.GetFirstElement("Adjective");
			while (adjectiveNode != null)
			{
				var adjective = new CharacterAdjective();
				if (adjective.LoadFromXml(adjectiveNode))
					adjectives.Add(adjective);
				adjectiveNode = adjectiveNode.GetNextSibling();
			}

			// Nouns
			nouns.Clear();
			var nounNode = xmlNode.GetFirstElement("Noun");
			while (nounNode != null)
			{
				var noun = new CharacterNoun();
				if (noun.LoadFromXml(nounNode))
					nouns.Add(noun);
				nounNode = nounNode.GetNextSibling();
			}

			var addendumNode = xmlNode.GetFirstElement("Addendum");
			while (addendumNode != null)
			{
				var addendum = new CharacterNoun();
				if (addendum.LoadFromXml(addendumNode))
				{
					addendum.affix = CharacterNoun.Affix.Addendum;
					nouns.Add(addendum);
				}
				addendumNode = addendumNode.GetNextSibling();
			}

			uid = GetHashCode();
			return true;
		}

		public void SaveToXml(XmlNode xmlNode)
		{
			xmlNode.AddAttribute("id", id.ToString());
			xmlNode.AddAttribute("format", FormatVersion);
			xmlNode.AddAttribute("version", version.ToString());

			xmlNode.AddValueElement("Name", origName);
			xmlNode.AddValueElement("Title", title);
			if (string.IsNullOrWhiteSpace(categoryTag) == false)
				xmlNode.AddValueElement("Category", categoryTag);

			// Requires
			if (requires != null)
				xmlNode.AddValueElement("Requires", requires.ToString());

			// Descripiton
			if (string.IsNullOrEmpty(description) == false)
				xmlNode.AddValueElement("Description", description);

			// Descripiton
			if (string.IsNullOrEmpty(author) == false)
				xmlNode.AddValueElement("Author", author);

			// Multiple
			if (allowMultiple != AllowMultiple.No)
				xmlNode.AddValueElement("Multiple", EnumHelper.ToString(allowMultiple));

			// Order?
			if (order.HasValue)
				xmlNode.AddValueElement("Order", order.Value);

			// (Custom) color
			if (hasCustomColor)
				xmlNode.AddValueElement("Color", ColorTranslator.ToHtml(color));

			// Tags
			if (flags.Count > 0)
				xmlNode.AddValueElement("Flags", Utility.ListToCommaSeparatedString(flags));

			// Parameters
			if (parameters.Count > 0)
			{
				foreach (var parameter in parameters)
					parameter.SaveToXml(xmlNode);
			}

			// Blocks
			foreach (var block in blocks)
			{
				if (block is AttributeBlock)
				{
					var attributeNode = xmlNode.AddElement("Attribute");
					(block as AttributeBlock).SaveToXml(attributeNode);
				}
				else
				{
					var blockNode = xmlNode.AddElement("Node");
					block.SaveToXml(blockNode);
				}
			}

			// Templates
			foreach (var template in templates)
			{
				XmlElement templateNode;
				switch (template.channel)
				{
				default:
					continue;
				case Component.System:		templateNode = xmlNode.AddElement("System"); break;
				case Component.Persona:		templateNode = xmlNode.AddElement("Persona"); break;
				case Component.Scenario:	templateNode = xmlNode.AddElement("Scenario"); break;
				case Component.Greeting:	templateNode = xmlNode.AddElement("Greeting"); break;
				case Component.Example:		templateNode = xmlNode.AddElement("Example"); break;
				case Component.Grammar:		templateNode = xmlNode.AddElement("Grammar"); break;
				case Component.UserPersona:	templateNode = xmlNode.AddElement("User"); break;
				}

				if (template.condition != null)
					templateNode.AddAttribute("rule", template.condition.ToString());
				if (template.isDetached)
					templateNode.AddAttribute("detached", true);
				if (template.isRaw)
					templateNode.AddAttribute("raw", true);
				if (template.isImportant)
					templateNode.AddAttribute("important", true);
				if (template.isGroupOnly)
					templateNode.AddAttribute("group", true);
				templateNode.AddTextValue(template.text);
			}

			// Lore
			foreach (var lore in loreItems)
			{
				XmlElement loreNode = xmlNode.AddElement("Lore");

				loreNode.AddValueElement("Name", lore.key);
				loreNode.AddValueElement("Value", lore.text);

				loreNode.AddAttribute("order", lore.order);
				if (lore.condition != null)
					loreNode.AddAttribute("rule", lore.condition.ToString());
				loreNode.AddTextValue(lore.text);
			}

			// Strings (and macros, rules)
			strings.SaveToXml(xmlNode);
		}

		public string GetMenuLabel()
		{
			return Utility.EscapeMenu(name);
		}

		public string GetTitle()
		{
			StringBuilder sb = new StringBuilder(Utility.EscapeMenu(title));

			if (string.IsNullOrEmpty(categoryTag) == false && AppSettings.Settings.ShowRecipeCategory)
				sb.Insert(0, string.Concat("[",categoryTag,"] "));
			if (isNSFW)
				sb.Append(" (NSFW)");
			return sb.ToString();
		}

		public object Clone()
		{
			var clone = new Recipe(this.filename);
			clone.id = this.id;
			clone.uid = this.uid;
			clone.origName = this.origName;
			clone.name = this.name;
			clone.title = this.title;
			clone.path = (string[])this.path?.Clone();
			clone.drawer = this.drawer;
			clone.category = this.category;
			clone.categoryTag = this.categoryTag;
			clone.type = this.type;
			clone.instanceIndex = this.instanceIndex;
			clone.isEnabled = this.isEnabled;
			clone.isCollapsed = this.isCollapsed;
			clone.version = this.version;
			clone.description = this.description;
			clone.author = this.author;
			clone.allowMultiple = this.allowMultiple;
			clone.requires = this.requires;
			clone.order = this.order;
			clone.flags = new HashSet<StringHandle>(this.flags);
			clone.color = this.color;
			clone.hasCustomColor = this.hasCustomColor;
			clone.strings = this.strings;
			clone.includes = new List<StringHandle>(this.includes);
			clone.enableTextFormatting = this.enableTextFormatting;
			clone.enableNSFWContent = this.enableNSFWContent;
			clone.levelOfDetail = this.levelOfDetail;

			clone.blocks = new List<Block>(this.blocks.Count);
			for (int i = 0; i < this.blocks.Count; ++i)
			{
				var other = this.blocks[i];
				if (other is AttributeBlock)
					clone.blocks.Add((AttributeBlock)other.Clone());
				else
					clone.blocks.Add((Block)other.Clone());
			}

			clone.templates = new List<Template>(this.templates.Count);
			for (int i = 0; i < this.templates.Count; ++i)
			{
				var other = this.templates[i];
				clone.templates.Add(new Template() {
					channel = other.channel,
					text = other.text,
					condition = other.condition,
					flags = other.flags,
				});
			}
			clone.parameters = new List<IParameter>(this.parameters.Count);
			for (int i = 0; i < this.parameters.Count; ++i)
			{
				var other = (IParameter)this.parameters[i].Clone();
				other.recipe = clone;
				clone.parameters.Add(other);

			}
			clone.loreItems = new List<LoreItem>(this.loreItems.Count);
			for (int i = 0; i < this.loreItems.Count; ++i)
			{
				var other = this.loreItems[i];
				clone.loreItems.Add(new LoreItem() {
					key = other.key,
					text = other.text,
					condition = other.condition,
					order = other.order,
				});
			}
			clone.adjectives = new List<CharacterAdjective>(this.adjectives.Count);
			for (int i = 0; i < this.adjectives.Count; ++i)
			{
				var other = this.adjectives[i];
				clone.adjectives.Add(new CharacterAdjective() {
					value = other.value,
					priority = other.priority,
					condition = other.condition,
					order = other.order,
				});
			}
			clone.nouns = new List<CharacterNoun>(this.nouns.Count);
			for (int i = 0; i < this.nouns.Count; ++i)
			{
				var other = this.nouns[i];
				clone.nouns.Add(new CharacterNoun() {
					value = other.value,
					affix = other.affix,
					priority = other.priority,
					condition = other.condition
				});
			}

			return clone;
		}

		public static void CopyParameterValues(Recipe from, Recipe to)
		{
			if (from == null || to == null || ReferenceEquals(from, to))
				return;

			to.isEnabled = from.isEnabled;
			to.isCollapsed = from.isCollapsed;
			to.enableTextFormatting = from.enableTextFormatting;
			to.levelOfDetail = from.levelOfDetail;
			to.enableNSFWContent = from.enableNSFWContent;

			for (int i = 0; i < from.parameters.Count; ++i)
			{
				var source = from.parameters[i];
				var srcType = source.GetType();
				IParameter target;
				if (i < to.parameters.Count && to.parameters[i].id == source.id && to.parameters[i].GetType() == srcType)
					target = to.parameters[i];
				else
					target = to.parameters.FirstOrDefault(p => p.id == source.id && p.GetType() == srcType);

				if (target == null)
					continue;
				
				if (source is BooleanParameter)
				{
					(source as BooleanParameter).CopyValuesTo(target as BooleanParameter);
				}
				else if (source is NumberParameter)
				{
					(source as NumberParameter).CopyValuesTo(target as NumberParameter);
				}
				else if (source is RangeParameter)
				{
					(source as RangeParameter).CopyValuesTo(target as RangeParameter);
				}
				else if (source is MeasurementParameter)
				{
					(source as MeasurementParameter).CopyValueTo(target as MeasurementParameter);
				}
				else if (source is TextParameter)
				{
					(source as TextParameter).CopyValuesTo(target as TextParameter);
				}
				else if (source is ListParameter)
				{
					(source as ListParameter).CopyValuesTo(target as ListParameter);
				}
				else if (source is ChoiceParameter)
				{
					(source as ChoiceParameter).CopyValuesTo(target as ChoiceParameter);
				}
				else if (source is MultiChoiceParameter)
				{
					(source as MultiChoiceParameter).CopyValuesTo(target as MultiChoiceParameter);
				}
				else if (source is HintParameter)
				{
					(source as HintParameter).CopyValuesTo(target as HintParameter);
				}
				else if (source is SetVarParameter)
				{
					(source as SetVarParameter).CopyValuesTo(target as SetVarParameter);
				}
				else if (source is SetFlagParameter)
				{
					(source as SetFlagParameter).CopyValuesTo(target as SetFlagParameter);
				}
				else if (source is EraseParameter)
				{
					(source as EraseParameter).CopyValuesTo(target as EraseParameter);
				}
				else if (source is LorebookParameter)
				{
					(source as LorebookParameter).CopyValuesTo(target as LorebookParameter);
				}
				else
				{
					throw new Exception("Unknown parameter encounted during CopyParameterValues.");
				}
			}
		}

		public void ResetParameters()
		{
			if (parameters.ContainsAny(p => p is IResettableParameter) == false)
				return;

			Context evalContext = Current.Character.GetContext(CharacterData.ContextType.Full);
			var evalConfig = new ContextString.EvaluationConfig() {
				macroSuppliers = new IMacroSupplier[] { Current.Strings },
				referenceSuppliers = new IStringReferenceSupplier[] { Current.Strings },
				ruleSuppliers = new IRuleSupplier[] { Current.Strings },
			};

			char[] brackets = new char[] { '{', '[' };
			var characterNames = new string[] { Current.Name };
			var userName = Current.Card.userPlaceholder;

			// Eval adjectives
			if (parameters.OfType<IResettableParameter>().ContainsAny(p => p.defaultValue.ContainsPhrase("adjective")) )
				Generator.GetAdjectivesAndNoun(Current.Character.recipes, evalContext);

			foreach (var parameter in parameters.OfType<IResettableParameter>())
			{
				string defaultValue = parameter.defaultValue;
				if (parameter.raw)
				{
					parameter.ResetValue(defaultValue);
					continue;
				}

				// Evaluate default value
				if (string.IsNullOrEmpty(defaultValue) == false && defaultValue.IndexOfAny(brackets, 0) != -1)
				{
					defaultValue = GingerString.FromString(Text.Eval(defaultValue, evalContext, evalConfig, Text.EvalOption.Default))
						.ToParameter();
					if (AppSettings.Settings.AutoConvertNames)
						defaultValue = GingerString.WithNames(defaultValue, characterNames, userName);
				}

				parameter.ResetValue(defaultValue);
			}
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hash = 0x56C9982B;
				hash ^= Utility.MakeHashCode(
					id,
					requires,
					strings,
					allowMultiple);
				hash ^= Utility.MakeHashCode(flags, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(templates, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(blocks, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(loreItems, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(parameters, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(adjectives, Utility.HashOption.None);
				hash ^= Utility.MakeHashCode(nouns, Utility.HashOption.None);
				return hash;
			}
		}

		public IParameter GetParameter(StringHandle id)
		{
			return parameters.FirstOrDefault(p => p.id == id);
		}

		public void SetParameterValue(StringHandle id, string value, bool bEnabled)
		{
			var parameter = GetParameter(id);
			if (parameter == null)
				return; // Not found

			if (parameter is BaseParameter<string>)
			{
				var textParameter = parameter as BaseParameter<string>;
				textParameter.Set(value);
				textParameter.isEnabled = bEnabled;
			}
			else if (parameter is BaseParameter<bool>)
			{
				var boolParameter = parameter as BaseParameter<bool>;
				boolParameter.Set(Utility.StringToBool(value));
				boolParameter.isEnabled = bEnabled;
			}
			else if (parameter is BaseParameter<decimal>)
			{
				var numberParameter = parameter as BaseParameter<decimal>;
				numberParameter.Set(Utility.StringToDecimal(value));
				numberParameter.isEnabled = bEnabled;
			}
			else if (parameter is BaseParameter<HashSet<string>>)
			{
				var collectionParameter = parameter as BaseParameter<HashSet<string>>;
				collectionParameter.Set(new HashSet<string>(Utility.ListFromCommaSeparatedString(value)));
				collectionParameter.isEnabled = bEnabled;
			}
		}

		public string GetTooltip()
		{
			StringBuilder sbTooltip = new StringBuilder();
			if (string.IsNullOrEmpty(categoryTag) == false && AppSettings.Settings.ShowRecipeCategory)
				sbTooltip.AppendFormat("[{0}] ", categoryTag);
			sbTooltip.Append(title);

			if (isBase || isNSFW)
			{
				List<string> tags = new List<string>();
				if (isBase)
					tags.Add("Base recipe");
				if (isNSFW)
					tags.Add("NSFW");
				sbTooltip.Append(" (");
				sbTooltip.Append(string.Join("; ", tags));
				sbTooltip.Append(")");

			}
			if (string.IsNullOrEmpty(author) == false)
			{
				sbTooltip.NewLine();
				sbTooltip.Append("By ");
				sbTooltip.AppendLine(author);
			}
			if (string.IsNullOrEmpty(description) == false)
			{
				sbTooltip.NewParagraph();
				sbTooltip.AppendLine(description);
			}

			return sbTooltip.ToString();
		}

		public bool CompareTo(Recipe other)
		{
			if (id != other.id)
				return false;

			if (parameters.Count != other.parameters.Count)
				return false;

			for (int i = 0; i < parameters.Count; ++i)
			{
				if (parameters[i].GetParameterType() != other.parameters[i].GetParameterType())
					return false;

				if (parameters[i].GetParameterType() == typeof(Lorebook))
				{
					var lorebookA = (Lorebook)parameters[i].GetValue();
					var lorebookB = (Lorebook)other.parameters[i].GetValue();
					if (lorebookA.CompareTo(lorebookB) == false)
						return false;
				}
				else if (parameters[i].GetParameterType() == typeof(HashSet<string>))
				{
					var valueA = parameters[i].GetValue() as HashSet<string>;
					var valueB = other.parameters[i].GetValue() as HashSet<string>;
					if (Enumerable.SequenceEqual(valueA, valueB) == false)
						return false;
				}
				else if (parameters[i].GetParameterType() == typeof(string))
				{
					string valueA = (string)parameters[i].GetValue();
					string valueB = (string)other.parameters[i].GetValue();
					if (valueA == null && valueB == null)
						return true;
					if ((valueA == null) != (valueB == null))
						return false;

					if (valueA.ConvertLinebreaks(Linebreak.Default).Equals(valueB.ConvertLinebreaks(Linebreak.Default)) == false)
						return false;
				}
				else
				{
					var valueA = parameters[i].GetValue();
					var valueB = other.parameters[i].GetValue();
					if (valueA == null && valueB == null)
						return true;
					if ((valueA == null) != (valueB == null))
						return false;
					if (valueA.Equals(valueB) == false)
						return false;
				}
			}

			return true;
		}

		public int GetSortingOrder()
		{
			if (isBase)
				return 0;

			if (isComponent)
			{
				if (flags.Contains("__system"))
					return 199;
				else if (flags.Contains("__persona"))
					return 299;
				else if (flags.Contains("__user"))
					return 599;
				else if (flags.Contains("__scenario"))
					return 699;
				else if (flags.Contains("__greeting"))
					return 799;
				else if (flags.Contains("__example"))
					return 899;
				else if (flags.Contains("__lorebook"))
					return 999;
				else if (flags.Contains("__grammar"))
					return 1099;
				return 1000;
			}
			else
			{
				if (isBase)
					return 0;
				if (category == Category.Undefined)
					return EnumHelper.ToInt(Category.Custom);
				return EnumHelper.ToInt(category);
			}
		}

		public void EnableTextFormatting(bool bEnable)
		{
			enableTextFormatting = bEnable;
			foreach (var template in templates)
			{
				if (bEnable)
					template.flags &= ~Template.Flags.Raw;
				else
					template.flags |= Template.Flags.Raw;
			}
		}

		public void ConvertCharacterMarkers(string characterName, string userName)
		{
			ConvertCharacterMarkers(new string[] { characterName }, userName);
		}

		public void ConvertCharacterMarkers(string[] characterNames, string userName)
		{
			foreach (var parameter in parameters)
			{
				if (parameter is BaseParameter<string>)
				{
					var textParameter = parameter as BaseParameter<string>;
					textParameter.value = GingerString.WithNames(textParameter.value, characterNames, userName);
				}
				else if (parameter is BaseParameter<Lorebook>)
				{
					var lorebookParameter = parameter as BaseParameter<Lorebook>;
					var lorebook = lorebookParameter.value;

					foreach (var entry in lorebook.entries)
						entry.value = GingerString.WithNames(entry.value, characterNames, userName);
				}
			}
		}
	}
}
