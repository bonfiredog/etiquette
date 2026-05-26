#!/usr/bin/env python3
"""
cleanerheavy.py
Aggressive-but-safe legibility cleanup for already-cleaned text chunks.

Input:  cleaned_texts/*.txt
Output: heavy_cleaned_texts/*.txt  (UTF-8)
Report: heavy_cleaned_texts/reports/<stem>.heavy_report.json

Goals:
- Repair common OCR artifacts (ligatures, soft hyphen, stray replacement chars)
- Fix broken words across line breaks (hyphenation)
- Unwrap hard-wrapped lines inside paragraphs (single newlines -> spaces)
- Remove control/weird characters while keeping punctuation/speech marks
- Do NOT remove "non-English" words yet; keep Unicode letters.
"""

from __future__ import annotations

import argparse
import json
import logging
import re
import unicodedata
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Dict, List, Tuple


# -----------------------------
# Reporting
# -----------------------------

@dataclass
class HeavyCleanReport:
    input_file: str
    output_file: str
    original_chars: int
    final_chars: int
    final_ratio_pct: float

    # Repairs
    ligatures_fixed: int = 0
    soft_hyphens_removed: int = 0
    hyphenated_linebreak_joins: int = 0
    unwrap_line_merges: int = 0
    ocr_substitutions: Dict[str, int] = None

    # Character filtering
    control_chars_removed: int = 0
    odd_chars_removed: int = 0
    replacement_chars_removed: int = 0

    def __post_init__(self) -> None:
        if self.ocr_substitutions is None:
            self.ocr_substitutions = {}


# -----------------------------
# Heavy cleaner
# -----------------------------

class HeavyCleaner:
    """
    Operates on already-cleaned text chunks and makes them more legible.
    """

    # A conservative set of punctuation we consider "standard" to keep.
    # We still keep other punctuation if it's in Unicode punctuation categories.
    STANDARD_PUNCT = set(r"""!"'(),-.:;?[]{}<>/\\|@#$%^&*_+=~`""")

    # Common OCR ligatures and typography artifacts
    LIGATURES = {
        "\ufb00": "ff",
        "\ufb01": "fi",
        "\ufb02": "fl",
        "\ufb03": "ffi",
        "\ufb04": "ffl",
        "\ufb05": "ft",
        "\ufb06": "st",
        "ﬁ": "fi",
        "ﬂ": "fl",
    }

    # Common OCR confusions – keep this small and low-risk.
    # NOTE: avoid “rn->m” etc. (too many false positives).
    OCR_SUBS = {
        "’": "'",  # curly apostrophe
        "‘": "'",
        "“": '"',
        "”": '"',
        "„": '"',
        "—": "-",
        "–": "-",
        "−": "-",
        "…": "...",
        "\u00a0": " ",   # NBSP
    }

    def __init__(self, keep_paragraphs: bool = True):
        self.keep_paragraphs = keep_paragraphs

    # ---------- Core steps ----------

    def fix_ligatures(self, text: str, report: HeavyCleanReport) -> str:
        for src, dst in self.LIGATURES.items():
            c = text.count(src)
            if c:
                report.ligatures_fixed += c
                text = text.replace(src, dst)
        return text

    def normalize_ocr_punct(self, text: str, report: HeavyCleanReport) -> str:
        for src, dst in self.OCR_SUBS.items():
            c = text.count(src)
            if c:
                key = f"{src}->{dst}"
                report.ocr_substitutions[key] = report.ocr_substitutions.get(key, 0) + c
                text = text.replace(src, dst)
        return text

    def remove_soft_hyphen(self, text: str, report: HeavyCleanReport) -> str:
        # Soft hyphen is a layout artifact; remove it safely.
        c = text.count("\u00ad")
        if c:
            report.soft_hyphens_removed += c
            text = text.replace("\u00ad", "")
        return text

    def fix_hyphenated_linebreaks(self, text: str, report: HeavyCleanReport) -> str:
        """
        Join words broken like:
            exam-
            ple
        -> example

        Only joins when both sides look like word parts.
        """
        # Count before/after using finditer count.
        pattern = re.compile(r"([A-Za-z])-\n([A-Za-z])")
        matches = list(pattern.finditer(text))
        if matches:
            report.hyphenated_linebreak_joins += len(matches)
            text = pattern.sub(r"\1\2", text)
        return text

    def unwrap_hard_wrapped_lines(self, text: str, report: HeavyCleanReport) -> str:
        """
        Many OCR/books are hard-wrapped at ~80 chars.
        We want paragraphs, not line breaks.

        Strategy:
        - Preserve blank lines as paragraph breaks
        - For non-blank lines inside a paragraph, replace newline with space
          unless the line ends with strong punctuation suggesting a paragraph break.

        This is intentionally conservative.
        """
        # Normalize newlines
        text = text.replace("\r\n", "\n").replace("\r", "\n")

        paras = text.split("\n\n")
        new_paras: List[str] = []

        strong_end = re.compile(r"""[.!?]["')\]]?\s*$""")

        merges = 0
        for para in paras:
            lines = [ln.strip() for ln in para.split("\n") if ln.strip() != ""]
            if not lines:
                new_paras.append("")
                continue

            rebuilt = []
            for i, ln in enumerate(lines):
                if i == 0:
                    rebuilt.append(ln)
                    continue

                prev = rebuilt[-1]
                # If previous line strongly ends, keep a paragraph-ish break as space anyway
                # (we are unwrapping, not reflowing into new paragraphs)
                if strong_end.search(prev):
                    rebuilt[-1] = prev + " " + ln
                    merges += 1
                else:
                    rebuilt[-1] = prev + " " + ln
                    merges += 1

            new_paras.append("".join(rebuilt))

        report.unwrap_line_merges += merges
        return "\n\n".join(p for p in new_paras if p is not None).strip()

    def filter_weird_characters(self, text: str, report: HeavyCleanReport) -> str:
        """
        Remove control chars and “odd” symbols that hurt legibility,
        while keeping:
        - Unicode letters/digits/marks
        - whitespace
        - Unicode punctuation
        - common math/connector characters that appear in text
        """
        out_chars: List[str] = []
        for ch in text:
            if ch == "\ufffd":  # replacement character
                report.replacement_chars_removed += 1
                continue

            cat = unicodedata.category(ch)  # e.g. 'Ll', 'Po', 'Cc'
            if cat == "Cc":
                # keep newlines/tabs; remove other controls
                if ch in ("\n", "\t"):
                    out_chars.append(ch)
                else:
                    report.control_chars_removed += 1
                continue

            # Keep letters, marks, numbers
            if cat[0] in ("L", "M", "N"):
                out_chars.append(ch)
                continue

            # Keep spaces/separators
            if cat[0] == "Z":
                out_chars.append(" ")
                continue

            # Keep punctuation and symbols that behave like punctuation
            if cat[0] == "P":
                out_chars.append(ch)
                continue

            # Symbols: keep a small subset that is frequently meaningful in text
            if cat[0] == "S":
                # Keep currency and a few common text symbols; discard emoji/dingbats etc.
                if ch in "£$€¥©®™":
                    out_chars.append(ch)
                else:
                    report.odd_chars_removed += 1
                continue

            # Fallback: keep if it’s a standard ASCII punctuation we know
            if ch in self.STANDARD_PUNCT:
                out_chars.append(ch)
            else:
                report.odd_chars_removed += 1

        # Collapse whitespace a bit, but preserve paragraph breaks
        filtered = "".join(out_chars)
        filtered = re.sub(r"[ \t]+", " ", filtered)
        filtered = re.sub(r"\n{3,}", "\n\n", filtered)
        return filtered.strip()

    def clean(self, text: str, report: HeavyCleanReport) -> str:
        # Order matters
        text = self.fix_ligatures(text, report)
        text = self.normalize_ocr_punct(text, report)
        text = self.remove_soft_hyphen(text, report)
        text = self.fix_hyphenated_linebreaks(text, report)
        text = self.unwrap_hard_wrapped_lines(text, report)
        text = self.filter_weird_characters(text, report)

        return text


