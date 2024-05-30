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
    public class Expression
    {
        public ExpressionType Type { get; private set; }
        public string Content { get; private set; }
        public string Group { get; private set; }
        public int Order { get; private set; }

        public Expression(string content, ExpressionType type, string group, int order)
        {
            if (content == null)
                throw new ArgumentNullException("content");
            if (group == null)
                throw new ArgumentNullException("group");

            Type = type;
            Content = content;
            Group = group;
            Order = order;
        }

        public Expression(string content, ExpressionType type)
            : this(content, type, String.Empty, -1)
        {
        }

        public override string ToString()
        {
            if (Type == ExpressionType.Newline)
                return String.Format("({0})", Type);

            return String.Format("({0} --> {1}{2})", Content, Type, Group.Length > 0 ? " --> " + Group : String.Empty);
        }
    }
}
