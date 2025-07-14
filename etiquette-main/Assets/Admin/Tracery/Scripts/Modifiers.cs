using System.Text.RegularExpressions;
using System.Globalization;

namespace UnityTracery {
  /// <summary>
  /// A static class containing all of the built-in ("universal") modifiers that can be applied.
  /// </summary>
  static class Modifiers {
    private static readonly Regex title_case_regex = new Regex(@"(?=^|\s)([a-z])");

    /// <summary>
    /// Punctuation used to end a sentence.
    /// </summary>
    private static readonly string sentence_punctuation = ",.!?";

    /// <summary>
    /// List of all vowels.
    /// </summary>
    private static readonly string vowels = "aeiou";

    /// <summary>
    /// Prefixes the string with 'a' or 'an' as appropriate.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string Article(string str) {
      var lastChar = str[0];

      if (IsVowel(lastChar)) {
        return "an " + str;
      }

      return "a " + str;
    }

    /// <summary>
    /// Replaces all s with zzz, like how bees speak.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string BeeSpeak(string str) {
      return str.Replace("s", "zzz");
    }

    /// <summary>
    /// Capitalizes the given string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string Capitalize(string str) {
      return char.ToUpper(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Capitalizes the entire string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string CapitalizeAll(string str) {
      return CultureInfo.CurrentCulture.TextInfo.ToUpper(str);
    }

    /// <summary>
    /// Places a comma after the string unless it's the end of a sentence.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string Comma(string str) {
      if (str.Length == 0) {
        return ",";
      }
      var lastChar = str.Substring(0, 1);

      if (sentence_punctuation.Contains(lastChar)) {
        return str;
      }

      return str + ",";
    }

    /// <summary>
    /// Wraps the given string in double-quotes.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string InQuotes(string str) {
      return "\"" + str + "\"";
    }

    /// <summary>
    /// Past-tensifies the specified string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string PastTense(string str) {
      if (str.Length == 0) {
        return "";
      }
      var lastChar = str[str.Length - 1];
      var rest = "";

      var index = str.IndexOf(' ');

      if (index > 0) {
        rest = str.Substring(index);
        str = str.Substring(0, index);
      }

      switch (lastChar) {
      case 'y':
        // carried
        if (str.Length > 1 && !IsVowel(str[str.Length - 2])) {
          return str.Substring(0, str.Length - 1) + "ied" + rest;
        }

        // hackneyed
        return str + "ed" + rest;

      case 'e':
        // cased, typed
        return str + "d" + rest;

      default:
        return str + "ed" + rest;
      }
    }

    /// <summary>
    /// Pluralises the given string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string Pluralize(string str) {
      var lastChar = str[str.Length - 1];
      var secondToLastChar = str[str.Length - 2];

      switch (lastChar) {
      case 'y':
        // rays, convoys
        if (IsVowel(secondToLastChar)) {
          return str + "s";
        }

        // harpies, cries
        return str.Substring(0, str.Length - 1) + "ies";
      case 'x':
        // oxen, boxen, foxen
        return str.Substring(0, str.Length - 1) + "xen";
      case 'z':
        return str.Substring(0, str.Length - 1) + "zes";
      case 'h':
        return str.Substring(0, str.Length - 1) + "hes";
      default:
        return str + "s";
      }
    }

    /// <summary>
    /// Title cases the given string.
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string TitleCase(string str) {
      return title_case_regex.Replace(str, "$1");
    }

    /// <summary>
    /// Checks to see if the given character is a consonant.
    /// </summary>
    /// <param name="c">Character to test.</param>
    /// <returns>Whether the character is a consonant.</returns>
    private static bool IsVowel(char c) {
      return vowels.IndexOf(c) >= 0;
    }
  }
}
