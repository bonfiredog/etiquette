#!/usr/bin/env python3
"""
2-extract_sentences.py
Stage 2: Sentence extraction + ranking (spaCy) with CLI progress output.

What it does:
- Reads all *.txt in an input directory (default: cleaned_texts)
- Uses spaCy sentence segmentation to extract complete sentences
- Reservoir-samples up to --total sentences at random across all files
- Ranks sentences by "sensibleness" using:
    * semantic coherence (sentence vector similarity to centroid)
    * simple linguistic heuristics (has verb, alpha ratio, length, ending punctuation)
- Discards bottom --discard-bottom sentences
- Writes final sentences to sentences.json

Progress output:
- File-by-file processing messages
- Sampling progress every --progress-every sentences seen
- Vectorization progress bar if tqdm is installed

Requirements:
  pip install spacy
  python -m spacy download en_core_web_md   (recommended; vectors required for semantic scoring)

Optional:
  pip install tqdm
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
from typing import Dict, Iterable, List, Optional, Tuple

import spacy

try:
    from tqdm import tqdm  # type: ignore
    HAS_TQDM = True
except ImportError:
    HAS_TQDM = False


# -----------------------------
# Data model
# -----------------------------

@dataclass
class RankedSentence:
    text: str
    score: float
    source_file: str
    start_char: int
    end_char: int


# -----------------------------
# File iteration
# -----------------------------

def iter_text_files(input_dir: Path) -> Iterable[Path]:
    for p in sorted(input_dir.glob("*.txt")):
        if p.is_file():
            yield p


# -----------------------------
# spaCy setup
# -----------------------------

def ensure_sentence_segmentation(nlp) -> None:
    """
    Ensure we can get sentence boundaries.
    If the model doesn't have a parser/senter, add a sentencizer.
    """
    if not nlp.has_pipe("parser") and not nlp.has_pipe("senter"):
        if not nlp.has_pipe("sentencizer"):
            nlp.add_pipe("sentencizer")
            logging.info("Added spaCy 'sentencizer' for sentence boundaries.")


# -----------------------------
# Sentence checks
# -----------------------------

def normalize_sentence_text(s: str) -> str:
    s = s.replace("\r\n", "\n").replace("\r", "\n")
    s = re.sub(r"\s+", " ", s).strip()
    return s


def is_complete_sentence(s: str) -> bool:
    """
    Conservative checks for "complete" sentences.
    """
    if len(s) < 20:
        return False
    if len(s) > 400:
        return False

    if sum(ch.isalpha() for ch in s) < 10:
        return False

    # Ends with sentence punctuation, optionally followed by quote/bracket.
    if not re.search(r"""[.!?]["')\]]?\s*$""", s):
        return False

    if re.match(r"^\s*(chapter|contents|table of contents)\b", s, flags=re.I):
        return False

    return True


# -----------------------------
# Reservoir sampling (with progress)
# -----------------------------

def reservoir_sample_stream(
    stream: Iterable[RankedSentence],
    k: int,
    rng: random.Random,
    progress_every: int = 10000,
) -> Tuple[List[RankedSentence], int]:
    """
    Uniformly sample k items from a stream without storing everything.
    Returns (sample, total_seen).
    """
    sample: List[RankedSentence] = []
    n = 0

    for item in stream:
        n += 1

        if len(sample) < k:
            sample.append(item)
        else:
            j = rng.randint(1, n)
            if j <= k:
                sample[j - 1] = item

        if progress_every and (n % progress_every == 0):
            print(f"  Seen: {n:,} | Sampled: {len(sample):,}", flush=True)

    return sample, n


# -----------------------------
# Scoring / ranking
# -----------------------------

def alpha_ratio(text: str) -> float:
    total = max(len(text), 1)
    letters = sum(ch.isalpha() for ch in text)
    return letters / total


def length_score(n_chars: int) -> float:
    """
    Peak around ~120 chars; penalize very short/very long.
    Returns [0..1].
    """
    mu = 120.0
    sigma = 80.0
    z = (n_chars - mu) / sigma
    return float(math.exp(-0.5 * z * z))


def has_verb(doc) -> bool:
    for tok in doc:
        if tok.pos_ == "VERB" or tok.pos_ == "AUX":
            return True
    return False


def semantic_coherence(sentence_doc, centroid_vec) -> float:
    """
    Cosine similarity to centroid vector.
    Returns [-1..1], later remapped to [0..1].
    """
    if centroid_vec is None:
        return 0.0

    v = sentence_doc.vector
    if v is None or v.shape[0] == 0:
        return 0.0

    denom = (float((v @ v) ** 0.5) * float((centroid_vec @ centroid_vec) ** 0.5))
    if denom == 0.0:
        return 0.0

    return float((v @ centroid_vec) / denom)


def compute_centroid(docs) -> Optional["spacy.tokens.Doc"]:
    """
    Return a centroid vector (numpy array) over docs with vectors.
    """
    vecs = []
    for d in docs:
        v = d.vector
        if v is not None and v.shape[0] > 0:
            if float((v @ v) ** 0.5) > 0.0:
                vecs.append(v)

    if not vecs:
        return None

    import numpy as np  # local import
    centroid = np.mean(np.stack(vecs, axis=0), axis=0)
    return centroid


