#!/usr/bin/env python3
"""
wordlistfromtexts.py

Build grammar-keyed wordlists for RoBERTa categorisation.

Outputs keys like:
  adj_positive, adj_negative, adj_neutral
  noun_positive, noun_negative, noun_neutral
  verb_positive, verb_negative, verb_neutral
  adv_positive, adv_negative, adv_neutral

This version is tuned to reduce garbage output:
- runs on heavy_cleaned_texts by default
- computes sentiment once per sentence (cached)
- adds plausibility filters to reduce OCR junk / malformed words
- applies stricter filtering for adverbs
- skips probable proper names
- ranks by semantic meaningfulness + frequency

Input:  heavy_cleaned_texts/*.txt
Output: wordlistfromtexts.json
"""

from __future__ import annotations

import argparse
import json
import logging
import math
import re
from collections import defaultdict
from pathlib import Path
from typing import Dict, Iterable, List, Optional

import numpy as np
import spacy
from textblob import TextBlob


# -----------------------------
# Sentiment
# -----------------------------

def get_sentiment(text: str) -> str:
    try:
        polarity = TextBlob(text).sentiment.polarity
        if polarity > 0.1:
            return "positive"
        elif polarity < -0.1:
            return "negative"
        return "neutral"
    except Exception:
        return "neutral"


# -----------------------------
# Config
# -----------------------------

WHITESPACE_RE = re.compile(r"\s+")

POS_MAP = {
    "ADJ": "adj",
    "NOUN": "noun",
    "VERB": "verb",
    "ADV": "adv",
}
ALLOWED_POS = set(POS_MAP.keys())

VOWELS = set("aeiouy")

COMMON_ADVERBS = {
    "again", "almost", "already", "also", "always", "away", "back", "deeply",
    "ever", "far", "fast", "here", "never", "not", "now", "often", "only",
    "quite", "rather", "slowly", "softly", "soon", "still", "then", "there",
    "thus", "today", "tomorrow", "truly", "very", "well", "yet"
}

COMMON_GOOD_SHORT_WORDS = {
    "dim", "ash", "elm", "fir", "oak", "yew", "sky", "sea", "ash", "ice",
    "red", "grey", "gray", "blue", "gold", "dark", "cold", "warm", "fire",
    "wind", "stone", "glass", "light", "night", "sound", "water", "earth"
}

BAD_SUFFIXES = (
    "st",   # tremblest, deliveredst
)

LIKELY_OCR_PATTERNS = (
    "vv", "rnrn", "iii", "lll", "0o", "1l", "l1"
)


# -----------------------------
# Helpers
# -----------------------------

def normalize_text(text: str) -> str:
    return WHITESPACE_RE.sub(" ", text).strip()


def iter_text_files(input_dir: Path) -> Iterable[Path]:
    for p in sorted(input_dir.glob("*.txt")):
        if p.is_file():
            yield p


def logfreq(count: int) -> float:
    return math.log1p(count)


def ensure_sentence_segmentation(nlp) -> None:
    if not nlp.has_pipe("parser") and not nlp.has_pipe("senter"):
        if not nlp.has_pipe("sentencizer"):
            nlp.add_pipe("sentencizer")
            logging.info("Added sentencizer for sentence boundaries.")


# -----------------------------
# Plausibility filters
# -----------------------------

def vowel_count(word: str) -> int:
    return sum(1 for c in word if c in VOWELS)


def has_long_consonant_run(word: str, limit: int = 5) -> bool:
    return re.search(rf"[bcdfghjklmnpqrstvwxyz]{{{limit},}}", word) is not None


def looks_like_roman_numeral(word: str) -> bool:
    return re.fullmatch(r"[ivxlcdm]+", word) is not None


def has_tripled_letter(word: str) -> bool:
    return re.search(r"(.)\1\1", word) is not None


def contains_likely_ocr_pattern(word: str) -> bool:
    return any(p in word for p in LIKELY_OCR_PATTERNS)


def is_probable_proper_name(token) -> bool:
    """
    Skip likely names/titles when POS has drifted.
    Uses original token form before lowercasing.
    """
    txt = token.text.strip()
    if not txt:
        return False
    # Capitalized non-sentence-initial words are often names in heavy_cleaned_texts
    return txt[0].isupper() and token.i != token.sent.start


