using System;
using System.Collections.Generic;
using System.Text;

/**
 * MIT LICENSE
 * 
 * Copyright 2023 belladore.ai
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the “Software”), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED “AS IS”, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 *	Original source: https://github.com/belladoreai/llama-tokenizer-js
 *	
 *	C# port by DominaeDev (c) 2023
 */

namespace LlamaTokenizer
{
	public static class LlamaTokenizer {

		private static string[] vocabById = null;
		private static Dictionary<string, ushort> vocabByString = null;
		private static Dictionary<string, int> merges = null;

		private static string BlankSpace => vocabById[29871];

		static LlamaTokenizer()
		{
			decodeVocabulary();
			decompressMerges();
		}

		private static void decodeVocabulary()
		{
			byte[] byteArray = Convert.FromBase64String(LlamaTokens.vocab_base64);
			vocabById = Encoding.UTF8.GetString(byteArray).Split('\n');
			vocabByString = new Dictionary<string, ushort>();
			for (uint i = 0; i < vocabById.Length; ++i)
				vocabByString.Add(vocabById[i], Convert.ToUInt16(i));
		}

		private static string getMergeIdentifierString(ushort firstTokenId, ushort secondTokenId)
		{
			return string.Concat(vocabById[firstTokenId], " ", vocabById[secondTokenId]);
		}

		private static string utf8ByteToHex(char c) {
			return string.Format("<0x{0:X2}>", Convert.ToByte(c));
		}

