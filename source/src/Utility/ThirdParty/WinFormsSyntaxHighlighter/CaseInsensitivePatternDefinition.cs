/*
 *  WinFormsSyntaxHighlighter
 * 
 * Copyright (C) 2014 sinairv
 * https://github.com/sinairv/WinFormsSyntaxHighlighter/
 * 
 * License: MIT
 */ 
 
 using System.Collections.Generic;

namespace WinFormsSyntaxHighlighter
{
    public class CaseInsensitivePatternDefinition : PatternDefinition
    {
        public CaseInsensitivePatternDefinition(IEnumerable<string> tokens)
            : base(false, tokens)
        {
        }

        public CaseInsensitivePatternDefinition(params string[] tokens)
            : base(false, tokens)
        {
        }
    }
}