def is_plausible_word(word: str, min_len: int, max_len: int) -> bool:
    word = word.lower().strip()

    if not word.isalpha():
        return False

    if len(word) < min_len or len(word) > max_len:
        return False

    if word in COMMON_GOOD_SHORT_WORDS:
        return True

    if looks_like_roman_numeral(word):
        return False

    if vowel_count(word) == 0:
        return False

    # Very low-vowel long words are suspicious
    if len(word) >= 7 and vowel_count(word) <= 1:
        return False

    if has_long_consonant_run(word, 5):
        return False

    if has_tripled_letter(word):
        return False

    if contains_likely_ocr_pattern(word):
        return False

    # Reject obvious archaic inflections / OCR endings that are noisy in your output
    if len(word) >= 8 and word.endswith(BAD_SUFFIXES):
        return False

    # Weird glued words like "andsthen", "asoft", "amind"
    if re.search(r"(and|the|with|from|into|upon).{0,2}(the|then|and|with)$", word):
        return False

    return True


def is_plausible_adverb(word: str) -> bool:
    word = word.lower()

    if word in COMMON_ADVERBS:
        return True

    # Stronger adverb rule than raw POS tag alone
    if word.endswith("ly") and len(word) >= 5:
        stem = word[:-2]
        if len(stem) >= 3 and is_plausible_word(word, 3, 20):
            return True

    # A few non-ly adverbs can be added manually as needed.
    return False


# -----------------------------
# Extraction
# -----------------------------

def extract_counts(nlp, text: str, min_len: int, max_len: int):
    text = normalize_text(text)
    if not text:
        return {}

    doc = nlp(text)
    counts = defaultdict(int)

    # Compute sentiment once per sentence
    sent_sentiment: Dict[int, str] = {}
    for sent in doc.sents:
        sent_sentiment[sent.start] = get_sentiment(sent.text)

    for token in doc:
        if token.is_space or token.is_punct or token.like_num or token.is_stop:
            continue

        pos = token.pos_
        if pos not in ALLOWED_POS:
            continue

        if is_probable_proper_name(token):
            continue

        lemma = (token.lemma_ or token.text).lower().strip()

        if not is_plausible_word(lemma, min_len, max_len):
            continue

        # POS-specific tightening
        if pos == "ADV" and not is_plausible_adverb(lemma):
            continue

        # A little tightening for adjectives too
        if pos == "ADJ" and lemma.endswith("ly"):
            # usually these are adverbs mis-tagged as adjectives
            continue

        sentiment = sent_sentiment.get(token.sent.start, "neutral")
        counts[(lemma, pos, sentiment)] += 1

    return counts


def aggregate_directory(nlp, input_dir: Path, min_len: int, max_len: int, progress_every_files: int):
    all_counts = defaultdict(int)
    files = list(iter_text_files(input_dir))
    logging.info("Found %d files", len(files))

    for i, fp in enumerate(files, 1):
        if progress_every_files and (i == 1 or i % progress_every_files == 0):
            logging.info("Processing %d/%d: %s", i, len(files), fp.name)

        text = fp.read_text(encoding="utf-8", errors="ignore")
        local = extract_counts(nlp, text, min_len, max_len)
        for k, v in local.items():
            all_counts[k] += v

    return all_counts


# -----------------------------
# Semantic ranking
# -----------------------------

def build_centroid(nlp, lemmas: List[str]) -> Optional[np.ndarray]:
    vecs = []
    for doc in nlp.pipe(lemmas, batch_size=1024):
        v = doc.vector
        if v is not None and v.shape[0] > 0 and float(np.linalg.norm(v)) > 0.0:
            vecs.append(v)
    if not vecs:
        return None
    return np.mean(np.stack(vecs, axis=0), axis=0)


def cosine_sim(a: np.ndarray, b: np.ndarray) -> float:
    denom = float(np.linalg.norm(a) * np.linalg.norm(b))
    if denom == 0.0:
        return 0.0
    return float(np.dot(a, b) / denom)