		private static byte hexToUtf8Byte(string hex)
		{
			hex = hex.Remove(0, 3);
			hex = hex.Remove(hex.Length - 1, 1);
			int n;
			if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.CurrentCulture, out n))
				return Convert.ToByte(n);
			return 0x32; // ' '
		}


		private static ushort[] mapCharactersToTokenIds(string prompt, bool add_bos_token, bool add_preceding_space)
		{
			List<ushort> tokenIds = new List<ushort>(1024);

			// Special "beginning of string" token.
			if (add_bos_token)
				tokenIds.Add(1);

			StringBuilder sb = new StringBuilder(prompt);

			// Special "preceding space" added to beginning of prompt.
			if (add_preceding_space)
				sb.Insert(0, " ");
			// Special: spaces are represented as thick underscore ▁ (id 29871)
			sb.Replace(" ", BlankSpace);
			prompt = sb.ToString();

			// UTF-32 because C# can't handle chars larger than 0xFFFF
			byte[] u32Array = Encoding.UTF32.GetBytes(prompt);

			// Transform each character to its corresponding token
			for (int i = 0; i < u32Array.Length; i += 4)
			{
				string c = new string(Encoding.UTF32.GetChars(new byte[] {
					u32Array[i + 0],
					u32Array[i + 1],
					u32Array[i + 2],
					u32Array[i + 3],
				}));

				if (vocabByString.ContainsKey(c))
				{
					// Typical case
					tokenIds.Add(vocabByString[c]);
				}
				else
				{
					byte[] bytes = Encoding.UTF8.GetBytes(c.ToCharArray());
					for (int j = 0; j < bytes.Length; ++j)
					{
						var hex = utf8ByteToHex(Convert.ToChar(bytes[j]));
						tokenIds.Add(vocabByString[hex]);
					}

				}
			}
			return tokenIds.ToArray();
		}

		private static void decompressMerges()
		{
			// Base64 decode binary.
			byte[] byteArray = Convert.FromBase64String(LlamaTokens.merges_binary);

			// Each byte-pair represents a tokenId.
			// Convert byte-pairs to tokenIds (integers between 0 and 32000).
			List<ushort> tokenIds = new List<ushort>(1024);
			for (int i = 0; i < byteArray.Length; i += 2)
			{
				int byte1 = byteArray[i];
				int byte2 = byteArray[i + 1];
				ushort tokenId = Convert.ToUInt16(byte1 + (byte2 << 8));
				tokenIds.Add(tokenId);
			}

			// Each pair of tokenIds represents a merge.
			merges = new Dictionary<string, int>();
			for (int i = 0; i < tokenIds.Count; i += 2)
			{
				ushort id1 = tokenIds[i];
				ushort id2 = tokenIds[i + 1];

				string mergeIdentifierString = getMergeIdentifierString(id1, id2);
				
				// Key identifies token pair, value represents merge priority
				merges.Add(mergeIdentifierString, i + 1);
			}
		}


		private class MergeNode : IComparable<MergeNode>
		{
			public int origPos;
			public ushort tokenId;
			public MergeNode prev;
			public MergeNode next;
			public int mergePrio;
			public string mergeToString;
			public bool deleted = false;

			public int CompareTo(MergeNode other)
			{
				return this.mergePrio.CompareTo(other.mergePrio);
			}
		}

		public static ushort[] encode(string prompt, bool add_bos_token = true, bool add_preceding_space = true)
		{
			if (vocabById == null || vocabByString == null)
				return new ushort[0]; // Error

			if (string.IsNullOrEmpty(prompt))
				return new ushort[0];

			var tokenIds = mapCharactersToTokenIds(prompt, add_bos_token, add_preceding_space);

			var mergeQueue = new PriorityQueue<MergeNode>();

			// Merge priority is primarily determined by the location of the merge in the "merges" data,
			// secondarily determined by the relative position of the node in the linked list
			// (We want to perform equal merges from left to right)
			Action<MergeNode> addToMergeQueue = (MergeNode leftNode) => {
				string mergeIdentifierString = getMergeIdentifierString(leftNode.tokenId, leftNode.next.tokenId);
				int mergePrio;
				if (merges.TryGetValue(mergeIdentifierString, out mergePrio))
				{
					mergePrio += leftNode.origPos / prompt.Length;
					if (mergePrio != 0)
					{
						// If mergePrio not found in merges, that means this merge is not possible according to vocabulary.
						leftNode.mergePrio = mergePrio;
						leftNode.mergeToString = mergeIdentifierString.Replace(" ", "");
						mergeQueue.push(leftNode);
					}
				}
			};

			var firstTokenNode = new MergeNode() {
				origPos = 0,
				tokenId = tokenIds[0],
				prev = null,
				next = null,
			};
			var prevTokenNode = firstTokenNode;
			for (int i = 1; i < tokenIds.Length; ++i)
			{
				var currTokenNode = new MergeNode() {
					origPos = i,
					tokenId = tokenIds[i],
					prev = prevTokenNode,
					next = null,
				};
				prevTokenNode.next = currTokenNode;
				addToMergeQueue(prevTokenNode);
				prevTokenNode = currTokenNode;
			}

			// Perform merges in priority order
			while (!mergeQueue.isEmpty())
			{
				var leftOfMerge = mergeQueue.pop();

				// Check that this merge is still possible
				if (leftOfMerge.deleted) continue;
				if (leftOfMerge.next == null) continue;
				if (leftOfMerge.next.deleted) continue;

				// Mark leftOfMerge and rightOfMerge as being deleted, because they are actually being replaced by a merged token.
				leftOfMerge.deleted = true;
				leftOfMerge.next.deleted = true;
				// It's a little bit more complicated to fix the prev of leftOfMerge.
				if (leftOfMerge.prev != null)
				{
					var oldPrev = leftOfMerge.prev;
					// Mark oldPrev as deleted, to avoid erroneous merges later (ref to this node might exist in priorityqueue)
					oldPrev.deleted = true;
					// Replace oldPrev within the linked list with a copy of itself
					var newPrev = new MergeNode {
						origPos = oldPrev.origPos,
						tokenId = oldPrev.tokenId,
						prev = oldPrev.prev,
						next = oldPrev.next,
					};
					leftOfMerge.prev = newPrev;
					// Update linked list reference of "prev of prev"
					if (newPrev.prev != null)
					{
						newPrev.prev.next = newPrev;
					}
					else
					{
						// If "prev of prev" does not exist, that means newPrev must be the new firstNode
						firstTokenNode = newPrev;
					}
				}

				// Create node representing merge result
				var resultOfMerge = new MergeNode {
					origPos = leftOfMerge.origPos,
					tokenId = vocabByString[leftOfMerge.mergeToString],
					prev = leftOfMerge.prev,
					next = leftOfMerge.next.next,
				};
				// Consider adding to merge queue: prev--resultOfMerge
				if (resultOfMerge.prev != null)
				{
					resultOfMerge.prev.next = resultOfMerge;
					addToMergeQueue(resultOfMerge.prev);
				}
				else
				{
					// If prev does not exist then this is the new firstNode
					firstTokenNode = resultOfMerge;
				}
				// Consider adding to merge queue: resultOfMerge--next
				if (resultOfMerge.next != null)
				{
					resultOfMerge.next.prev = resultOfMerge;
					addToMergeQueue(resultOfMerge);
				}
			}

			// Get final tokenIds by traversing the linked list
			List<ushort> mergedTokenIds = new List<ushort>(1024);
			for (var currTokenNode = firstTokenNode; currTokenNode != null; currTokenNode = currTokenNode.next)
				mergedTokenIds.Add(currTokenNode.tokenId);

			return mergedTokenIds.ToArray();
		}

		public static string decode(ushort[] tokenIds, bool add_bos_token = true, bool add_preceding_space = true) 
		{
			if (tokenIds == null || tokenIds.Length == 0)
				return "";

			var utf8byteVals = new List<byte>();
			var startIndex = add_bos_token ? 1 : 0;
			for (var i = startIndex; i < tokenIds.Length; ++i)
			{
				var tokenId = tokenIds[i];
				var tokenString = vocabById[tokenId];
				if (tokenString.StartsWith("<0x") && tokenString.EndsWith(">"))
				{
					// Special case
					var utf8byte = hexToUtf8Byte(tokenString);
					utf8byteVals.Add(utf8byte);
				}
				else
				{
					// Typical case
					var utf8bytes = Encoding.UTF8.GetBytes(tokenString);
					utf8byteVals.AddRange(utf8bytes);
				}
			}

			string decodedString = new string(Encoding.UTF8.GetChars(utf8byteVals.ToArray()));

			var spacesFixed = decodedString.Replace(BlankSpace, " ");
			// Note that preceding space must be removed here at string level, not earlier at token level, because multiple consecutive spaces are represented as single token.
			return add_preceding_space ? spacesFixed.Remove(0, 1) : spacesFixed;
		}
	}
}
