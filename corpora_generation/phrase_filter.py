"""
Phrase Filter - Clean, Repair & Rank Noun Phrases
--------------------------------------------------
Input: noun_phrases.json
Output: noun_phrases_filtered.json

Cleans:
- Repairs split words (e.g. "go ver nment" → "government")
- Removes punctuation
- Lowercases
- Removes non-English words
- Normalizes whitespace
- Keeps only 3–4 word phrases
- Computes semantic sense score using SpaCy vectors
- Selects TOP 100 most meaningful phrases
"""

import json
import re
from typing import Optional
import spacy


class PhraseFilter:

    def __init__(self, model_name="en_core_web_md"):
        print(f"Loading SpaCy model: {model_name}...")
        self.nlp = spacy.load(model_name)

        # regex for English-like tokens
        self.word_re = re.compile(r"^[a-zA-Z]+$")

    # ------------------------------------------------------
    # BASIC ENGLISH WORD DETECTOR
    # ------------------------------------------------------
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

    # ------------------------------------------------------
    # FIX SPLIT OCR WORD (remove internal spaces/fragments)
    # ------------------------------------------------------
    def fix_split_word(self, word: str) -> str:
        """
        Repairs OCR-style split words like:
        - "go ver nment" → "government"
        - "edu cat ion" → "education"
        - "inter nat ional" → "international"
        """
        return word.replace(" ", "").strip()

    # ------------------------------------------------------
    # CLEAN + REPAIR A PHRASE
    # ------------------------------------------------------
    def clean_phrase(self, phrase: str) -> Optional[str]:
        """Clean punctuation, repair split words, filter to English."""
        # lowercase
        phrase = phrase.lower()

        # remove punctuation
        phrase = re.sub(r"[^a-zA-Z\s]", " ", phrase)

        # compress whitespace
        phrase = re.sub(r"\s+", " ", phrase).strip()
        if not phrase:
            return None

        raw_words = phrase.split()
        repaired_words = []

        i = 0
        while i < len(raw_words):
            w = raw_words[i]

            # repair split OCR word fragments
            fixed = self.fix_split_word(w)

            # If no vowels, attempt merge with next token (OCR split)
            if (not self.is_english_word(fixed)) and i < len(raw_words) - 1:
                combined = fixed + raw_words[i + 1]
                combined = self.fix_split_word(combined)
                if self.is_english_word(combined):
                    fixed = combined
                    i += 1  # skip next word

            repaired_words.append(fixed)
            i += 1

        # filter non-English words
        final_words = [w for w in repaired_words if self.is_english_word(w)]

        # keep only 3–4 word phrases
        if len(final_words) < 3 or len(final_words) > 4:
            return None

        return " ".join(final_words)

    # ------------------------------------------------------
    # SEMANTIC "SENSE SCORE"
    # ------------------------------------------------------
    def phrase_sense_score(self, phrase: str) -> float:
        """
        Semantic score = vector magnitude.
        Higher = more meaningful, coherent phrase.
        """
        doc = self.nlp(phrase)
        if doc.vector_norm == 0:
            return 0.0
        return float(doc.vector_norm)

    # ------------------------------------------------------
    # MAIN FILTER FUNCTION
    # ------------------------------------------------------
    def filter_file(self, input_path: str, output_path="noun_phrases_filtered.json"):
        print(f"Loading noun phrases from: {input_path}")

        with open(input_path, "r", encoding="utf-8") as f:
            phrases = json.load(f)

        cleaned = []
        for p in phrases:
            cp = self.clean_phrase(p)
            if cp:
                cleaned.append(cp)

        cleaned = sorted(set(cleaned))
        print(f"Found {len(cleaned)} valid cleaned phrases.")

        if not cleaned:
            print("No valid phrases found.")
            return []

        print("Computing semantic sense scores…")
        scored = [(p, self.phrase_sense_score(p)) for p in cleaned]

        # sort by best semantic coherence
        scored.sort(key=lambda x: x[1], reverse=True)

        # choose top 100
        top500 = [p for p, score in scored[:500]]

        with open(output_path, "w", encoding="utf-8") as f:
            json.dump(top500, f, indent=2, ensure_ascii=False)

        print(f"Saved top 500 meaningful phrases → {output_path}")
        return top500


# ------------------------------------------------------
# CLI
# ------------------------------------------------------
if __name__ == "__main__":
    import argparse

    parser = argparse.ArgumentParser(description="Clean, repair & rank noun phrases")
    parser.add_argument("input", help="noun_phrases.json")
    parser.add_argument("-o", "--output", default="noun_phrases_filtered.json")
    parser.add_argument("--model", default="en_core_web_md",
                        help="SpaCy model (md/lg recommended)")

    args = parser.parse_args()

    pf = PhraseFilter(model_name=args.model)
    pf.filter_file(args.input, args.output)
