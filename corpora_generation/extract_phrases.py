#!/usr/bin/env python3
"""
extract_phrases.py

Free n-gram scene-fragment extractor.

Goal:
- sample ANY contiguous fragment up to 5 words
- but only keep fragments that feel like little scene pieces

Allowed broadly:
- verb fragments: "eating voraciously", "a cat lies down"
- noun fragments with article: "a horrible ape"
- prepositional scene fragments: "on a horse", "in the doorway"

Rejected broadly:
- discourse glue: "at the same time", "and with him"
- abstract/genitive fragments: "of human will", "censure of fathers"
- broken OCR/hyphenation junk
"""

from __future__ import annotations

import argparse
import json
import logging
import math
import random
import re
import time
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable, List, Optional, Set, Tuple

import numpy as np
import spacy

try:
    from tqdm import tqdm
    HAS_TQDM = True
except Exception:
    HAS_TQDM = False


# -----------------------------
# Data model
# -----------------------------
@dataclass
class RankedFragment:
    text: str
    score: float
    cohesion: float
    semantic: float
    source_file: str
    sent_start_char: int
    sent_end_char: int
    frag_start_char: int
    frag_end_char: int


# -----------------------------
# Regex / constants
# -----------------------------
WHITESPACE_RE = re.compile(r"\s+")
OUTER_QUOTES_RE = re.compile(r'^[\"“”‘’]+|[\"“”‘’]+$')
BAD_OCR_RE = re.compile(r"[A-Z]{2,}[a-z]{0,1}[A-Z]{2,}|[^\w\s,.;:!?'\-]")

ARTICLES = {"a", "an", "the"}
BAD_START_WORDS = {
    "and", "but", "or", "that", "which", "who", "whom", "whose",
    "if", "because", "though", "although", "while", "as"
}
BAD_SINGLE_PREPS = {"of", "for", "to", "with", "by", "from"}
SCENE_PREPS = {"on", "in", "under", "over", "behind", "before", "beside", "near", "against", "inside", "outside", "upon"}

ABSTRACT_NOUN_HINTS = {
    "time", "cause", "conduct", "execution", "will", "censure",
    "introduction", "refuge", "dwelling", "article"
}


# -----------------------------
# Basic utils
# -----------------------------
def normalize_ws(s: str) -> str:
    return WHITESPACE_RE.sub(" ", s).strip()


def strip_outer_quotes(s: str) -> str:
    return OUTER_QUOTES_RE.sub("", s.strip()).strip()


def clean_text(s: str) -> str:
    return strip_outer_quotes(normalize_ws(s)).lower()


def count_words(s: str) -> int:
    return len([w for w in s.split() if w])


def alpha_ratio(text: str) -> float:
    total = max(len(text), 1)
    letters = sum(ch.isalpha() for ch in text)
    return letters / total


def iter_text_files(input_dir: Path) -> Iterable[Path]:
    for p in sorted(input_dir.glob("*.txt")):
        if p.is_file():
            yield p


def ensure_dependency_parser(nlp) -> None:
    if not nlp.has_pipe("parser"):
        raise SystemExit("spaCy model needs dependency parser (e.g. en_core_web_md).")


# -----------------------------
# Vector helpers
# -----------------------------
def compute_centroid(vecs: List[np.ndarray]) -> Optional[np.ndarray]:
    if not vecs:
        return None
    return np.mean(np.stack(vecs, axis=0), axis=0)


def cosine_sim(a: np.ndarray, b: np.ndarray) -> float:
    denom = float(np.linalg.norm(a) * np.linalg.norm(b))
    if denom == 0.0:
        return 0.0
    return float(np.dot(a, b) / denom)


def length_score_by_words(n_words: int) -> float:
    mu = 3.5
    sigma = 1.0
    z = (n_words - mu) / sigma
    return float(math.exp(-0.5 * z * z))


# -----------------------------
# Token helpers
# -----------------------------
def wordish_tokens(span):
    return [t for t in span if not t.is_space]

def nonpunct_tokens(span):
    return [t for t in wordish_tokens(span) if not t.is_punct]

