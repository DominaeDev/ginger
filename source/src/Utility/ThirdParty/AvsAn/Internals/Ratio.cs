namespace AvsAnLib.Internals {
    /// <summary>
    /// The ratio of a's vs. an's for a given prefix
    /// </summary>
    public struct Ratio {
        public int Occurence, AminAnDiff;

        public int aCount {
            get { return (Occurence + AminAnDiff) / 2; }
            set {
                var old_anCount = anCount;
                Occurence = value + old_anCount;
                AminAnDiff = value - old_anCount;
            }
        }

        public int anCount {
            get { return (Occurence - AminAnDiff) / 2; }
            set {
                var old_aCount = aCount;
                Occurence = old_aCount + value;
                AminAnDiff = old_aCount - value;
            }
        }

        public void IncA() {
            Occurence++;
            AminAnDiff++;
        }
        public void IncAn() {
            Occurence++;
            AminAnDiff--;
        }

        public bool isSet => Occurence != 0;

        public int Quality() {
            if (AminAnDiff == 0)
                return 0;
            return (int)(AminAnDiff * (long) AminAnDiff / Occurence);

        }
    }
}