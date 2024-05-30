/*
 *  WinFormsSyntaxHighlighter
 * 
 * Copyright (C) 2014 sinairv
 * https://github.com/sinairv/WinFormsSyntaxHighlighter/
 * 
 * License: MIT
 */

using System.Drawing;

namespace WinFormsSyntaxHighlighter
{
    public struct SyntaxStyle
    {
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public Color Color { get; set; }
        public bool Underline { get; set; }
        public bool Monospace { get; set; }

        public SyntaxStyle(Color color, bool bold, bool italic)
        {
            Color = color;
            Bold = bold;
            Italic = italic;
            Underline = false;
            Monospace = false;
        }

        public static SyntaxStyle Monospaced(Color color)
        {
            return new SyntaxStyle(color) {
                Monospace = true,
            };
        }

        public static SyntaxStyle Underlined(Color color)
        {
            return new SyntaxStyle(color) {
                Underline = true,
            };
        }

        public SyntaxStyle(Color color) : this(color, false, false)
        {
        }

        public SyntaxStyle(SyntaxStyle other)
        {
            Color = other.Color;
            Bold = other.Bold;
            Italic = other.Italic;
            Underline = other.Underline;
            Monospace = other.Monospace;
        }
    }
}
