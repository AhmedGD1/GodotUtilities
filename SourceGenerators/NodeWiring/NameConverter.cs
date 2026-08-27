using System.Collections.Generic;
using System.Text;

namespace GodotUtilities.SourceGenerators.NodeWiring;

internal static class NameConverter
{
    public static string ToNodeName(string memberName)
    {
        var sb = new StringBuilder(memberName.Length + 4);
        foreach (var word in SplitWords(memberName))
        {
            AppendCapitalized(sb, word);
        }

        return sb.Length > 0 ? sb.ToString() : memberName;
    }

    public static string ToSnakeCase(string memberName)
    {
        var words = SplitWords(memberName);
        if (words.Count == 0)
        {
            return memberName;
        }

        var sb = new StringBuilder(memberName.Length + 4);
        for (var i = 0; i < words.Count; i++)
        {
            if (i > 0)
            {
                sb.Append('_');
            }
            sb.Append(words[i].ToLowerInvariant());
        }

        return sb.ToString();
    }

    public static string ToCamelCase(string memberName)
    {
        var words = SplitWords(memberName);
        if (words.Count == 0)
        {
            return memberName;
        }

        var sb = new StringBuilder(memberName.Length + 4);
        sb.Append(words[0].ToLowerInvariant());
        for (var i = 1; i < words.Count; i++)
        {
            AppendCapitalized(sb, words[i]);
        }

        return sb.ToString();
    }

    private static void AppendCapitalized(StringBuilder sb, string word)
    {
        sb.Append(char.ToUpperInvariant(word[0]));
        for (var i = 1; i < word.Length; i++)
        {
            sb.Append(char.ToLowerInvariant(word[i]));
        }
    }

    private static List<string> SplitWords(string memberName)
    {
        var words = new List<string>();

        var start = 0;
        while (start < memberName.Length && memberName[start] == '_')
        {
            start++;
        }

        if (start >= memberName.Length)
        {
            return words;
        }

        var current = new StringBuilder();

        for (var i = start; i < memberName.Length; i++)
        {
            var c = memberName[i];

            if (c == '_')
            {
                if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }
                continue;
            }

            var isNewWord = current.Length > 0 &&
                ((char.IsUpper(c) && char.IsLower(memberName[i - 1]))
                 || (char.IsLetter(c) && char.IsDigit(memberName[i - 1])));

            if (isNewWord)
            {
                words.Add(current.ToString());
                current.Clear();
            }

            current.Append(c);
        }

        if (current.Length > 0)
        {
            words.Add(current.ToString());
        }

        return words;
    }
}
