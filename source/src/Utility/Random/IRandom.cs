namespace Ginger
{
	public enum RandomOption
	{
		Exclusive,
		Inclusive,
	}

	public interface IRandom
	{
		void SetSeed(int seed, int position = 0);
		void Advance(int count = 1);

		int Int(int maxExclusive);
		int Int(int min, int max, RandomOption option);
		float Float();
		float Float(float min, float max);
		
		int GenerateSeed();
	}

	public static class RandomExtensions
	{
		public static int Range(this IRandom rnd, int min, int max)
		{
			return rnd.Int(min, max, RandomOption.Inclusive);
		}

		public static int Range(this IRandom rnd, RangeInt range)
		{
			return rnd.Int(range.min, range.max, RandomOption.Inclusive);
		}

		public static float Range(this IRandom rnd, Range range)
		{
			return rnd.Float(range.min, range.max);
		}
	}
}
