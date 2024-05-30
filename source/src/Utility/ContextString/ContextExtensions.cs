namespace Ginger
{
	public static class ContextHelper
	{
		public static bool GetContextualValue(this IContextual ctx, StringHandle valueName, out float value)
		{
			var contextualValue = ctx.GetContextualValue(valueName);
			if (contextualValue.isFloat)
			{
				value = contextualValue.ToFloat();
				return true;
			}
			value = default(float);
			return false;
		}

        public static bool GetContextualValue(this IContextual ctx, StringHandle valueName, out string value)
		{
			var contextualValue = ctx.GetContextualValue(valueName);
			if (contextualValue.isString)
			{
				value = contextualValue.ToString();
				return true;
			}

			if (contextualValue.isNil)
			{
				value = default(string);
				return true;
			}

			value = default(string);
			return false;
		}
		
	}
}
