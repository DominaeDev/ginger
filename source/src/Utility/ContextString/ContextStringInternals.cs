using System;
using System.Collections.Generic;

namespace Ginger
{
	public static class ContextStringSubstitutor
	{
		public static MacroBank macros = new MacroBank();

		private delegate string Replacement(Internal_Context ctx);
		private static Dictionary<string, Replacement> replacements = new Dictionary<string, Replacement>()
		{
			// Article
			{ "a", (Internal_Context ctx) =>				{ return "<%%a_or_an%%>"; } },

			// Conditional "the"
//			{ "the", (Internal_Context ctx) =>				{ return "<%%the%%>"; } },

			// Whitespace
			{ "br", (Internal_Context ctx) =>				{ return Text.Break; } },			// Explicit linebreak
			{ "p", (Internal_Context ctx) =>				{ return Text.ParagraphBreak; } },	// Explicit paragraph break
			{ "sp", (Internal_Context ctx) =>				{ return Text.Space; } },			// Explicit blankspace
			{ "tab", (Internal_Context ctx) =>				{ return Text.Tab; } },				// Explicit tab
			{ "ln", (Internal_Context ctx) =>				{ return Text.SoftBreak; } },		// Soft linebreak

			// Functions
			{ "nocap", (Internal_Context ctx) =>			{ return "<!nocap>"; } },			// reset capitalization = off
			{ "cap", (Internal_Context ctx) =>				{ return "<!cap>"; } },				// reset capitalization = on
			{ "unp", (Internal_Context ctx) =>				{ return "<%%unp%%>"; } },			// collapse whitespace
			{ "clr", (Internal_Context ctx) =>				{ return "<%%clr%%>"; } },			// collapse whitespace
			{ "comma", (Internal_Context ctx) =>			{ return "<%%clr%%><!com>"; } },	// comma
			{ "fs", (Internal_Context ctx) =>				{ return "<%%clr%%><!fs>"; } },		// full stop
			{ "qt", (Internal_Context ctx) =>				{ return "<!qt>"; } },				// quote
			{ "noparse", (Internal_Context ctx) =>			{ return "<!np+>"; } },				// stop parsing ... <!np->
//			{ "parse",  },	// resume parsing (Checked in parser)
		};

		public static bool Resolve(string phrase, Internal_Context internalContext, out string replacement, out bool isMacro)
		{
			if (phrase.Length == 0)
			{
				replacement = phrase;
				isMacro = false;
				return false;
			}

			string phrase_lowercase = phrase.ToLowerInvariant();

			// Built-in macro?
			Replacement rMacro = null;
			if (replacements.TryGetValue(phrase_lowercase, out rMacro))
			{
				replacement = rMacro(internalContext);
				isMacro = false;
				return true;
			}

			// Is a variable?
			if (internalContext.context.TryGetValue(phrase_lowercase, out replacement)) // Check context variables
			{
				isMacro = false;
				return true;
			}

			// External variable?
			if (internalContext.valueSuppliers != null)
			{
				foreach (var valueSupplier in internalContext.valueSuppliers)
				{
					if (valueSupplier.TryGetValue(phrase_lowercase, out replacement))
					{
						isMacro = false;
						return true;
					}
				}
			}

			// Custom macro?
			if (internalContext.macroSuppliers != null)
			{
				foreach (var macroSupplier in internalContext.macroSuppliers)
				{
					if (macroSupplier.GetMacroBank().TryGetMacro(phrase_lowercase, out replacement))
					{
						isMacro = true;
						return true;
					}
				}
			}

			// User macro?
			if (macros.TryGetMacro(phrase_lowercase, out replacement))
			{
				isMacro = true;
				return true;
			}
			
			isMacro = false;
			return false;
		}
	}

	public interface IValueSupplier
	{
		bool TryGetValue(StringHandle id, out string value);
	}

	public class Internal_Context
	{
		public Context context;

		public IMacroSupplier[] macroSuppliers = null;
		public IRuleSupplier[] ruleSuppliers = null;
		public IStringReferenceSupplier[] referenceStringBanks = null;
		public IValueSupplier[] valueSuppliers = null;
		public IRandom randomizer = null;

		public int stackDepth = 0;
		public static int MaxStackDepth = 100;
		public bool haltParsing = false;

		public ContextString.EvaluationConfig evalConfig
		{
			get
			{
				return new ContextString.EvaluationConfig() {
					macroSuppliers = macroSuppliers,
					referenceSuppliers = referenceStringBanks,
					valueSuppliers = valueSuppliers,
					ruleSuppliers = ruleSuppliers,
				};
			}
		}

		private Internal_Context()
		{
		}

		public Internal_Context(Context context)
		{
			if (context == null)
				throw new ArgumentNullException("ctx");

			this.context = context;
		}

		public Internal_Context Clone()
		{
			Internal_Context clone = new Internal_Context();
			clone.context = context;
			clone.stackDepth = stackDepth;
			clone.referenceStringBanks = referenceStringBanks;
			clone.macroSuppliers = macroSuppliers;
			clone.ruleSuppliers = ruleSuppliers;
			clone.valueSuppliers = valueSuppliers;
			clone.randomizer = randomizer;
			return clone;
		}
		
		private bool PushStack()
		{
			if (stackDepth >= MaxStackDepth)
				return false;
			++stackDepth;
			return true;
		}

		private void PopStack()
		{
			--stackDepth;
		}

		public bool PushStack(Action @delegate)
		{
			if (@delegate == null)
				return false;

			if (PushStack())
			{
				@delegate.Invoke();
				PopStack();
				return true;
			}
			return false;
		}

	}
}
