namespace Ginger
{
	public interface ISyntaxHighlighted
	{
		void RefreshSyntaxHighlight(bool immediate, bool invalidate = true);
	}
}
