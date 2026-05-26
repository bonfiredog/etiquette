#!/usr/bin/env python3
"""
prepare_texts.py

Final preparation stage for categorisation.

- Input:  heavy_cleaned_texts/*.txt
- Output: final_prepared_texts/*.txt

Operations:
- Lowercase all text
- Remove all punctuation
- Remove all non-ASCII characters
- Keep only: a-z and 0-9
- Normalize whitespace
"""

from __future__ import annotations

import argparse
import logging
import re
import time
from pathlib import Path


# -----------------------------
# Core cleaning
# -----------------------------

ALLOWED_RE = re.compile(r"[^a-z0-9\s]")   # keep lowercase letters, digits, whitespace
WHITESPACE_RE = re.compile(r"\s+")


def prepare_text(text: str) -> str:
    """
    Convert to lowercase and remove punctuation and non a-z0-9 characters.
    """
    # Lowercase
    text = text.lower()

    # Remove everything not a-z, 0-9, whitespace
    text = ALLOWED_RE.sub(" ", text)

    # Normalize whitespace
    text = WHITESPACE_RE.sub(" ", text)

    return text.strip()


# -----------------------------
# Processing
# -----------------------------

def process_directory(input_dir: Path, output_dir: Path) -> None:
    output_dir.mkdir(parents=True, exist_ok=True)

    files = sorted([p for p in input_dir.glob("*.txt") if p.is_file()])
    print(f"Found {len(files)} files in {input_dir}\n")

    total_chars_before = 0
    total_chars_after = 0

    for i, fp in enumerate(files, 1):
        print(f"[{i}/{len(files)}] Processing {fp.name}")

        raw = fp.read_text(encoding="utf-8", errors="ignore")
        total_chars_before += len(raw)

        cleaned = prepare_text(raw)
        total_chars_after += len(cleaned)

        out_fp = output_dir / fp.name
        out_fp.write_text(cleaned, encoding="utf-8")

    print("\nDone.")
    print(f"Total characters before: {total_chars_before:,}")
    print(f"Total characters after : {total_chars_after:,}")
    if total_chars_before > 0:
        pct = (total_chars_after / total_chars_before) * 100
        print(f"Retention: {pct:.2f}%")



# -----------------------------
# CLI
# -----------------------------

def main() -> None:
    ap = argparse.ArgumentParser(description="Final text preparation for categorisation.")
    ap.add_argument("--input-dir", default="heavy_cleaned_texts",
                    help="Input directory (default: heavy_cleaned_texts)")
    ap.add_argument("--output-dir", default="final_prepared_texts",
                    help="Output directory (default: final_prepared_texts)")
    args = ap.parse_args()

    start = time.time()

    input_dir = Path(args.input_dir)
    output_dir = Path(args.output_dir)

    if not input_dir.exists():
        raise SystemExit(f"Input directory not found: {input_dir}")

    process_directory(input_dir, output_dir)

    elapsed = time.time() - start
    print(f"Total runtime: {elapsed:.2f} seconds")


if __name__ == "__main__":
    main()