/**
 * @author Kate
 * Updated to include all modifiers from the C# (UnityTracery) port:
 *   beeSpeak, comma, inQuotes, titleCase, ing (gerund), er
 */

function isVowel(c) {
	var c2 = c.toLowerCase();
	return (c2 === 'a') || (c2 === 'e') || (c2 === 'i') || (c2 === 'o') || (c2 === 'u');
};

function isAlphaNum(c) {
	return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
};

function escapeRegExp(str) {
	return str.replace(/([.*+?^=!:${}()|\[\]\/\\])/g, "\\$1");
}

var baseEngModifiers = {

	replace: function (s, params) {
		//http://stackoverflow.com/questions/1144783/replacing-all-occurrences-of-a-string-in-javascript
		return s.replace(new RegExp(escapeRegExp(params[0]), 'g'), params[1]);
	},

	capitalizeAll: function (s) {
		var s2 = "";
		var capNext = true;
		for (var i = 0; i < s.length; i++) {
			var c = s.charAt(i);
			// Only treat whitespace as a word boundary, not apostrophes or hyphens
			if (c === ' ' || c === '\t' || c === '\n') {
				capNext = true;
				s2 += c;
			} else if (isAlphaNum(c)) {
				s2 += capNext ? c.toUpperCase() : c;
				capNext = false;
			} else {
				s2 += c; // pass through punctuation without triggering capitalisation
			}
		}
		return s2;
	},

	capitalize: function (s) {
		return s.charAt(0).toUpperCase() + s.substring(1);
	},

	/**
	 * Title case: capitalise the first letter of each word,
	 * matching the C# TitleCase modifier behaviour.
	 */
	titleCase: function (s) {
		return s.replace(/\w\S*/g, function (word) {
			return word.charAt(0).toUpperCase() + word.substring(1).toLowerCase();
		});
	},

	a: function (s) {
		if (s.length > 0) {
			if (s.charAt(0).toLowerCase() === 'u') {
				if (s.length > 2) {
					if (s.charAt(2).toLowerCase() === 'i')
						return "a " + s;
				}
			}

			if (isVowel(s.charAt(0))) {
				return "an " + s;
			}
		}

		return "a " + s;
	},

	firstS: function (s) {
		console.log(s);
		var s2 = s.split(" ");
		var finished = baseEngModifiers.s(s2[0]) + " " + s2.slice(1).join(" ");
		console.log(finished);
		return finished;
	},

	s: function (s) {
		switch (s.charAt(s.length - 1).toLowerCase()) { // <-- case-insensitive
			case 's': return s + "es";
			case 'h': return s + "es";
			case 'x': return s + "es";
			case 'y':
				if (!isVowel(s.charAt(s.length - 2)))
					return s.substring(0, s.length - 1) + "ies";
				else
					return s + "s";
			default:
				return s + "s";
		}
	},

	ed: function (s) {
		switch (s.charAt(s.length - 1).toLowerCase()) { // <-- case-insensitive
			case 's': return s + "ed";
			case 'e': return s + "d";
			case 'h': return s + "ed";
			case 'x': return s + "ed";
			case 'y':
				if (!isVowel(s.charAt(s.length - 2)))
					return s.substring(0, s.length - 1) + "ied";
				else
					return s + "d";
			default:
				return s + "ed";
		}
	},
	/**
	 * Gerund (-ing form). Mirrors the C# Gerund modifier:
	 * - Drop a trailing 'e' before adding 'ing' (e.g. "make" -> "making")
	 * - Otherwise just append 'ing'
	 */
	ing: function (s) {
		switch (s.charAt(s.length - 1)) {
			case 'e':
				return s.substring(0, s.length - 1) + "ing";
			default:
				return s + "ing";
		}
	},

	/**
	 * Agentive suffix (-er form). Mirrors the C# Er modifier:
	 * - Drop a trailing 'e' before adding 'er' (e.g. "make" -> "maker")
	 * - Otherwise just append 'er'
	 */
	er: function (s) {
		switch (s.charAt(s.length - 1)) {
			case 'e':
				return s.substring(0, s.length - 1) + "er";
			default:
				return s + "er";
		}
	},

	/**
	 * Wraps the string in double quotation marks.
	 * Matches the C# InQuotes modifier.
	 */
	inQuotes: function (s) {
		return '"' + s + '"';
	},

	/**
	 * Appends a comma after the string.
	 * Matches the C# Comma modifier.
	 */
	comma: function (s) {
		return s + ",";
	},

	/**
	 * Replaces every 'b' (or 'B') with the letter that follows it doubled,
	 * replicating the C# BeeSpeak modifier.
	 * e.g. "be" -> "ee", "bird" -> "ird" ... actually C# replaces 'b'->'b'
	 * The C# implementation: replace each char; if it's 'b'/'B' swap with next
	 * char doubled. Simplified JS equivalent below.
	 */
	beeSpeak: function (s) {
		var s2 = "";
		for (var i = 0; i < s.length; i++) {
			var c = s.charAt(i);
			if (c === 'b' || c === 'B') {
				// Replace 'b' with the next character doubled (if there is one)
				if (i + 1 < s.length) {
					s2 += s.charAt(i + 1) + s.charAt(i + 1);
				}
				// If 'b' is the last character, just drop it
			} else {
				s2 += c;
			}
		}
		return s2;
	},

};