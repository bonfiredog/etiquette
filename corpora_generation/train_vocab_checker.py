#!/usr/bin/env python3
"""
Audit sentences.json for railway vocabulary.

Supports either:
A) Default built-in term list
B) A newline-separated terms file (one term per line)
C) A categorized JSON terms file:
   - JSON object: { "category": ["term1", "term2", ...], ... }
   - or list containing one such object: [ { ... } ]

Outputs:
- Total railway-term hits (occurrences)
- Sentences containing >=1 term: count + percentage
- Unique matched terms list
- Category breakdown (hits and sentence coverage per category)
"""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter, defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Dict, Iterable, List, Tuple, Optional


DEFAULT_RAIL_TERMS = [
    "train", "trains", "rail", "rails", "railway", "railways", "railroad", "railroads",
    "station", "stations", "platform", "platforms", "track", "tracks", "signal", "signals",
    "junction", "junctions", "depot", "depots", "yard", "yards", "crossing", "crossings",
    "locomotive", "locomotives", "engine", "engines", "carriage", "carriages", "coach", "coaches",
    "wagon", "wagons", "timetable", "timetables", "ticket", "tickets", "conductor", "conductors",
    "guard", "guards", "driver", "drivers", "metro", "subway", "underground", "tube", "tram", "trams",
    "light rail", "monorail", "level crossing", "level crossings",
]


def load_sentences(path: Path) -> List[str]:
    data = json.loads(path.read_text(encoding="utf-8"))
    texts: List[str] = []

    if isinstance(data, list):
        for item in data:
            if isinstance(item, dict) and "text" in item:
                texts.append(str(item["text"]))
            elif isinstance(item, str):
                texts.append(item)
    else:
        raise ValueError("sentences.json must be a list of dicts with 'text' or a list of strings")

    return texts


def normalize_match(m: str) -> str:
    return re.sub(r"\s+", " ", m.strip().lower())


def build_regex_from_terms(terms: Iterable[str]) -> re.Pattern:
    pieces = []
    for t in terms:
        t = t.strip()
        if not t:
            continue
        if " " in t:
            words = [re.escape(w) for w in t.split()]
            pieces.append(r"\b" + r"\s+".join(words) + r"\b")
        else:
            pieces.append(r"\b" + re.escape(t) + r"\b")
    combined = "|".join(pieces) if pieces else r"(?!x)x"
    return re.compile(combined, flags=re.IGNORECASE)


def load_terms_lines(path: Path) -> Dict[str, List[str]]:
    # single pseudo-category
    terms = [
        ln.strip()
        for ln in path.read_text(encoding="utf-8").splitlines()
        if ln.strip() and not ln.strip().startswith("#")
    ]
    return {"terms": terms}


def load_terms_json(path: Path) -> Dict[str, List[str]]:
    obj = json.loads(path.read_text(encoding="utf-8"))

    # Support either {cat: [...]} or [ {cat: [...]} ]
    if isinstance(obj, list):
        if len(obj) != 1 or not isinstance(obj[0], dict):
            raise ValueError("JSON terms file as a list must contain exactly one object.")
        obj = obj[0]

    if not isinstance(obj, dict):
        raise ValueError("JSON terms file must be an object {category: [terms...]} (or [ {..} ]).")

    out: Dict[str, List[str]] = {}
    for cat, terms in obj.items():
        if not isinstance(cat, str):
            continue
        if not isinstance(terms, list) or not all(isinstance(t, str) for t in terms):
            raise ValueError(f"Category '{cat}' must map to a list of strings.")
        out[cat] = [t.strip() for t in terms if t.strip()]

    if not out:
        raise ValueError("No categories/terms found in JSON terms file.")

    return out


