using System.IO;

namespace PNGNet
{
	[Chunk("acTL", AllowMultiple = false)]
	public class acTLChunk : Chunk
	{
		private uint _numFrames;
		private uint _numPlays;

		public acTLChunk(PNGImage image) : base("acTL", image)
		{
			this.NumFrames = 1;
			this.NumPlays = 0;
		}

		public acTLChunk(byte[] data, PNGImage image) : base(data, image)
		{
			this.AssertDataLength(data, 8);

            this.NumFrames = Utils.BytesToUInt(data, 4, Utils.Endianness.Big);
            this.NumPlays = Utils.BytesToUInt(data, 8, Utils.Endianness.Big);
		}

		protected override void WriteChunkData(MemoryStream ms)
		{
			BinaryWriter bw = new BinaryWriter(ms);
			bw.Write(Utils.UIntToBytes(this.NumFrames, Utils.Endianness.Big));
			bw.Write(Utils.UIntToBytes(this.NumPlays, Utils.Endianness.Big));
		}

		public uint NumFrames
		{
			get { return _numFrames; }
			set
			{
				if (value == 0)
					throw new InvalidChunkDataException("NumFrames cannot be 0.");
				this.AssertNumber31Bits(value, "NumFrames");
				_numFrames = value;
			}
		}

		public uint NumPlays
		{
			get { return _numPlays; }
			set
			{
				this.AssertNumber31Bits(value, "NumPlays");
				_numPlays = value;
			}
		}
	}
}
