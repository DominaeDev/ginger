/*
 *  WinFormsSyntaxHighlighter
 * 
 * Copyright (C) 2014 sinairv
 * https://github.com/sinairv/WinFormsSyntaxHighlighter/
 * 
 * License: MIT
 */

namespace WinFormsSyntaxHighlighter
{
    /// <summary>
    /// Enumerates the type of the parsed content
    /// </summary>
    public enum ExpressionType
    {
        None = 0,
        Identifier, // i.e. a word which is neither keyword nor inside any word-group
        Operator,
        Number,
        Whitespace,
        Newline,
        Keyword,
        Comment,
        CommentLine,
        String,
        DelimitedGroup,       // needs extra argument
        WordGroup             // needs extra argument
    }
}
