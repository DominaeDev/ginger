namespace AvsAnLib.Internals
{
    public static class WordQuery
    {
        /// <summary>
        ///     Determines the article for a given prefix by looking it up in the prefix trie.  Recursive.
        /// </summary>
        /// <param name="node">The root of the (remaining) prefix-trie to check</param>
        /// <param name="word">The word being checked</param>
        public static AvsAn.Result Query(Node node, string word)
        {
            var depth = 0;
            var result = node.ratio;
            var length = word.Length;
            while (true)
                if (depth >= length) {
                    return new AvsAn.Result(result, word, depth);
                } else {
                    var c = word[depth];
                    if (c == '"' || c == '‘' || c == '’' || c == '“' || c == '”' || c == '$' || c == '\'' || c == '-' || c == '(')
                        depth++;
                    else
                        break;
                }
            while (true) {
                if (node.SortedKids == null)
                    break;
                var c = depth == length ? ' ' : word[depth];
                var candidateIdx = node.SortedKids.Length - 1;
                var start = 0;
                //invariant: only LT nodes before start
                //invariant: only GTE nodes at or past candidateIdx *OR* needle doesn't exist.

                while (candidateIdx != start) {
                    var midpoint = (candidateIdx + start) >> 1;
                    if (node.SortedKids[midpoint].c < c)
                        start = midpoint + 1;
                    else
                        candidateIdx = midpoint;
                }

                if (node.SortedKids[candidateIdx].c != c)
                    break;
                node = node.SortedKids[candidateIdx];
                if (node.ratio.isSet)
                    result = node.ratio;

                depth++;
                if (depth > length)
                    break;
            }
            return new AvsAn.Result(result, word, depth);
        }
    }
}
