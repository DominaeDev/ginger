using System.Collections.Generic;

namespace PNGNet
{
	public sealed class ChunkCollection : List<Chunk>
    {
        public Chunk this[string type]
        {
            get
            {
                foreach (Chunk c in this)
                {
                    if (c.Type == type)
                        return c;
                }

                return null;
            }
            set
            {
                for (int i = 0; i < this.Count; i++)
                {
                    if (this[i].Type == type)
                    {
                        this[i] = value;
                        return;
                    }
                }

                throw new KeyNotFoundException();
            }
        }

        public int IndexOf(string type)
        {
            for (int i = 0; i < this.Count; i++)
                if (this[i].Type == type)
                    return i;

            return -1;
        }
    }
}
