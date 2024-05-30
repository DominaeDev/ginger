using System;

namespace WinFormsSyntaxHighlighter
{
    internal class PatternStyleMap
    {
        public string Name { get; private set; }
        public PatternDefinition PatternDefinition { get; private set; }
        public SyntaxStyle SyntaxStyle { get; private set; }
        public int Order { get; private set; }

        public PatternStyleMap(string name, PatternDefinition patternDefinition, SyntaxStyle syntaxStyle, int order)
        {
            if (patternDefinition == null)
                throw new ArgumentNullException("patternDefinition");
            if (String.IsNullOrEmpty(name))
                throw new ArgumentException("name must not be null or empty", "name");

            Name = name;
            PatternDefinition = patternDefinition;
            SyntaxStyle = syntaxStyle;
            Order = order;
        }
    }
}