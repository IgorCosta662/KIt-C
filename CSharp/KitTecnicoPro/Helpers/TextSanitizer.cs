using System.Text.RegularExpressions;

namespace KitTecnicoPro.Helpers
{
    /// <summary>Normaliza textos exibidos ao usuário (remove emojis e símbolos decorativos).</summary>
    public static partial class TextSanitizer
    {
        // Pares substitutos (emojis fora do BMP) + blocos de símbolos/dingbats + seletores de variação.
        [GeneratedRegex(@"(?:\p{Cs}|[\u2139\u231A\u231B\u23E9-\u23FA\u25A0-\u25FF\u2600-\u27BF\u2B00-\u2BFF\uFE0F\u200D\u20E3])+ ?")]
        private static partial Regex EmojiRegex();

        /// <summary>Remove emojis preservando a indentação inicial da mensagem.</summary>
        public static string StripEmoji(string? text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            return EmojiRegex().Replace(text, string.Empty).TrimEnd();
        }
    }
}

