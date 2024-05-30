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
    internal class StyleGroupPair
    {
        public int Index { get; set; }
        public SyntaxStyle SyntaxStyle { get; set; }
        public string GroupName { get; set; }

        public StyleGroupPair(SyntaxStyle syntaxStyle, string groupName)
        {
            if (groupName == null)
                throw new ArgumentNullException("groupName");

            SyntaxStyle = syntaxStyle;
            GroupName = groupName;
        }
    }
}
