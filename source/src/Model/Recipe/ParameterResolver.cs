using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public static class ParameterResolver
	{
		public static ParameterStates ResolveParameters(Recipe[] recipes, Context context)
		{
			Context tmp;
			return ResolveParameters(recipes, context, out tmp);
		}

		public static ParameterStates ResolveParameters(Recipe[] recipes, Context context, out Context globalContext)
		{
			var parameterStates = new ParameterStates(recipes);

			globalContext = Context.Copy(context);

			// Collect global flags
			var globalFlags = new HashSet<StringHandle>[recipes.Length];
			for (int i = 0; i < recipes.Length; ++i)
			{
				var flags = new HashSet<StringHandle>();
				globalFlags[i] = flags;
				for (int j = 0; j < recipes.Length; ++j)
					if (i != j && recipes[j].isEnabled)
						flags.UnionWith(recipes[j].flags);
			}

			// Create parameter states
			for (int i = 0; i < recipes.Length; ++i)
			{
				var state = parameterStates[i];
				state.outerScope.CopyFromContext(context);
				state.outerScope.SetFlags(globalFlags[i]);
				parameterStates[i] = state;
			}

			// Resolve parameters
			int idxLastRecipe = -1;
			for (int i = 0; i < recipes.Length; ++i)
			{
				var recipe = recipes[i];
				if (recipe.isEnabled == false)
					continue; // Skip

				var state = parameterStates[i];
				state.evalConfig = new ContextString.EvaluationConfig() {
					macroSuppliers = new IMacroSupplier[] { recipe.strings, Current.Strings },
					referenceSuppliers = new IStringReferenceSupplier[] { recipe.strings, Current.Strings },
					ruleSuppliers = new IRuleSupplier[] { recipe.strings, Current.Strings },
					valueSuppliers = new IValueSupplier[] { parameterStates },
				};
				if (idxLastRecipe != -1) // Forward reserved values
					state.CopyReserved(parameterStates[idxLastRecipe]);

				foreach (var parameter in recipe.parameters.OrderByDescending(p => p.isImmediate))
				{
					if (idxLastRecipe != -1 && parameter.isGlobal && parameterStates[idxLastRecipe].IsReserved(parameter.id))
						continue; // Skip reserved
					parameter.Apply(state);
				}

				CopyGlobals(parameterStates, i, globalContext);

				idxLastRecipe = i;
			}

			return parameterStates;
		}

		private static void CopyGlobals(ParameterStates parameterStates, int idxSource, Context globalContext)
		{
			var globals = parameterStates[idxSource].globalScope;

			for (int i = 0; i < parameterStates.Length; ++i)
			{
				if (i == idxSource)
					continue;

				var state = parameterStates[i];

				// Erase flags/values
				foreach (var flag in globals.erasedFlags)
					state.outerScope.EraseFlag(flag);
				foreach (var value in globals.erasedValues)
					state.outerScope.EraseValue(value);

				// Copy flags/values
				foreach (var value in globals.values)
					state.outerScope.SetValue(value.Key, value.Value);
				state.outerScope.SetFlags(globals.flags);
				state.Dirty();
			}

			// Update global context
			foreach (var flag in globals.erasedFlags)
				globalContext.RemoveFlag(flag);
			foreach (var value in globals.erasedValues)
				globalContext.SetValue(value, null);
			foreach (var value in globals.values)
				globalContext.SetValue(value.Key, value.Value);
			globalContext.SetFlags(globals.flags);
		}

		public static Context[] GetLocalContexts(Recipe[] recipes, Context outerContext)
		{
			if (recipes == null || recipes.Length == 0)
				return new Context[0];

			ParameterStates parameterStates = ResolveParameters(recipes, outerContext);
			Context[] localContexts = new Context[recipes.Length];

			// Create contexts
			for (int i = 0; i < recipes.Length; ++i)
				localContexts[i] = parameterStates[i].evalContext;

			return localContexts;
		}

	}
}
