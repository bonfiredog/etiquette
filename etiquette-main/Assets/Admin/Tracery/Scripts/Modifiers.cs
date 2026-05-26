using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

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
    /// Consonants that are never doubled before a suffix in English.
    /// w (sew→sewing), h, x (mix→mixing, because x=/ks/), y (play→playing).
    /// </summary>
    private static readonly string non_doubling_consonants = "whxy";

    /// <summary>
    /// Irregular plural forms, including Latin/Greek-origin words and invariant plurals.
    /// Checked before any rule-based logic in Pluralize.
    /// </summary>
    private static readonly Dictionary<string, string> irregular_plurals =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            // True irregulars
            { "goose",      "geese"      },
            { "mouse",      "mice"       },
            { "louse",      "lice"       },
            { "man",        "men"        },
            { "woman",      "women"      },
             { "old man",        "old men"        },
            { "old woman",      "old women"      },
            { "child",      "children"   },
            { "foot",       "feet"       },
            { "tooth",      "teeth"      },
            { "ox",         "oxen"       },
            { "person",     "people"     },
            // Latin/Greek -us → -i
            { "cactus",      "cacti"       },
            { "focus",       "foci"        },
            { "fungus",      "fungi"       },
            { "nucleus",     "nuclei"      },
            { "syllabus",    "syllabi"     },
            { "alumnus",     "alumni"      },
            { "radius",      "radii"       },
            { "stimulus",    "stimuli"     },
            { "calculus",    "calculi"     },
            { "gladius",     "gladii"      },
            // Latin/Greek -um → -a
            { "datum",       "data"        },
            { "medium",      "media"       },
            { "millennium",  "millennia"   },
            { "bacterium",   "bacteria"    },
            { "curriculum",  "curricula"   },
            { "erratum",     "errata"      },
            { "stratum",     "strata"      },
            { "aquarium",    "aquaria"     },
            { "symposium",   "symposia"    },
            { "stadium",     "stadia"      },
            { "memorandum",  "memoranda"   },
            { "referendum",  "referenda"   },
            { "addendum",    "addenda"     },
            { "corrigendum", "corrigenda"  },
            // Latin/Greek -on → -a
            { "phenomenon",  "phenomena"   },
            { "criterion",   "criteria"    },
            { "automaton",   "automata"    },
            // Latin/Greek -is → -es
            { "analysis",    "analyses"    },
            { "ellipsis",    "ellipses"    },
            { "oasis",       "oases"       },
            { "thesis",      "theses"      },
            { "crisis",      "crises"      },
            { "basis",       "bases"       },
            { "axis",        "axes"        },
            { "diagnosis",   "diagnoses"   },
            { "hypothesis",  "hypotheses"  },
            { "parenthesis", "parentheses" },
            { "synopsis",    "synopses"    },
            // Invariant (same in singular and plural)
            { "sheep",      "sheep"      },
            { "deer",       "deer"       },
            { "fish",       "fish"       },
            { "moose",      "moose"      },
            { "species",    "species"    },
            { "series",     "series"     },
            { "aircraft",   "aircraft"   },
            // -o words that take -oes rather than -os
            { "hero",       "heroes"     },
            { "potato",     "potatoes"   },
            { "tomato",     "tomatoes"   },
            { "echo",       "echoes"     },
            { "torpedo",    "torpedoes"  },
            { "veto",       "vetoes"     },
            { "volcano",    "volcanoes"  },
            { "cargo",      "cargoes"    },
        };

    // -------------------------------------------------------------------------
    // Public modifiers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Prefixes the string with 'a' or 'an' as appropriate.
    /// </summary>
    public static string Article(string str) {
      if (string.IsNullOrEmpty(str)) return str;
      return IsVowel(str[0]) ? "an " + str : "a " + str;
    }

    /// <summary>
    /// Replaces all s with zzz, like how bees speak.
    /// </summary>
    public static string BeeSpeak(string str) {
      return str.Replace("s", "zzz");
    }

    /// <summary>
    /// Capitalizes the first character of the string.
    /// </summary>
    public static string Capitalize(string str) {
      if (string.IsNullOrEmpty(str)) return str;
      return char.ToUpper(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Converts the entire string to uppercase.
    /// </summary>
    public static string CapitalizeAll(string str) {
      return CultureInfo.CurrentCulture.TextInfo.ToUpper(str);
    }

    /// <summary>
    /// Converts the entire string to uppercase (ALL CAPS), culture-invariant.
    /// </summary>
    public static string AllCaps(string str) {
      return str.ToUpperInvariant();
    }

    /// <summary>
    /// Places a comma after the string unless it already ends with sentence punctuation.
    /// </summary>
    public static string Comma(string str) {
      if (str.Length == 0) return ",";
      var lastChar = str[str.Length - 1].ToString();
      return sentence_punctuation.Contains(lastChar) ? str : str + ",";
    }

    /// <summary>
    /// Wraps the given string in double-quotes.
    /// </summary>
    public static string InQuotes(string str) {
      return "\"" + str + "\"";
    }

    /// <summary>
    /// Converts a verb to its simple past tense.
    /// Handles:
    ///   consonant + y  → -ied    (carry → carried)
    ///   vowel + y      → -ed     (hackney → hackneyed)
    ///   silent e       → -d      (type → typed)
    ///   default        → -ed
    /// </summary>
    public static string PastTense(string str) {
      if (str.Length == 0) return "";

      var rest = "";
      var index = str.IndexOf(' ');
      if (index > 0) {
        rest = str.Substring(index);
        str  = str.Substring(0, index);
      }

      var lastChar = str[str.Length - 1];

      switch (lastChar) {
        case 'y':
          // carried → consonant before y; hackneyed → vowel before y
          if (str.Length > 1 && !IsVowel(str[str.Length - 2]))
            return str.Substring(0, str.Length - 1) + "ied" + rest;
          return str + "ed" + rest;

        case 'e':
          // typed, cased
          return str + "d" + rest;

        default:
          return str + "ed" + rest;
      }
    }

    /// <summary>
    /// Pluralises the given string.
    ///
    /// Irregular forms are resolved first via a lookup table, covering:
    ///   - True irregulars          (goose → geese, man → men, child → children)
    ///   - Latin/Greek -us → -i     (cactus → cacti, fungus → fungi)
    ///   - Latin/Greek -um → -a     (millennium → millennia, datum → data)
    ///   - Latin/Greek -on → -a     (phenomenon → phenomena, criterion → criteria)
    ///   - Latin/Greek -is → -es    (analysis → analyses, crisis → crises)
    ///   - Invariant plurals        (sheep, deer, fish, moose)
    ///   - -o words taking -oes     (potato → potatoes, hero → heroes)
    ///
    /// Rule-based cases then handle:
    ///   consonant + y  → -ies       (harpy → harpies)
    ///   vowel + y      → -s         (ray → rays)
    ///   s, x           → -es        (boss → bosses, fox → foxes)
    ///   ch, sh         → -es        (branch → branches, wish → wishes)
    ///   other -h       → -s         (stomach → stomachs)
    ///   CVC -z         → -zes       (quiz → quizzes)
    ///   other -z       → -es        (topaz → topazes)
    ///   ff             → -s         (staff → staffs)
    ///   other -f       → -ves       (leaf → leaves)
    ///   -fe            → -ves       (knife → knives)
    ///   default        → -s
    /// </summary>
    public static string Pluralize(string str) {
      if (str.Length == 0) return str;

      // Irregular forms take priority over all rules.
      if (irregular_plurals.TryGetValue(str, out var irregular))
        return irregular;

      var lastChar         = str[str.Length - 1];
      var secondToLastChar = str.Length > 1 ? str[str.Length - 2] : '\0';

      switch (lastChar) {
        case 'y':
          // rays, convoys → +s;  harpies, cries → -y +ies
          return IsVowel(secondToLastChar)
              ? str + "s"
              : str.Substring(0, str.Length - 1) + "ies";

        case 's':
        case 'x':
          // boss → bosses, fox → foxes
          return str + "es";

        case 'h':
          // ch/sh → +es (branch → branches, wish → wishes)
          // other → +s  (stomach → stomachs)
          return (secondToLastChar == 'c' || secondToLastChar == 's')
              ? str + "es"
              : str + "s";

        case 'z':
          // Short CVC words double the z (quiz → quizzes).
          // Longer/vowel-before-z words use +es (topaz → topazes).
          return (str.Length >= 3 && !IsVowel(secondToLastChar) && IsVowel(str[str.Length - 3]))
              ? str + "zes"
              : str + "es";

        case 'f':
          // staff, cliff → +s (double-f keeps the f)
          // leaf → leaves, wolf → wolves
          return secondToLastChar == 'f'
              ? str + "s"
              : str.Substring(0, str.Length - 1) + "ves";

        case 'e':
          // knife → knives, wife → wives (-fe → -ves)
          return secondToLastChar == 'f'
              ? str.Substring(0, str.Length - 2) + "ves"
              : str + "s";

        default:
          return str + "s";
      }
    }

    /// <summary>
    /// Title-cases the given string.
    /// </summary>
    public static string TitleCase(string str) {
      return title_case_regex.Replace(str, m => m.Value.ToUpper());
    }

    /// <summary>
    /// Converts a verb to its gerund (-ing) form.
    ///
    /// Handles:
    ///   -ie            → -ying      (die → dying, lie → lying)
    ///   silent -e      → -ing       (make → making, write → writing)
    ///   vowel + e      → -ing       (agree → agreeing, hoe → hoeing)
    ///   CVC consonant  → double+ing (run → running, jog → jogging)
    ///     (w, h, x, y are never doubled)
    ///   default        → -ing
    /// </summary>
    public static string Gerund(string str) {
      if (string.IsNullOrEmpty(str)) return str;

      var rest  = "";
      var index = str.IndexOf(' ');
      if (index > 0) {
        rest = str.Substring(index);
        str  = str.Substring(0, index);
      }

      var lastChar = str[str.Length - 1];

      // die → dying, lie → lying, tie → tying
      if (lastChar == 'e' && str.Length > 1 && str[str.Length - 2] == 'i')
        return str.Substring(0, str.Length - 2) + "ying" + rest;

      // Silent e (make → making); vowel+e kept (agree → agreeing, hoe → hoeing)
      if (lastChar == 'e' && str.Length > 1) {
        return IsVowel(str[str.Length - 2])
            ? str + "ing" + rest
            : str.Substring(0, str.Length - 1) + "ing" + rest;
      }

      // CVC doubling: run → running, jog → jogging, sit → sitting
      // w/h/x/y are excluded — they are never doubled.
      if (str.Length >= 3
          && !IsVowel(lastChar)
          && non_doubling_consonants.IndexOf(lastChar) < 0
          && IsVowel(str[str.Length - 2])
          && !IsVowel(str[str.Length - 3])) {
        return str + lastChar + "ing" + rest;
      }

      return str + "ing" + rest;
    }

    /// <summary>
    /// Converts a verb or adjective to its -er comparative/agent form.
    ///
    /// Handles:
    ///   -e             → -er        (write → writer, make → maker)
    ///   CVC consonant  → double+er  (run → runner, big → bigger)
    ///     (w, h, x, y are never doubled)
    ///   consonant + y  → -ier       (carry → carrier)
    ///   default        → -er
    /// </summary>
    public static string Er(string str) {
      if (string.IsNullOrEmpty(str)) return str;

      var rest  = "";
      var index = str.IndexOf(' ');
      if (index > 0) {
        rest = str.Substring(index);
        str  = str.Substring(0, index);
      }

      var lastChar = str[str.Length - 1];

      // write → writer, make → maker
      if (lastChar == 'e')
        return str + "r" + rest;

      // run → runner, big → bigger
      // w/h/x/y are excluded — they are never doubled.
      if (str.Length >= 3
          && !IsVowel(lastChar)
          && non_doubling_consonants.IndexOf(lastChar) < 0
          && IsVowel(str[str.Length - 2])
          && !IsVowel(str[str.Length - 3])) {
        return str + lastChar + "er" + rest;
      }

      // carry → carrier
      if (lastChar == 'y' && str.Length > 1 && !IsVowel(str[str.Length - 2]))
        return str.Substring(0, str.Length - 1) + "ier" + rest;

      return str + "er" + rest;
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns true if the character is a standard English vowel (a, e, i, o, u).
    /// Note: 'y' is intentionally excluded; it is treated as a consonant here.
    /// </summary>
    private static bool IsVowel(char c) {
      return vowels.IndexOf(c) >= 0;
    }
  }
}