def has_verb(span) -> bool:
    return any(t.pos_ in {"VERB", "AUX"} for t in span)

def has_noun(span) -> bool:
    return any(t.pos_ in {"NOUN", "PROPN"} for t in span)

def has_article(span) -> bool:
    return any(t.pos_ == "DET" and t.lower_ in ARTICLES for t in span)

def noun_tokens(span):
    return [t for t in span if t.pos_ in {"NOUN", "PROPN"}]

def prep_tokens(span):
    return [t for t in span if t.pos_ == "ADP"]

def adj_tokens(span):
    return [t for t in span if t.pos_ == "ADJ"]

def adv_tokens(span):
    return [t for t in span if t.pos_ == "ADV"]


# -----------------------------
# Quality / gating
# -----------------------------
def looks_broken_or_ocrish(text: str) -> bool:
    if BAD_OCR_RE.search(text):
        return True
    if text.endswith("-"):
        return True
    if "  " in text:
        return True
    return False


def starts_bad(span) -> bool:
    toks = nonpunct_tokens(span)
    if not toks:
        return True
    return toks[0].lower_ in BAD_START_WORDS


def ends_bad(span) -> bool:
    toks = wordish_tokens(span)
    if not toks:
        return True
    last = toks[-1]
    return last.text in {",", ";", ":"}


def is_discoursey_time_phrase(span) -> bool:
    txt = clean_text(span.text)
    bad_exact = {
        "at the same time",
        "in the same way",
        "on the other hand",
        "at the first",
        "at the last",
        "for the first time",
    }
    if txt in bad_exact:
        return True

    # "on the fourth", "at the second", etc.
    toks = nonpunct_tokens(span)
    if len(toks) >= 3:
        if toks[0].lower_ in {"on", "at", "in"} and toks[1].lower_ == "the":
            if toks[2].like_num or toks[2].lower_ in {
                "first", "second", "third", "fourth", "fifth",
                "sixth", "seventh", "eighth", "ninth", "tenth"
            }:
                return True
    return False


def is_too_abstract_np(span) -> bool:
    if has_verb(span):
        return False

    nouns = noun_tokens(span)
    if not nouns:
        return False

    # reject "of X" and similar abstract/genitive fragments
    toks = nonpunct_tokens(span)
    if toks and toks[0].lower_ in BAD_SINGLE_PREPS:
        return True

    # reject abstract noun phrases unless they have strong scene anchors
    noun_lemmas = {t.lemma_.lower() for t in nouns}
    if noun_lemmas & ABSTRACT_NOUN_HINTS:
        if not prep_tokens(span) and not adj_tokens(span):
            return True

    return False


def passes_scene_constraint(span) -> bool:
    """
    Hard acceptance logic.
    """
    toks = nonpunct_tokens(span)
    if not toks:
        return False

    if starts_bad(span):
        return False
    if ends_bad(span):
        return False
    if is_discoursey_time_phrase(span):
        return False
    if is_too_abstract_np(span):
        return False

    verb = has_verb(span)
    noun = has_noun(span)
    article = has_article(span)
    prep = prep_tokens(span)

    # Verb fragments are okay, but should not be pure glue
    if verb:
        return True

    # Noun fragments require article
    if noun and article:
        # scene noun phrase or scene prep phrase
        if prep:
            if prep[0].lower_ in SCENE_PREPS:
                return True
        if adj_tokens(span):
            return True
        # also allow compact article+noun if noun looks concrete-ish
        noun_lemmas = {t.lemma_.lower() for t in noun_tokens(span)}
        if not (noun_lemmas & ABSTRACT_NOUN_HINTS):
            return True

    return False


# -----------------------------
# Cohesion scoring
# -----------------------------
SCENE_DEPS = {"nsubj", "nsubjpass", "obj", "dobj", "pobj", "prep", "amod", "advmod"}