def audit(
    texts: List[str],
    category_terms: Dict[str, List[str]],
) -> Dict:
    # Precompile regex per category
    cat_regex: Dict[str, re.Pattern] = {
        cat: build_regex_from_terms(terms)
        for cat, terms in category_terms.items()
        if terms
    }

    total_sentences = len(texts)

    total_hits_counter: Counter[str] = Counter()
    sentences_with_any_hit = 0

    # category breakdown
    cat_hits: Dict[str, int] = defaultdict(int)
    cat_sentence_hits: Dict[str, int] = defaultdict(int)

    # per-term counts (normalized)
    per_term_counts: Counter[str] = Counter()

    for s in texts:
        sentence_had_any = False
        sentence_cats_hit = set()

        for cat, rx in cat_regex.items():
            matches = [normalize_match(m.group(0)) for m in rx.finditer(s)]
            if matches:
                sentence_had_any = True
                sentence_cats_hit.add(cat)

                # counts
                cat_hits[cat] += len(matches)
                per_term_counts.update(matches)
                total_hits_counter.update(matches)

        if sentence_had_any:
            sentences_with_any_hit += 1
        for cat in sentence_cats_hit:
            cat_sentence_hits[cat] += 1

    total_hits = sum(total_hits_counter.values())
    pct_any = (sentences_with_any_hit / total_sentences * 100.0) if total_sentences else 0.0

    # Build category stats
    category_stats = []
    for cat in sorted(cat_regex.keys()):
        n_sent = cat_sentence_hits.get(cat, 0)
        pct = (n_sent / total_sentences * 100.0) if total_sentences else 0.0
        category_stats.append({
            "category": cat,
            "hits": int(cat_hits.get(cat, 0)),
            "sentences_with_category_hit": int(n_sent),
            "percentage_sentences_with_category_hit": pct,
        })

    return {
        "total_sentences": total_sentences,
        "sentences_with_rail_vocab": sentences_with_any_hit,
        "percentage_sentences_with_rail_vocab": pct_any,
        "total_rail_vocab_hits": total_hits,
        "unique_matched_words": sorted(total_hits_counter.keys()),
        "matched_word_counts": dict(per_term_counts.most_common()),
        "category_stats": category_stats,
    }


def main() -> None:
    ap = argparse.ArgumentParser(description="Audit sentences.json for railway vocabulary.")
    ap.add_argument("--input", default="sentences.json", help="Path to sentences.json")

    group = ap.add_mutually_exclusive_group()
    group.add_argument("--terms-file", default=None, help="Newline-separated terms file (one per line)")
    group.add_argument("--terms-json", default=None, help="Categorized JSON terms file")

    ap.add_argument("--show-counts", action="store_true", help="Print per-term counts")
    ap.add_argument("--show-categories", action="store_true", help="Print per-category breakdown")
    args = ap.parse_args()

    input_path = Path(args.input)
    if not input_path.exists():
        raise SystemExit(f"Not found: {input_path}")

    texts = load_sentences(input_path)

    if args.terms_json:
        category_terms = load_terms_json(Path(args.terms_json))
    elif args.terms_file:
        category_terms = load_terms_lines(Path(args.terms_file))
    else:
        category_terms = {"default": DEFAULT_RAIL_TERMS}

    result = audit(texts, category_terms)

    # Required outputs
    print(f"How many railway-related word hits: {result['total_rail_vocab_hits']}")
    print(
        f"Across what percentage of total sentences: "
        f"{result['sentences_with_rail_vocab']}/{result['total_sentences']} "
        f"({result['percentage_sentences_with_rail_vocab']:.2f}%)"
    )
    print("List of matched words/phrases:")
    for w in result["unique_matched_words"]:
        print(f"- {w}")

    if args.show_categories:
        print("\nCategory breakdown:")
        for row in result["category_stats"]:
            print(
                f"- {row['category']}: {row['hits']} hits | "
                f"{row['sentences_with_category_hit']}/{result['total_sentences']} "
                f"({row['percentage_sentences_with_category_hit']:.2f}%)"
            )

    if args.show_counts:
        print("\nPer-term counts (descending):")
        for w, c in result["matched_word_counts"].items():
            print(f"{c:6d}  {w}")


if __name__ == "__main__":
    main()