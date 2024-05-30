using System;

namespace Ginger
{
	public class RandomNoise : IRandom
	{
		public int seed;
		public int position;

		public RandomNoise()
		{
			position = 0;
			seed = (int)DateTime.Now.Ticks;
		}

		public RandomNoise(int seed, int position = 0)
		{
			SetSeed(seed, position);
		}

		public void SetSeed(int seed, int position = 0)
		{
			this.seed = seed;
			this.position = position;
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
			if (option == RandomOption.Inclusive)
				return NextInt(min, max + 1 );
			else
				return NextInt(min, max);
		}

		public float Float(float min, float max)
		{
			return min + (max - min) * NextFloat();
		}

		public float Float()
		{
			return NextFloat();
		}
		
		public bool Roll(float p)
		{
			return NextFloat() < p;
		}

		public int GenerateSeed()
		{
			return (int)NextUInt();
		}

		private uint NextUInt()
		{			
			return Squirrel3.Get(position++, (uint)seed);
		}

		private int NextInt(int min, int max) // exclusive
		{
			if (min == max)
			{
				++position; // Increment position
				return min;
			}
			else if (min > max) // Ensure numerical order
			{
				int tmp = min;
				min = max;
				max = tmp;				
			}

			uint r = NextUInt() % (uint)(max - min);
			return min + (int)r;
		}

		private float NextFloat()
		{
			return (float)NextDouble();
		}

		private double NextDouble()
		{
			uint r = NextUInt();
			return (double)r / (double)uint.MaxValue;			
		}
	}
}
