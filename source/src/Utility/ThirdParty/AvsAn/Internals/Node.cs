using System;

namespace AvsAnLib.Internals {
    /// <summary>
    /// A node the article lookup trie. Do not mutate after construction.
    /// </summary>
    public struct Node : IComparable<Node> {
        public char c;
        public Ratio ratio;
        public Node[] SortedKids;
        public int CompareTo(Node other) { return c.CompareTo(other.c); }

        public void LoadPrefixRatio(string prefix, int depth, Ratio prefixRatio) {
            if (prefix.Length == depth) {
                ratio = prefixRatio;
            } else {
                var kidC = prefix[depth];
                var idx = GetOrAddKidIdx(kidC);
                SortedKids[idx].LoadPrefixRatio(prefix, depth + 1, prefixRatio);
            }
        }

        public int GetOrAddKidIdx(char kidC) {
            if (SortedKids == null) {
                SortedKids = new[] { new Node { c = kidC, } }; //expensive, so many arrays.
                return 0;
            } else {
                var idx = IdxAfterLastLtNode(kidC);

                if (idx == SortedKids.Length || SortedKids[idx].c != kidC)
                    InsertBefore(idx, kidC);
                return idx;
            }
        }

        int IdxAfterLastLtNode(char needle) {
            int start = 0, end = SortedKids.Length;
            //invariant: only LT nodes before start
            //invariant: only GTE nodes at or past end

            while (end != start) {
                var midpoint = end + start >> 1;
                // start <= midpoint < end
                if (SortedKids[midpoint].c < needle) {
                    start = midpoint + 1;//i.e.  midpoint < start1 so start0 < start1
                } else {
                    end = midpoint;//i.e end1 = midpoint so end1 < end0
                }
            }
            return end;
        }

        void InsertBefore(int idx, char newC) {
            var newArr = new Node[SortedKids.Length + 1];
            var i = 0;
            for (; i < idx; i++) {
                newArr[i] = SortedKids[i];
            }
            newArr[idx].c = newC;
            for (; i < SortedKids.Length; i++) {
                newArr[i + 1] = SortedKids[i];
            }
            SortedKids = newArr;
        }
    }
}