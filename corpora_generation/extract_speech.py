#!/usr/bin/env python3
"""
3-extract_direct_speech.py
Stage 3: Extract + rank random direct-speech passages from cleaned texts (spaCy) with CLI progress.

What it does:
- Reads all *.txt in an input directory (default: heavy_cleaned_texts)
- Extracts direct speech passages based on quotation marks:
    * Handles "..." passages possibly spanning multiple sentences/newlines
    * Conservatively avoids extracting tiny/empty quotes
    * NEW: only keeps quotes that end like complete utterances (., !, ?)
    * NEW: enforces a minimum character length for the extracted utterance
- Reservoir-samples up to --total passages at random across all files
- Ranks passages by "sensibleness" using:
    * semantic coherence (passage vector similarity to centroid)
    * simple linguistic heuristics (has verb, alpha ratio, length)
- Discards bottom --discard-bottom passages
- Writes final passages to speech.json

Options:
- --allow-multiline: allow quotes that span newlines
- --strip-speech-marks: remove outer quote marks from passages in final output JSON
- --min-chars: minimum length of extracted quoted utterance (including quotes unless stripped at end)

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
from typing import Iterable, List, Optional, Tuple

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
class RankedSpeech:
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
    if not nlp.has_pipe("parser") and not nlp.has_pipe("senter"):
        if not nlp.has_pipe("sentencizer"):
            nlp.add_pipe("sentencizer")
            logging.info("Added spaCy 'sentencizer' for sentence boundaries.")


# -----------------------------
# Normalization helpers
# -----------------------------

def normalize_quotes(text: str) -> str:
    replacements = {
        "\u201c": '"', "\u201d": '"', "\u201e": '"', "\u00ab": '"', "\u00bb": '"',
        "\u2018": "'", "\u2019": "'", "\u201a": "'", "\u2032": "'", "\u2033": '"',
    }
    for src, dst in replacements.items():
        text = text.replace(src, dst)
    text = text.replace("`", "'")
    return text


def normalize_whitespace(s: str) -> str:
    s = s.replace("\r\n", "\n").replace("\r", "\n")
    s = re.sub(r"[ \t]+", " ", s)
    s = re.sub(r"\s+\n", "\n", s)
    s = re.sub(r"\n{3,}", "\n\n", s)
    return s.strip()


def normalize_passage_text(s: str) -> str:
    return re.sub(r"\s+", " ", s).strip()


def strip_outer_speech_marks(s: str) -> str:
    """
    Remove ONLY outermost leading/trailing double quotes if present.
    Keeps interior quotes/apostrophes.
    """
    s = s.strip()
    if len(s) >= 2 and s[0] == '"' and s[-1] == '"':
        s = s[1:-1].strip()
    return s


# NEW: full-utterance detector (rejects quotes ending in comma, colon, etc.)
FULL_UTTERANCE_RE = re.compile(r"""[.!?]["')\]]?\s*$""")


# -----------------------------
# Direct speech extraction
# -----------------------------

QUOTE_SPAN_RE = re.compile(r'"([^"\n]{0,5000}?)"', flags=re.DOTALL)
QUOTE_SPAN_RE_DOTALL = re.compile(r'"(.{1,8000}?)"', flags=re.DOTALL)


def extract_quoted_passages(
    text: str,
    allow_multiline: bool,
    min_chars: int,
) -> List[Tuple[str, int, int]]:
    """
    Returns list of (passage_text, start_char, end_char) INCLUDING the outer quotes.
    Keeps quotes at extraction-time for reliability.

    Filters:
    - must end with . ! ? (optionally followed by closing quote/bracket)
    - must be at least min_chars long (after whitespace normalization)
    """
    rx = QUOTE_SPAN_RE_DOTALL if allow_multiline else QUOTE_SPAN_RE
    out: List[Tuple[str, int, int]] = []

    for m in rx.finditer(text):
        start, end = m.span()
        raw_inner = m.group(1)

        passage = '"' + raw_inner + '"'
        passage = normalize_passage_text(passage)

        # Minimum length (user-controlled)
        if len(passage) < min_chars:
            continue

        # Basic filters
        alpha = sum(ch.isalpha() for ch in passage)
        if alpha < 8:
            continue

        if re.match(r'^"\s*(chapter|contents)\b', passage, flags=re.I):
            continue

        # NEW: keep only full utterances (reject comma-ended partial quotes)
        if not FULL_UTTERANCE_RE.search(passage):
            continue

        out.append((passage, start, end))

    return out


# -----------------------------
# Reservoir sampling (with progress)
# -----------------------------

def reservoir_sample_stream(
    stream: Iterable[RankedSpeech],
    k: int,
    rng: random.Random,
    progress_every: int = 5000,
) -> Tuple[List[RankedSpeech], int]:
    sample: List[RankedSpeech] = []
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
    mu = 220.0
    sigma = 180.0
    z = (n_chars - mu) / sigma
    return float(math.exp(-0.5 * z * z))


def has_verb(doc) -> bool:
    for tok in doc:
        if tok.pos_ == "VERB" or tok.pos_ == "AUX":
            return True
    return False


def semantic_coherence(doc, centroid_vec) -> float:
    if centroid_vec is None:
        return 0.0
    v = doc.vector
    if v is None or v.shape[0] == 0:
        return 0.0
    denom = (float((v @ v) ** 0.5) * float((centroid_vec @ centroid_vec) ** 0.5))
    if denom == 0.0:
        return 0.0
    return float((v @ centroid_vec) / denom)