def cohesion_score(span) -> float:
    toks = nonpunct_tokens(span)
    if not toks:
        return 0.0

    span_set = {t.i for t in toks}
    internal = 0
    boundary = 0
    dep_bonus_hits = 0
    subj_verb = False
    verb_obj = False
    prep_pobj = False

    prep_idxs: Set[int] = set()
    pobj_heads: Set[int] = set()

    for t in toks:
        if t.head is not None and t.head.i in span_set and t.head.i != t.i:
            internal += 1
        else:
            boundary += 1

        if t.dep_ in SCENE_DEPS:
            dep_bonus_hits += 1

        if t.dep_ in {"nsubj", "nsubjpass"} and t.head.pos_ in {"VERB", "AUX"} and t.head.i in span_set:
            subj_verb = True

        if t.dep_ in {"obj", "dobj"} and t.head.pos_ in {"VERB", "AUX"} and t.head.i in span_set:
            verb_obj = True

        if t.dep_ == "prep" and t.pos_ == "ADP":
            prep_idxs.add(t.i)

        if t.dep_ == "pobj" and t.head is not None and t.head.i in span_set:
            pobj_heads.add(t.head.i)

    if prep_idxs & pobj_heads:
        prep_pobj = True

    base = internal / (internal + boundary + 1e-6)
    dep_bonus = min(0.20, dep_bonus_hits * 0.04)

    rel_bonus = 0.0
    if subj_verb:
        rel_bonus += 0.18
    if verb_obj:
        rel_bonus += 0.10
    if prep_pobj:
        rel_bonus += 0.12

    score = base + dep_bonus + rel_bonus
    return float(max(0.0, min(1.0, score)))


# -----------------------------
# Free n-grams
# -----------------------------
def extract_free_ngrams(sent, min_words: int, max_words: int) -> List[Tuple[int, int]]:
    idxs = [t.i for t in sent if not t.is_space and not t.is_punct]
    spans: List[Tuple[int, int]] = []

    for a in range(len(idxs)):
        for n in range(min_words, max_words + 1):
            b = a + n
            if b > len(idxs):
                continue
            s_i = idxs[a]
            e_i = idxs[b - 1] + 1
            spans.append((s_i, e_i))

    return spans


# -----------------------------
# Spans -> fragments
# -----------------------------
def spans_to_fragments(sent, spans, min_words, max_words, min_alpha_ratio):
    out = []
    seen = set()
    doc = sent.doc

    for s_i, e_i in spans:
        if e_i <= s_i:
            continue

        span = doc[s_i:e_i]
        frag = clean_text(span.text)

        if not frag:
            continue
        if frag in seen:
            continue

        nw = count_words(frag)
        if nw < min_words or nw > max_words:
            continue

        if alpha_ratio(span.text) < min_alpha_ratio:
            continue
        if looks_broken_or_ocrish(span.text):
            continue
        if not passes_scene_constraint(span):
            continue

        coh = cohesion_score(span)
        if coh < 0.18:
            continue

        seen.add(frag)
        out.append((frag, span.start_char, span.end_char, coh))

    return out


# -----------------------------
# Reservoir sampling
# -----------------------------
def reservoir_sample_stream(stream, k, rng, progress_every=20000):
    sample = []
    n = 0
    for item in stream:
        n += 1
        if len(sample) < k:
            sample.append(item)
        else:
            j = rng.randint(1, n)
            if j <= k:
                sample[j - 1] = item

        if progress_every and n % progress_every == 0:
            print(f"  Seen: {n:,} | Sampled: {len(sample):,}", flush=True)

    return sample, n


# -----------------------------
# Ranking
# -----------------------------
def score_fragment(doc, cohesion, centroid_vec):
    text = doc.text
    n_words = count_words(text)

    sem01 = 0.0
    v = doc.vector
    if centroid_vec is not None and v is not None and v.shape[0] > 0 and float(np.linalg.norm(v)) > 0.0:
        sim = cosine_sim(v, centroid_vec)
        sem01 = (sim + 1.0) / 2.0

    a = alpha_ratio(text)
    l = length_score_by_words(n_words)

    score = (
        0.35 * sem01 +
        0.40 * cohesion +
        0.15 * a +
        0.10 * l
    )
    return float(max(0.0, min(1.0, score))), sem01


