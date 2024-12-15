using System;
using System.Collections.Generic;
using System.Linq;

namespace Ginger
{
	public static class ParameterResolver
	{
		public static ParameterStates ResolveParameters(Recipe[] recipes, Context context)
		{
			var parameterStates = new ParameterStates(recipes);

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
				state.globalContext = Context.Copy(context);
				state.globalContext.AddFlags(globalFlags[i]);
				state.globalParameters.SetFlags(globalFlags[i]);
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
				if (idxLastRecipe != -1) // Pass on global state
					state.CopyGlobals(parameterStates[idxLastRecipe]);

				foreach (var parameter in recipe.parameters.OrderByDescending(p => p.isImmediate))
				{
					if (idxLastRecipe != -1 && parameter.isGlobal && parameterStates[idxLastRecipe].IsReserved(parameter.id))
						continue; // Skip reserved

					parameter.Apply(state);
				}

				idxLastRecipe = i;
			}

			return parameterStates;
		}

		public static Context[] GetLocalContexts(Recipe[] recipes, Context outerContext)
		{
			if (recipes == null || recipes.Length == 0)
				return new Context[0];

			ParameterStates parameterStates = ResolveParameters(recipes, outerContext);

			// Create contexts
			Context[] localContexts = new Context[recipes.Length];
			for (int i = 0; i < recipes.Length; ++i)
			{
				if (recipes[i].isEnabled == false)
					continue; // Skip
				localContexts[i] = parameterStates[i].evalContext;
			}

			return localContexts;
		}

		public static Context GetFinalContext(Recipe[] recipes, Context outerContext)
		{
			if (recipes == null || recipes.Length == 0)
				return Context.Copy(outerContext);

			ParameterStates parameterStates = ResolveParameters(recipes, outerContext);
			int idxLastRecipe = Array.FindLastIndex(recipes, r => r.isEnabled);
			if (idxLastRecipe == -1)
				return Context.Copy(outerContext);

			var context = Context.Copy(parameterStates[idxLastRecipe].evalContext);
			context.AddFlags(recipes[idxLastRecipe].flags);
			return context;
		}

	}
}