# -----------------------------
# Pipeline
# -----------------------------

def process_directory(input_dir: Path, output_dir: Path) -> None:
    cleaner = HeavyCleaner()

    output_dir.mkdir(parents=True, exist_ok=True)
    report_dir = output_dir / "reports"
    report_dir.mkdir(parents=True, exist_ok=True)

    files = sorted([p for p in input_dir.glob("*.txt") if p.is_file()])
    logging.info("Found %d .txt files in %s", len(files), input_dir)

    for fp in files:
        raw = fp.read_text(encoding="utf-8", errors="replace")
        out_fp = output_dir / fp.name

        report = HeavyCleanReport(
            input_file=str(fp),
            output_file=str(out_fp),
            original_chars=len(raw),
            final_chars=0,
            final_ratio_pct=0.0,
        )

        cleaned = cleaner.clean(raw, report)

        report.final_chars = len(cleaned)
        report.final_ratio_pct = (report.final_chars / report.original_chars * 100.0) if report.original_chars else 0.0

        out_fp.write_text(cleaned, encoding="utf-8")
        rep_fp = report_dir / f"{fp.stem}.heavy_report.json"
        rep_fp.write_text(json.dumps(asdict(report), ensure_ascii=False, indent=2), encoding="utf-8")

        logging.info(
            "Processed %-40s  %s -> %s chars (%.1f%%)  | reports/%s",
            fp.name,
            f"{report.original_chars:,}",
            f"{report.final_chars:,}",
            report.final_ratio_pct,
            rep_fp.name,
        )


def main() -> None:
    ap = argparse.ArgumentParser(description="Aggressive legibility cleaner for cleaned_texts chunks.")
    ap.add_argument("--input-dir", default="cleaned_texts", help="Input directory (default: cleaned_texts)")
    ap.add_argument("--output-dir", default="heavy_cleaned_texts", help="Output directory (default: heavy_cleaned_texts)")
    ap.add_argument("--log-level", default="INFO", choices=["DEBUG", "INFO", "WARNING", "ERROR"])
    args = ap.parse_args()

    logging.basicConfig(level=getattr(logging, args.log_level), format="%(levelname)s: %(message)s")

    input_dir = Path(args.input_dir)
    output_dir = Path(args.output_dir)

    if not input_dir.exists():
        raise SystemExit(f"Input directory not found: {input_dir}")

    process_directory(input_dir, output_dir)


if __name__ == "__main__":
    main()