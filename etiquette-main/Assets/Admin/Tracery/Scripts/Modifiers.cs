using System.Text.RegularExpressions;
using System.Globalization;

namespace UnityTracery {
  /// <summary>
  /// A static class containing all of the built-in ("universal") modifiers that can be applied.
  /// </summary>
  static class Modifiers {
    private static readonly Regex title_case_regex = new Regex(@"(?:^|\s)([a-z])");

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
      if (string.IsNullOrEmpty(str)) return str;
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
      if (string.IsNullOrEmpty(str)) return str;
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
    /// Converts the entire string to uppercase (ALL CAPS).
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string AllCaps(string str) {
      return str.ToUpperInvariant();
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
      var lastChar = str[str.Length - 1].ToString();

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
    /// Handles the following cases:
    ///   - consonant + y  → -ies       (harpy → harpies)
    ///   - vowel + y      → -s         (ray → rays)
    ///   - s, ss, x       → -es        (boss → bosses, fox → foxes)
    ///   - ch, sh         → -es        (branch → branches, wish → wishes)
    ///   - other -h       → -s         (stomach → stomachs)
    ///   - CVC -z         → -zes       (quiz → quizzes)
    ///   - other -z       → -es        (topaz → topazes)
    ///   - ff             → -s         (staff → staffs)
    ///   - other -f       → -ves       (leaf → leaves)
    ///   - -fe            → -ves       (knife → knives)
    ///   - default        → -s
    /// </summary>
    /// <param name="str">The string to modify.</param>
    /// <returns>The modified string.</returns>
    public static string Pluralize(string str) {
      if (str.Length == 0) return str;
      var lastChar = str[str.Length - 1];
      var secondToLastChar = str.Length > 1 ? str[str.Length - 2] : '\0';

      switch (lastChar) {
        case 'y':
          // rays, convoys → +s; harpies, cries → -y +ies
          if (IsVowel(secondToLastChar)) {
            return str + "s";
          }
          return str.Substring(0, str.Length - 1) + "ies";

        case 's':
        case 'x':
          // boss → bosses, fox → foxes
          return str + "es";

        case 'h':
          // ch/sh → +es (branch → branches, wish → wishes)
          // other → +s  (stomach → stomachs)
          if (secondToLastChar == 'c' || secondToLastChar == 's') {
            return str + "es";
          }
          return str + "s";

        case 'z':
          // Short CVC words: double the z (quiz → quizzes)
          // Longer/vowel-before-z words: just +es (topaz → topazes)
          if (str.Length >= 3 && !IsVowel(secondToLastChar) && IsVowel(str[str.Length - 3])) {
            return str + "zes";
          }
          return str + "es";

        case 'f':
          // staff, cliff → +s (double-f words keep the f)
          if (secondToLastChar == 'f') {
            return str + "s";
          }
          // leaf → leaves, wolf → wolves
          return str.Substring(0, str.Length - 1) + "ves";

        case 'e':
          // knife → knives, wife → wives (-fe → -ves)
          if (secondToLastChar == 'f') {
            return str.Substring(0, str.Length - 2) + "ves";
          }
          return str + "s";

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
      return title_case_regex.Replace(str, m => m.Value.ToUpper());
    }

    /// <summary>
    /// Checks to see if the given character is a vowel.
    /// </summary>
    /// <param name="c">Character to test.</param>
    /// <returns>Whether the character is a vowel.</returns>
    private static bool IsVowel(char c) {
      return vowels.IndexOf(c) >= 0;
    }

    /// <summary>
    /// Converts a verb to its gerund (-ing) form.
    /// </summary>
    public static string Gerund(string str) {
      if (string.IsNullOrEmpty(str)) return str;

      var index = str.IndexOf(' ');
      var rest = "";
      if (index > 0) {
        rest = str.Substring(index);
        str = str.Substring(0, index);
      }

      var lastChar = str[str.Length - 1];

      // e.g. "make" → "making", "write" → "writing"
      if (lastChar == 'e' && str.Length > 1) {
        return str.Substring(0, str.Length - 1) + "ing" + rest;
      }

      // e.g. "run" → "running", "sit" → "sitting"
      // Double the consonant if: CVC pattern and last char is a consonant
      if (str.Length >= 3
          && !IsVowel(lastChar)
          && IsVowel(str[str.Length - 2])
          && !IsVowel(str[str.Length - 3])) {
        return str + lastChar + "ing" + rest;
      }

      return str + "ing" + rest;
    }

    /// <summary>
    /// Converts a verb or adjective to its -er comparative/agent form.
    /// </summary>
    public static string Er(string str) {
      if (string.IsNullOrEmpty(str)) return str;

      var index = str.IndexOf(' ');
      var rest = "";
      if (index > 0) {
        rest = str.Substring(index);
        str = str.Substring(0, index);
      }

      var lastChar = str[str.Length - 1];

      // e.g. "write" → "writer", "make" → "maker"
      if (lastChar == 'e') {
        return str + "r" + rest;
      }

      // e.g. "run" → "runner", "big" → "bigger"
      if (str.Length >= 3
          && !IsVowel(lastChar)
          && IsVowel(str[str.Length - 2])
          && !IsVowel(str[str.Length - 3])) {
        return str + lastChar + "er" + rest;
      }

      // e.g. "carry" → "carrier"
      if (lastChar == 'y' && str.Length > 1 && !IsVowel(str[str.Length - 2])) {
        return str.Substring(0, str.Length - 1) + "ier" + rest;
      }

      return str + "er" + rest;
    }
  }
}