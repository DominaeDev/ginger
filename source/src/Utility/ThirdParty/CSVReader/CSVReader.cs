/* CSVReader - a simple open source C# class library to read CSV data
 * by Andrew Stellman - http://www.stellman-greene.com/CSVReader
 * 
 * CSVReader.cs - Class to read CSV data from a string, file or stream
 * 
 * download the latest version: http://svn.stellman-greene.com/CSVReader
 * 
 * (c) 2008, Stellman & Greene Consulting
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of Stellman & Greene Consulting nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY STELLMAN & GREENE CONSULTING ''AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL STELLMAN & GREENE CONSULTING BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */

 /*
  * Updated by DominaeDev in 2024
  * + Support linebreaks in fields.
  * + Stripped out data conversion. Nothing but strings, baby.
  */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Com.StellmanGreene.CSVReader
{
	/// <summary>
	/// Read CSV-formatted data from a file or TextReader
	/// </summary>
	public class CSVReader : IDisposable
	{
		public const string NEWLINE = "\r\n";

		/// <summary>
		/// This reader will read all of the CSV data
		/// </summary>
		private BinaryReader reader;

		/// <summary>
		/// The number of rows to scan for types when building a DataTable (0 to scan the whole file)
		/// </summary>
		public int ScanRows = 0;

		#region Constructors

		/// <summary>
		/// Read CSV-formatted data from a string
		/// </summary>
		/// <param name="csvData">String containing CSV data</param>
		public CSVReader(string csvData)
		{
			if (csvData == null)
				throw new ArgumentNullException("Null string passed to CSVReader");

			this.reader = new BinaryReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvData)));
		}

		/// <summary>
		/// Read CSV-formatted data from a TextReader
		/// </summary>
		/// <param name="reader">TextReader that's reading CSV-formatted data</param>
		public CSVReader(TextReader reader)
		{
			if (reader == null)
				throw new ArgumentNullException("Null TextReader passed to CSVReader");

			this.reader = new BinaryReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(reader.ReadToEnd())));
		}

		#endregion


		string currentLine = "";
		/// <summary>
		/// Read the next row from the CSV data
		/// </summary>
		/// <returns>A list of objects read from the row, or null if there is no next row</returns>
		public List<string> ReadRow()
		{
			// ReadLine() will return null if there's no next line
			if (reader.BaseStream.Position >= reader.BaseStream.Length)
				return null;

			StringBuilder builder = new StringBuilder();

			// Read the next line
			int nQuotes = 0;
			while ((reader.BaseStream.Position < reader.BaseStream.Length) && !(builder.ToString().EndsWith(NEWLINE) && nQuotes % 2 == 0))
			{
				char c = reader.ReadChar();
				if (c == '"')
					nQuotes++;
				builder.Append(c);
			}

			currentLine = builder.ToString();
			if (currentLine.EndsWith(NEWLINE))
				currentLine = currentLine.Remove(currentLine.Length - NEWLINE.Length, NEWLINE.Length);

			// Build the list of objects in the line
			List<string> objects = new List<string>();
			while (currentLine != "")
				objects.Add(ReadNextObject());
			return objects;
		}

		/// <summary>
		/// Read the next object from the currentLine string
		/// </summary>
		/// <returns>The next object in the currentLine string</returns>
		private string ReadNextObject()
		{
			if (currentLine == null)
				return null;

			// Check to see if the next value is quoted
			bool quoted = false;
			if (currentLine.StartsWith("\""))
				quoted = true;

			// Find the end of the next value
			string nextObjectString = "";
			int i = 0;
			int len = currentLine.Length;
			bool foundEnd = false;
			while (!foundEnd && i <= len)
			{
				// Check if we've hit the end of the string
				if ((!quoted && i == len) // non-quoted strings end with a comma or end of line
					|| (!quoted && currentLine.Substring(i, 1) == ",")
					// quoted strings end with a quote followed by a comma or end of line
					|| (quoted && i == len - 1 && currentLine.EndsWith("\""))
					|| (quoted && currentLine.Substring(i, 2) == "\","))
					foundEnd = true;
				else
					i++;
			}
			if (quoted)
			{
				if (i > len || !currentLine.Substring(i, 1).StartsWith("\""))
					throw new FormatException("Invalid CSV format: " + currentLine.Substring(0, i));
				i++;
			}
			nextObjectString = currentLine.Substring(0, i).Replace("\"\"", "\"");

			if (i < len)
				currentLine = currentLine.Substring(i + 1);
			else
				currentLine = "";

			if (quoted)
			{
				if (nextObjectString.StartsWith("\""))
					nextObjectString = nextObjectString.Substring(1);
				if (nextObjectString.EndsWith("\""))
					nextObjectString = nextObjectString.Substring(0, nextObjectString.Length - 1);
				return nextObjectString;
			}
			else
			{
				return nextObjectString;
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (reader != null)
			{
				try
				{
					// Can't call BinaryReader.Dispose due to its protection level
					reader.Close();
				}
				catch { }
			}
		}

		#endregion
	}
}
