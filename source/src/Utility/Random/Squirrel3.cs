using System;

namespace Ginger
{
	public static class Squirrel3
	{
		public static int GenerateSeed()
		{
			return (int)Get(0, (uint)DateTime.Now.Ticks);
		}

		public static uint Get(int position, uint seed = 0)
		{
			// Squirrel3

			const uint BIT_NOISE1 = 0xB5297A4D;
			const uint BIT_NOISE2 = 0x68E3ADA4;
			const uint BIT_NOISE3 = 0x1B56C4E9;

			uint value = (uint)position;
			value *= BIT_NOISE1;
			value += seed;
			value ^= (value >> 8);
			value += BIT_NOISE2;
			value ^= (value << 8);
			value *= BIT_NOISE3;
			value ^= (value >> 8);
			return value;
		}
	}
}
