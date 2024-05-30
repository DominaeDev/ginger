
namespace Ginger
{
	public interface ISpellChecked
	{
		void SpellCheck(bool bForce = false, bool bRehighlight = false);
		void EnableSpellCheck(bool enabled);
		void ClearSpellCheck();
	}
}