def compute_centroid(docs) -> Optional["spacy.tokens.Doc"]:
    vecs = []
    for d in docs:
        v = d.vector
        if v is not None and v.shape[0] > 0:
            if float((v @ v) ** 0.5) > 0.0:
                vecs.append(v)
    if not vecs:
        return None
    import numpy as np  # local import
    return np.mean(np.stack(vecs, axis=0), axis=0)


def score_passage(doc, centroid_vec) -> float:
    text = doc.text
    a = alpha_ratio(text)
    l = length_score(len(text))
    v = 1.0 if has_verb(doc) else 0.0

    sim = semantic_coherence(doc, centroid_vec)
    sim01 = (sim + 1.0) / 2.0

    score = (0.55 * sim01 + 0.20 * v + 0.15 * a + 0.10 * l)
    return max(0.0, min(1.0, float(score)))


# -----------------------------
# Stream construction
# -----------------------------

def build_stream_of_speech(
    nlp,
    files: List[Path],
    allow_multiline: bool,
    min_chars: int,
) -> Iterable[RankedSpeech]:
    total_files = len(files)

    for i, fp in enumerate(files, 1):
        print(f"Processing file {i}/{total_files}: {fp.name}", flush=True)

        text = fp.read_text(encoding="utf-8", errors="replace")
        if not text.strip():
            continue

        text = normalize_quotes(text)
        text = normalize_whitespace(text)

        passages = extract_quoted_passages(text, allow_multiline=allow_multiline, min_chars=min_chars)
        for passage, start, end in passages:
            yield RankedSpeech(
                text=passage,
                score=0.0,
                source_file=fp.name,
                start_char=start,
                end_char=end,
            )


# -----------------------------
# CLI
# -----------------------------

def main() -> None:
    ap = argparse.ArgumentParser(description="Extract + rank random direct speech passages from cleaned texts.")
    ap.add_argument("--input-dir", default="heavy_cleaned_texts",
                    help="Directory with cleaned .txt files (default: heavy_cleaned_texts)")
    ap.add_argument("--total", type=int, default=2000, help="Max number of speech passages to sample")
    ap.add_argument("--discard-bottom", type=int, default=300, help="Discard bottom N after ranking")
    ap.add_argument("--output", default="speech.json", help="Output JSON file path")
    ap.add_argument("--spacy-model", default="en_core_web_md", help="spaCy model (md/lg recommended for vectors)")
    ap.add_argument("--seed", type=int, default=42, help="Random seed for reproducibility")
    ap.add_argument("--allow-multiline", action="store_true",
                    help='Allow quotes that span newlines (useful for some OCR/formatting)')
    ap.add_argument("--strip-speech-marks", action="store_true",
                    help='Remove outer quote marks from passages in the final output JSON')
    ap.add_argument("--min-chars", type=int, default=60,
                    help="Minimum length of extracted quoted utterance (default: 60)")
    ap.add_argument("--progress-every", type=int, default=5000,
                    help="Print sampling progress every N passages seen (default: 5000)")
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
    print(f"Found {len(files)} text files in {input_dir}\n", flush=True)

    if not files:
        Path(args.output).write_text("[]", encoding="utf-8")
        print(f"No input files. Wrote empty {args.output}", flush=True)
        return

    print(f"Sampling up to {args.total:,} speech passages...\n", flush=True)
    stream = build_stream_of_speech(
        nlp=nlp,
        files=files,
        allow_multiline=args.allow_multiline,
        min_chars=args.min_chars,
    )
    sampled, total_seen = reservoir_sample_stream(
        stream,
        k=args.total,
        rng=rng,
        progress_every=args.progress_every,
    )

    print(f"\nFinished sampling.", flush=True)
    print(f"  Total speech passages seen: {total_seen:,}", flush=True)
    print(f"  Total sampled: {len(sampled):,}\n", flush=True)

    if not sampled:
        Path(args.output).write_text("[]", encoding="utf-8")
        print(f"No speech passages found. Wrote empty {args.output}", flush=True)
        return

    print("Vectorizing sampled passages...", flush=True)
    pipe_iter = nlp.pipe((s.text for s in sampled), batch_size=128)
    speech_docs = list(tqdm(pipe_iter, total=len(sampled))) if HAS_TQDM else list(pipe_iter)

    print("Computing semantic centroid...", flush=True)
    centroid_vec = None
    try:
        centroid_vec = compute_centroid(speech_docs)
        if centroid_vec is None:
            logging.warning(
                "No usable vectors found. Semantic ranking will be weak. "
                "Use en_core_web_md or en_core_web_lg for vectors."
            )
    except Exception as e:
        logging.warning("Could not compute centroid vector (%s). Proceeding without semantics.", e)
        centroid_vec = None

    print("Scoring passages...", flush=True)
    scored: List[RankedSpeech] = []
    for meta, doc in zip(sampled, speech_docs):
        meta.score = score_passage(doc, centroid_vec)
        scored.append(meta)

    scored.sort(key=lambda x: x.score, reverse=True)

    discard = max(0, min(args.discard_bottom, len(scored)))
    final = scored[:-discard] if discard else scored

    # Optional: strip outer quotes at the end
    if args.strip_speech_marks:
        for item in final:
            item.text = strip_outer_speech_marks(item.text)
        # If stripping makes it shorter than min_chars, drop it (rare but possible)
        final = [x for x in final if len(x.text) >= args.min_chars]

    print(f"Discarded bottom {discard}. Final passages: {len(final):,}", flush=True)

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