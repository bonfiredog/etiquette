"""
Word List Filter - Updated Version
----------------------------------
Supports:
- New sentiment adjective/adverb categories
- random_noun_phrases
- Safer token-cleaning rules
"""

import json
import re
from typing import List, Dict


class WordFilter:
    def __init__(self):
        """Initialize filter with rules"""

        # Common English words (always keep)
        self.whitelist = {
            'oh', 'ah', 'uh', 'no', 'yes', 'hey', 'wow',
            'a', 'i', 'we', 'he', 'she', 'it',
            'be', 'do', 'go', 'in', 'on', 'at', 'to',
            'of', 'or', 'if', 'so', 'and', 'but', 'for',
            'nor', 'yet', 'the'
        }

        # Updated POS minimum length table
        self.min_lengths = {
            'nouns': 3,
            'proper_nouns': 3,
            'verbs': 2,
            'adpositions': 2,
            'noun_phrases': 5,

            # Sentiment adjectives
            'positive_adjectives': 3,
            'negative_adjectives': 3,
            'neutral_adjectives': 3,

            # Sentiment adverbs
            'positive_adverbs': 3,
            'negative_adverbs': 3,
            'neutral_adverbs': 3,

            # New category
            'random_noun_phrases': 5,
        }

    def is_valid_token(self, word: str, pos_category: str) -> bool:
        """Check validity of a token based on filtering rules."""
        # Whitelist
        if word.lower() in self.whitelist:
            return True

        # Length rule
        min_len = self.min_lengths.get(pos_category, 3)
        if len(word) < min_len:
            return False

        # Weird punctuation
        if re.search(r'["\'`!@#$%^&*()\[\]{}\\/|~]', word):
            return False

        # Broken hyphenation
        if word.endswith('-') or word.startswith('-'):
            return False
        if '--' in word:
            return False

        # Weird OCR casing
        if word[0].islower() and any(c.isupper() for c in word[1:]):
            capitals = sum(1 for c in word if c.isupper())
            if capitals > 1:
                return False

        # Digits (except proper nouns)
        if pos_category != 'proper_nouns' and any(c.isdigit() for c in word):
            return False

        # All caps short noise
        if word.isupper() and len(word) <= 3:
            return False

        # Too many dots
        if word.count('.') > 1:
            return False

        # Excessively long (OCR join)
        if len(word) > 30:
            return False

        # Whitespace noise
        if any(c in word for c in ['\n', '\r', '\t', '  ']):
            return False

        # Ends with punctuation
        if word[0] in '.,;:!?' or word[-1] in '.,;:!?':
            return False

        # No vowels (likely noise)
        if len(word) > 4 and pos_category not in ('interjections', 'random_noun_phrases'):
            if not any(c in 'aeiouAEIOU' for c in word):
                return False

        return True

    def filter_category(self, words: List[str], pos_category: str) -> List[str]:
        """Apply filtering rules to one category."""
        filtered = [w for w in words if self.is_valid_token(w, pos_category)]
        return sorted(list(set(filtered)))

    def filter_extracted_words(
        self,
        input_path: str,
        output_path: str = "extracted_words_filtered.json"
    ) -> Dict[str, List[str]]:

        with open(input_path, 'r', encoding='utf-8') as f:
            extracted = json.load(f)

        print("=" * 60)
        print("FILTERING EXTRACTED WORDS")
        print("=" * 60)

        filtered = {}

        for pos_category, words in extracted.items():
            original_count = len(words)
            filtered_words = self.filter_category(words, pos_category)
            filtered_count = len(filtered_words)
            removed = original_count - filtered_count

            filtered[pos_category] = filtered_words

            print(f"{pos_category:30s}: {original_count:5,} → {filtered_count:5,} (-{removed:4,})")

        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(filtered, f, indent=2, ensure_ascii=False)

        print("=" * 60)
        print(f"Saved to: {output_path}")
        print("=" * 60)

        return filtered

    def show_removed_samples(self, input_path: str, n_samples: int = 10):
        """Preview samples that would be removed."""
        with open(input_path, 'r', encoding='utf-8') as f:
            extracted = json.load(f)

        print("\n" + "=" * 60)
        print("SAMPLE WORDS THAT WILL BE REMOVED")
        print("=" * 60)

        for pos_category, words in extracted.items():
            removed = [w for w in words if not self.is_valid_token(w, pos_category)]
            if removed:
                sample = removed[:n_samples]
                print(f"\n{pos_category} (showing {len(sample)} of {len(removed)}):")
                for word in sample:
                    print(f"  ✗ {word}")


# ---------------------------
# CLI
# ---------------------------
if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Filter extracted word lists")
    parser.add_argument("input", help="Path to extracted_words.json")
    parser.add_argument("-o", "--output", default="extracted_words_filtered.json",
                        help="Where to save filtered word list")
    parser.add_argument("--preview", action="store_true",
                        help="Preview removed words")

    args = parser.parse_args()

    wf = WordFilter()

    if args.preview:
        wf.show_removed_samples(args.input)
    else:
        wf.filter_extracted_words(args.input, args.output)