def score_sentence(doc, centroid_vec) -> float:
    """
    Combine semantics + heuristics into a single score in [0..1].
    """
    text = doc.text
    a = alpha_ratio(text)                 # 0..1
    l = length_score(len(text))           # 0..1
    v = 1.0 if has_verb(doc) else 0.0     # 0 or 1
    ends = 1.0 if re.search(r"""[.!?]["')\]]?\s*$""", text) else 0.0

    sim = semantic_coherence(doc, centroid_vec)
    sim01 = (sim + 1.0) / 2.0

    score = (
        0.45 * sim01 +
        0.20 * v +
        0.15 * a +
        0.15 * l +
        0.05 * ends
    )

    return max(0.0, min(1.0, float(score)))


# -----------------------------
# Stream construction (file-by-file progress)
# -----------------------------

def build_stream_of_sentences(nlp, files: List[Path]) -> Iterable[RankedSentence]:
    total_files = len(files)

    for i, fp in enumerate(files, 1):
        print(f"Processing file {i}/{total_files}: {fp.name}", flush=True)

        text = fp.read_text(encoding="utf-8", errors="replace")
        if not text.strip():
            continue

        doc = nlp(text)
        for sent in doc.sents:
            s = normalize_sentence_text(sent.text)
            if not s:
                continue
            if not is_complete_sentence(s):
                continue

            yield RankedSentence(
                text=s,
                score=0.0,
                source_file=fp.name,
                start_char=sent.start_char,
                end_char=sent.end_char,
            )


# -----------------------------
# CLI
# -----------------------------

def main() -> None:
    ap = argparse.ArgumentParser(description="Extract + rank random complete sentences from cleaned_texts.")
    ap.add_argument("--input-dir", default="cleaned_texts", help="Directory with chunked cleaned .txt files")
    ap.add_argument("--total", type=int, default=3000, help="Max number of sentences to sample")
    ap.add_argument("--discard-bottom", type=int, default=500, help="Discard bottom N after ranking")
    ap.add_argument("--output", default="sentences.json", help="Output JSON file path")
    ap.add_argument("--spacy-model", default="en_core_web_md", help="spaCy model (md/lg recommended for vectors)")
    ap.add_argument("--seed", type=int, default=42, help="Random seed for reproducibility")
    ap.add_argument("--progress-every", type=int, default=10000,
                    help="Print sampling progress every N sentences seen (default: 10000)")
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
    ensure_sentence_segmentation(nlp)

    files = list(iter_text_files(input_dir))
    print(f"Found {len(files)} cleaned text files in {input_dir}\n", flush=True)

    if not files:
        Path(args.output).write_text("[]", encoding="utf-8")
        print(f"No input files. Wrote empty {args.output}", flush=True)
        return

    print(f"Sampling up to {args.total:,} sentences...\n", flush=True)
    stream = build_stream_of_sentences(nlp, files)
    sampled, total_seen = reservoir_sample_stream(
        stream,
        k=args.total,
        rng=rng,
        progress_every=args.progress_every,
    )

    print(f"\nFinished sampling.", flush=True)
    print(f"  Total complete sentences seen: {total_seen:,}", flush=True)
    print(f"  Total sampled: {len(sampled):,}\n", flush=True)

    if not sampled:
        Path(args.output).write_text("[]", encoding="utf-8")
        print(f"No sentences found. Wrote empty {args.output}", flush=True)
        return

    # Vectorize sampled sentences
    print("Vectorizing sampled sentences...", flush=True)
    pipe_iter = nlp.pipe((s.text for s in sampled), batch_size=256)

    if HAS_TQDM:
        sentence_docs = list(tqdm(pipe_iter, total=len(sampled)))
    else:
        sentence_docs = list(pipe_iter)

    # Compute centroid for semantic coherence
    print("Computing semantic centroid...", flush=True)
    centroid_vec = None
    try:
        centroid_vec = compute_centroid(sentence_docs)
        if centroid_vec is None:
            logging.warning(
                "No usable vectors found. Semantic ranking will be weak. "
                "Use en_core_web_md or en_core_web_lg for vectors."
            )
    except Exception as e:
        logging.warning("Could not compute centroid vector (%s). Proceeding without semantics.", e)
        centroid_vec = None

    # Score
    print("Scoring sentences...", flush=True)
    scored: List[RankedSentence] = []
    for meta, doc in zip(sampled, sentence_docs):
        meta.score = score_sentence(doc, centroid_vec)
        scored.append(meta)

    scored.sort(key=lambda x: x.score, reverse=True)

    discard = max(0, min(args.discard_bottom, len(scored)))
    final = scored[:-discard] if discard else scored

    print(f"Discarded bottom {discard}. Final sentences: {len(final):,}", flush=True)

    out_path = Path(args.output)
    out_path.write_text(
        json.dumps([asdict(s) for s in final], ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    elapsed = time.time() - start_time
    print(f"\nDone. Wrote {out_path}", flush=True)
    print(f"Total runtime: {elapsed:.2f} seconds", flush=True)


if __name__ == "__main__":
    main()