# -----------------------------
# Stream builder
# -----------------------------
def build_stream_of_fragments(nlp, files, min_words, max_words, min_alpha_ratio):
    total_files = len(files)

    for i, fp in enumerate(files, 1):
        print(f"Processing file {i}/{total_files}: {fp.name}", flush=True)
        text = fp.read_text(encoding="utf-8", errors="replace")
        if not text.strip():
            continue

        doc = nlp(text)
        for sent in doc.sents:
            spans = extract_free_ngrams(sent, min_words, max_words)
            frags = spans_to_fragments(sent, spans, min_words, max_words, min_alpha_ratio)

            for frag_text, frag_start_char, frag_end_char, coh in frags:
                yield RankedFragment(
                    text=frag_text,
                    score=0.0,
                    cohesion=coh,
                    semantic=0.0,
                    source_file=fp.name,
                    sent_start_char=sent.start_char,
                    sent_end_char=sent.end_char,
                    frag_start_char=frag_start_char,
                    frag_end_char=frag_end_char,
                )


# -----------------------------
# Main
# -----------------------------
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--input-dir", default="heavy_cleaned_texts")
    ap.add_argument("--output", default="fragments.json")
    ap.add_argument("--total", type=int, default=12000)
    ap.add_argument("--discard-bottom", type=int, default=3000)
    ap.add_argument("--min-words", type=int, default=2)
    ap.add_argument("--max-words", type=int, default=5)
    ap.add_argument("--min-alpha-ratio", type=float, default=0.45)
    ap.add_argument("--spacy-model", default="en_core_web_md")
    ap.add_argument("--seed", type=int, default=42)
    ap.add_argument("--progress-every", type=int, default=20000)
    ap.add_argument("--log-level", default="INFO", choices=["DEBUG", "INFO", "WARNING", "ERROR"])
    args = ap.parse_args()

    logging.basicConfig(level=getattr(logging, args.log_level), format="%(levelname)s: %(message)s")
    start_time = time.time()

    input_dir = Path(args.input_dir)
    if not input_dir.exists():
        raise SystemExit(f"Input dir not found: {input_dir}")

    rng = random.Random(args.seed)

    print(f"Loading spaCy model: {args.spacy_model}", flush=True)
    nlp = spacy.load(args.spacy_model, disable=["ner"])
    ensure_dependency_parser(nlp)

    files = list(iter_text_files(input_dir))
    print(f"Found {len(files)} text files in {input_dir}\n", flush=True)

    if not files:
        Path(args.output).write_text("[]", encoding="utf-8")
        return

    stream = build_stream_of_fragments(
        nlp,
        files,
        args.min_words,
        args.max_words,
        args.min_alpha_ratio,
    )
    sampled, total_seen = reservoir_sample_stream(stream, args.total, rng, args.progress_every)

    print(f"\nFinished sampling.")
    print(f"  Total fragments seen: {total_seen:,}")
    print(f"  Total sampled: {len(sampled):,}\n")

    if not sampled:
        Path(args.output).write_text("[]", encoding="utf-8")
        return

    print("Vectorizing sampled fragments...", flush=True)
    pipe_iter = nlp.pipe((f.text for f in sampled), batch_size=512)
    docs = list(tqdm(pipe_iter, total=len(sampled))) if HAS_TQDM else list(pipe_iter)

    vecs = []
    for d in docs:
        v = d.vector
        if v is not None and v.shape[0] > 0 and float(np.linalg.norm(v)) > 0.0:
            vecs.append(v)
    centroid = compute_centroid(vecs)

    print("Scoring fragments...", flush=True)
    scored = []
    for meta, doc in zip(sampled, docs):
        final_score, sem01 = score_fragment(doc, meta.cohesion, centroid)
        meta.score = final_score
        meta.semantic = sem01
        scored.append(meta)

    scored.sort(key=lambda x: x.score, reverse=True)

    discard = max(0, min(args.discard_bottom, len(scored)))
    final = scored[:-discard] if discard else scored

    out_path = Path(args.output)
    out_path.write_text(
        json.dumps([asdict(f) for f in final], ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    print(f"Discarded bottom {discard}. Final fragments: {len(final):,}")
    print(f"Done. Wrote {out_path}")
    print(f"Runtime: {time.time() - start_time:.2f}s")


if __name__ == "__main__":
    main()