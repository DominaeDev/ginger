using System;

namespace Ginger
{
	public class RandomDefault : IRandom
	{
		System.Random random_engine = new System.Random();
		
		public RandomDefault()
		{
			SetSeed((int)DateTime.Now.Ticks);
		}

		public RandomDefault(int seed, int position = 0)
		{
			SetSeed(seed, position);
		}

		public void SetSeed(int seed, int position = 0)
		{
			random_engine = new System.Random(seed);
		}

		public void Advance(int count = 1)
		{
			for (int i = 0; i < count; ++i)
			NextInt(0, 0);
		}

		public int Int(int maxExclusive)
		{
			return Int(0, maxExclusive, RandomOption.Exclusive);
		}

		public int Int(int min, int max, RandomOption option)
		{
			int _min = Math.Min(min, max);
			int _max = Math.Max(min, max);
			if (option == RandomOption.Inclusive)
				return NextInt(_min, _max + 1);
			else
				return NextInt(_min, _max);
		}

		public float Float()
		{
			return (float)random_engine.NextDouble();
		}

		public float Float(float min, float max)
		{
			if (min == max)
				return min;

			float _min = Math.Min(min, max);
			float _max = Math.Max(min, max);

			return _min + (_max - _min) * (float)random_engine.NextDouble();
		}
		
		public bool Roll(float probability)
		{
			return (float)random_engine.NextDouble() < probability;
		}

		private int NextInt(int min, int max) // exclusive
		{
			int _min = Math.Min(min, max);
			int _max = Math.Max(min, max);
			return _min + random_engine.Next(_max - _min);
		}

		public int GenerateSeed()
		{
			return Int(0, int.MaxValue, RandomOption.Exclusive);
		}

		public T Flip<T>(T a, T b)
		{
			return Roll(0.5f) ? a : b;
		}
		
	}
}
