/*
 *  WinFormsSyntaxHighlighter
 * 
 * Copyright (C) 2014 sinairv
 * https://github.com/sinairv/WinFormsSyntaxHighlighter/
 * 
 * License: MIT
 */

using System;

namespace WinFormsSyntaxHighlighter
{
    public static class StringExtensions
    {
        public static string NormalizeLineBreaks(this string instance, string preferredLineBreak)
        {
            return instance.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", preferredLineBreak);
        }

        public static string NormalizeLineBreaks(this string instance)
        {
            return NormalizeLineBreaks(instance, Environment.NewLine);
        }
    }
}