def score_entries(nlp, counts):
    if not counts:
        return []

    unique_lemmas = sorted({lemma for (lemma, _, _) in counts})
    centroid = build_centroid(nlp, unique_lemmas)

    lemma_vec = {}
    lemma_norm = {}

    for doc in nlp.pipe(unique_lemmas, batch_size=1024):
        lemma_vec[doc.text] = doc.vector
        lemma_norm[doc.text] = float(np.linalg.norm(doc.vector)) if doc.vector is not None else 0.0

    max_lf = max(logfreq(c) for c in counts.values())
    if max_lf <= 0:
        max_lf = 1.0

    entries = []
    for (lemma, pos, sentiment), cnt in counts.items():
        lf = logfreq(cnt) / max_lf

        sem01 = 0.0
        if centroid is not None and lemma_norm.get(lemma, 0.0) > 0.0:
            sim = cosine_sim(lemma_vec[lemma], centroid)
            sem01 = (sim + 1.0) / 2.0

        # Slight POS priors to stabilize output a bit
        pos_prior = {
            "NOUN": 1.00,
            "VERB": 0.98,
            "ADJ": 0.96,
            "ADV": 0.90,
        }.get(pos, 0.95)

        score = (0.55 * sem01 + 0.35 * lf + 0.10 * pos_prior)
        entries.append({
            "lemma": lemma,
            "pos": pos,
            "sentiment": sentiment,
            "count": cnt,
            "score": float(score),
        })

    return entries


# -----------------------------
# Grammar grouping
# -----------------------------

def make_key(pos: str, sentiment: str) -> str:
    return f"{POS_MAP[pos]}_{sentiment}"


def build_wordlists(entries, top_n: int, min_count: int):
    grouped = defaultdict(list)

    for e in entries:
        if e["count"] < min_count:
            continue
        key = make_key(e["pos"], e["sentiment"])
        grouped[key].append(e)

    wordlists = {}
    for key, items in grouped.items():
        # Deduplicate by lemma, keeping best-scoring instance
        best_by_lemma = {}
        for item in items:
            prev = best_by_lemma.get(item["lemma"])
            if prev is None or item["score"] > prev["score"]:
                best_by_lemma[item["lemma"]] = item

        deduped = list(best_by_lemma.values())
        deduped.sort(key=lambda x: x["score"], reverse=True)
        top = deduped[:top_n] if top_n > 0 else deduped
        wordlists[key] = [x["lemma"] for x in top]

    return wordlists


# -----------------------------
# CLI
# -----------------------------

def main():
    ap = argparse.ArgumentParser(description="Build cleaner grammar-keyed wordlists for RoBERTa categorisation.")
    ap.add_argument("--input-dir", default="heavy_cleaned_texts")
    ap.add_argument("--output", default="wordlistfromtexts.json")
    ap.add_argument("--spacy-model", default="en_core_web_md")
    ap.add_argument("--min-len", type=int, default=3)
    ap.add_argument("--max-len", type=int, default=18)
    ap.add_argument("--top-n", type=int, default=500)
    ap.add_argument("--min-count", type=int, default=2,
                    help="Minimum total count for a lemma to survive into output (default: 2)")
    ap.add_argument("--progress-every-files", type=int, default=10)
    ap.add_argument("--log-level", default="INFO")

    args = ap.parse_args()
    logging.basicConfig(level=getattr(logging, args.log_level), format="%(levelname)s: %(message)s")

    input_dir = Path(args.input_dir)
    if not input_dir.exists():
        raise SystemExit(f"Input directory not found: {input_dir}")

    logging.info("Loading spaCy model: %s", args.spacy_model)
    nlp = spacy.load(args.spacy_model, disable=["ner"])
    ensure_sentence_segmentation(nlp)

    counts = aggregate_directory(nlp, input_dir, args.min_len, args.max_len, args.progress_every_files)

    logging.info("Scoring entries...")
    entries = score_entries(nlp, counts)

    wordlists = build_wordlists(entries, args.top_n, args.min_count)

    out = {
        "meta": {
            "input_dir": str(input_dir),
            "spacy_model": args.spacy_model,
            "top_n_per_key": args.top_n,
            "min_count": args.min_count,
            "min_len": args.min_len,
            "max_len": args.max_len,
        },
        "wordlists": wordlists,
    }

    Path(args.output).write_text(
        json.dumps(out, indent=2, ensure_ascii=False),
        encoding="utf-8"
    )
    logging.info("Wrote %s", args.output)


if __name__ == "__main__":
    main()