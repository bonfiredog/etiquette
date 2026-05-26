"""
Data Filter - Clean, Repair & Rank Words and Noun Phrases
----------------------------------------------------------
Input: extracted_data.json
Output: extracted_data_filtered.json

Features:
- Cleans and filters word categories with validation rules
- Handles nested structure (POS -> sentiment -> words)
- Repairs split words in noun phrases (e.g. "go ver nment" → "government")
- Computes semantic sense scores using SpaCy vectors
- Ranks all categories by meaningfulness
- Configurable top-N cutoff per category (separate for words vs phrases)
"""

import json
import re
from typing import Optional, List, Dict, Any
import spacy


class DataFilter:

    def __init__(self, model_name="en_core_web_lg"):
        print(f"Loading SpaCy model: {model_name}...")
        self.nlp = spacy.load(model_name)

        # Regex for English-like tokens
        self.word_re = re.compile(r"^[a-zA-Z]+$")

        # Common English words (always keep)
        self.whitelist = {
            'oh', 'ah', 'uh', 'no', 'yes', 'hey', 'wow',
            'a', 'i', 'we', 'he', 'she', 'it',
            'be', 'do', 'go', 'in', 'on', 'at', 'to',
            'of', 'or', 'if', 'so', 'and', 'but', 'for',
            'nor', 'yet', 'the'
        }

        # Map POS tags to category names for filtering rules
        self.pos_to_category = {
            'NOUN': 'nouns',
            'PROPN': 'proper_nouns',
            'VERB': 'verbs',
            'ADJ': 'adjectives',
            'ADV': 'adverbs',
            'ADP': 'adpositions',
            'INTJ': 'interjections'
        }

        # Minimum length requirements per category
        self.min_lengths = {
            'nouns': 3,
            'proper_nouns': 3,
            'verbs': 2,
            'adpositions': 2,
            'adjectives': 3,
            'adverbs': 3,
            'interjections': 2,
            'noun_phrases': 5,
            'random_noun_phrases': 5,
        }

    # ======================================================
    # WORD FILTERING
    # ======================================================

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
        if word and word[0].islower() and any(c.isupper() for c in word[1:]):
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
        if word and (word[0] in '.,;:!?' or word[-1] in '.,;:!?'):
            return False

        # No vowels (likely noise)
        if len(word) > 4 and pos_category not in ('interjections', 'random_noun_phrases'):
            if not any(c in 'aeiouAEIOU' for c in word):
                return False

        return True

    def filter_word_list(self, words: List[str], pos_category: str) -> List[str]:
        """Apply filtering rules to one category of words."""
        filtered = [w for w in words if self.is_valid_token(w, pos_category)]
        return sorted(list(set(filtered)))

    # ======================================================
    # PHRASE CLEANING
    # ======================================================

    def is_english_word(self, word: str) -> bool:
        """Simple English detector using vowels + alphabetic pattern."""
        word = word.lower()
        if len(word) < 2:
            return False
        if not self.word_re.match(word):
            return False
        if any(c in "aeiou" for c in word):
            return True
        return False

    def fix_split_word(self, word: str) -> str:
        """
        Repairs OCR-style split words like:
        - "go ver nment" → "government"
        - "edu cat ion" → "education"
        """
        return word.replace(" ", "").strip()

    def clean_phrase(self, phrase: str) -> Optional[str]:
        """Clean punctuation, repair split words, filter to English."""
        # Lowercase
        phrase = phrase.lower()

        # Remove punctuation
        phrase = re.sub(r"[^a-zA-Z\s]", " ", phrase)

        # Compress whitespace
        phrase = re.sub(r"\s+", " ", phrase).strip()
        if not phrase:
            return None

        raw_words = phrase.split()
        repaired_words = []

        i = 0
        while i < len(raw_words):
            w = raw_words[i]

            # Repair split OCR word fragments
            fixed = self.fix_split_word(w)

            # If no vowels, attempt merge with next token (OCR split)
            if (not self.is_english_word(fixed)) and i < len(raw_words) - 1:
                combined = fixed + raw_words[i + 1]
                combined = self.fix_split_word(combined)
                if self.is_english_word(combined):
                    fixed = combined
                    i += 1  # Skip next word

            repaired_words.append(fixed)
            i += 1

        # Filter non-English words
        final_words = [w for w in repaired_words if self.is_english_word(w)]

        # Keep only 3–4 word phrases
        if len(final_words) < 3 or len(final_words) > 4:
            return None

        return " ".join(final_words)

    def filter_phrase_list(self, phrases: List[str]) -> List[str]:
        """Clean and filter a list of noun phrases."""
        cleaned = []
        for p in phrases:
            cp = self.clean_phrase(p)
            if cp:
                cleaned.append(cp)
        return sorted(set(cleaned))

    # ======================================================
    # SEMANTIC SENSE SCORING
    # ======================================================

    def semantic_sense_score(self, text: str) -> float:
        """
        Semantic score = vector magnitude.
        Higher = more meaningful, coherent text.
        """
        doc = self.nlp(text)
        if doc.vector_norm == 0:
            return 0.0
        return float(doc.vector_norm)

    def rank_by_meaningfulness(
        self,
        items: List[str],
        top_n: int
    ) -> List[str]:
        """Rank items by semantic sense score and return top N."""
        if not items:
            return []

        print(f"    Computing semantic scores for {len(items)} items...")
        scored = [(item, self.semantic_sense_score(item)) for item in items]

        # Sort by best semantic coherence
        scored.sort(key=lambda x: x[1], reverse=True)

        # Return top N
        return [item for item, score in scored[:top_n]]

    # ======================================================
    # MAIN FILTER FUNCTION
    # ======================================================

    def filter_data(
        self,
        input_path: str,
        output_path: str = "extracted_data_filtered.json",
        top_n_words: int = 500,
        top_n_phrases: int = 500
    ) -> Dict[str, Any]:
        """
        Main filtering function that processes all categories.
        
        Args:
            input_path: Path to extracted_data.json
            output_path: Where to save filtered results
            top_n_words: Number of top words to keep per sentiment category
            top_n_phrases: Number of top phrases to keep per category
        """
        print(f"Loading data from: {input_path}")
        
        with open(input_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        print("=" * 60)
        print("FILTERING AND RANKING EXTRACTED DATA")
        print(f"Word categories: top {top_n_words} per sentiment")
        print(f"Phrase categories: top {top_n_phrases} per category")
        print("=" * 60)

        filtered = {}

        # Process categorized_words (nested structure: POS -> sentiment -> words)
        if 'categorized_words' in data:
            print("\n📝 Processing categorized_words...")
            filtered['categorized_words'] = {}
            
            for pos_tag, sentiment_dict in data['categorized_words'].items():
                print(f"\n  {pos_tag}:")
                filtered['categorized_words'][pos_tag] = {}
                
                # Get the category name for filtering rules
                pos_category = self.pos_to_category.get(pos_tag, 'nouns')
                
                for sentiment, words in sentiment_dict.items():
                    original_count = len(words)
                    print(f"    {sentiment}: {original_count:,} words", end="")
                    
                    # Filter words
                    cleaned = self.filter_word_list(words, pos_category)
                    cleaned_count = len(cleaned)
                    removed = original_count - cleaned_count
                    print(f" → {cleaned_count:,} (-{removed:,})", end="")
                    
                    # Rank by meaningfulness
                    if cleaned:
                        ranked = self.rank_by_meaningfulness(cleaned, top_n_words)
                        filtered['categorized_words'][pos_tag][sentiment] = ranked
                        print(f" → {len(ranked):,} (top {top_n_words})")
                    else:
                        filtered['categorized_words'][pos_tag][sentiment] = []
                        print(" → 0")

        # Process noun_phrases (simple list)
        if 'noun_phrases' in data:
            print("\n📝 Processing noun_phrases...")
            original_count = len(data['noun_phrases'])
            print(f"  Original count: {original_count:,}")
            
            cleaned = self.filter_phrase_list(data['noun_phrases'])
            cleaned_count = len(cleaned)
            removed = original_count - cleaned_count
            print(f"  After cleaning: {cleaned_count:,} (-{removed:,})")
            
            if cleaned:
                ranked = self.rank_by_meaningfulness(cleaned, top_n_phrases)
                filtered['noun_phrases'] = ranked
                print(f"  After ranking: {len(ranked):,} (top {top_n_phrases})")
            else:
                filtered['noun_phrases'] = []
                print(f"  After ranking: 0")

        # Process any other phrase categories
        for key, items in data.items():
            if key not in ('categorized_words', 'noun_phrases'):
                # Assume it's a phrase category if not already processed
                if isinstance(items, list):
                    print(f"\n📝 Processing {key}...")
                    original_count = len(items)
                    print(f"  Original count: {original_count:,}")
                    
                    cleaned = self.filter_phrase_list(items)
                    cleaned_count = len(cleaned)
                    removed = original_count - cleaned_count
                    print(f"  After cleaning: {cleaned_count:,} (-{removed:,})")
                    
                    if cleaned:
                        ranked = self.rank_by_meaningfulness(cleaned, top_n_phrases)
                        filtered[key] = ranked
                        print(f"  After ranking: {len(ranked):,} (top {top_n_phrases})")
                    else:
                        filtered[key] = []
                        print(f"  After ranking: 0")

        # Save results
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(filtered, f, indent=2, ensure_ascii=False)

        print("\n" + "=" * 60)
        print(f"Saved filtered data → {output_path}")
        print("=" * 60)

        return filtered

    def preview_removed(
        self,
        input_path: str,
        n_samples: int = 10
    ):
        """Preview samples that would be removed from each category."""
        with open(input_path, 'r', encoding='utf-8') as f:
            data = json.load(f)

        print("\n" + "=" * 60)
        print("SAMPLE ITEMS THAT WILL BE REMOVED")
        print("=" * 60)

        # Preview categorized_words
        if 'categorized_words' in data:
            for pos_tag, sentiment_dict in data['categorized_words'].items():
                pos_category = self.pos_to_category.get(pos_tag, 'nouns')
                
                for sentiment, words in sentiment_dict.items():
                    removed = [w for w in words if not self.is_valid_token(w, pos_category)]
                    
                    if removed:
                        sample = removed[:n_samples]
                        print(f"\n{pos_tag} - {sentiment} (showing {len(sample)} of {len(removed)}):")
                        for word in sample:
                            print(f"  ✗ {word}")

        # Preview noun_phrases
        if 'noun_phrases' in data:
            removed = [p for p in data['noun_phrases'] if not self.clean_phrase(p)]
            if removed:
                sample = removed[:n_samples]
                print(f"\nnoun_phrases (showing {len(sample)} of {len(removed)}):")
                for phrase in sample:
                    print(f"  ✗ {phrase}")


# ======================================================
# CLI
# ======================================================
if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(
        description="Filter and rank extracted words and phrases"
    )
    parser.add_argument(
        "input",
        help="Path to extracted_data.json"
    )
    parser.add_argument(
        "-o", "--output",
        default="extracted_data_filtered.json",
        help="Where to save filtered data"
    )
    parser.add_argument(
        "--top-words",
        type=int,
        default=500,
        help="Number of top words to keep per sentiment category (default: 500)"
    )
    parser.add_argument(
        "--top-phrases",
        type=int,
        default=500,
        help="Number of top phrases to keep per category (default: 500)"
    )
    parser.add_argument(
        "--model",
        default="en_core_web_lg",
        help="SpaCy model (md/lg recommended)"
    )
    parser.add_argument(
        "--preview",
        action="store_true",
        help="Preview items that will be removed"
    )

    args = parser.parse_args()

    df = DataFilter(model_name=args.model)

    if args.preview:
        df.preview_removed(args.input)
    else:
        df.filter_data(args.input, args.output, args.top_words, args.top_phrases)
