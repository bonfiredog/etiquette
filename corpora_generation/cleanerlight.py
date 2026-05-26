"""
Text Cleaning Pipeline for NLP Processing (Stage 1)
- Keeps punctuation (including speech marks)
- Removes page numbers, headers/footers, front/back matter
- Normalizes quote style and punctuation
- Normalizes whitespace
- Chunks large texts
- Saves cleaned UTF-8 outputs into cleaned_texts/
- Writes a JSON report describing what was removed/changed
"""

from __future__ import annotations

import argparse
import json
import logging
import os
import re
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Dict, List, Tuple


# -----------------------------
# Reporting
# -----------------------------

@dataclass
class CleanReport:
    input_file: str
    output_dir: str
    original_chars: int
    final_chars: int
    final_ratio_pct: float

    # removals
    removed_page_number_lines: int = 0
    removed_header_footer_lines: int = 0
    removed_frontmatter_chars: int = 0
    removed_backmatter_chars: int = 0

    # normalizations
    quote_normalizations: Dict[str, int] = None
    punctuation_normalizations: Dict[str, int] = None
    whitespace_collapses: Dict[str, int] = None

    # chunking
    chunked: bool = False
    chunks_written: int = 0
    chunk_files: List[str] = None

    def __post_init__(self) -> None:
        if self.quote_normalizations is None:
            self.quote_normalizations = {}
        if self.punctuation_normalizations is None:
            self.punctuation_normalizations = {}
        if self.whitespace_collapses is None:
            self.whitespace_collapses = {}
        if self.chunk_files is None:
            self.chunk_files = []


# -----------------------------
# Cleaner
# -----------------------------

class TextCleaner:
    def __init__(self, max_chunk_size: int = 100000, header_footer_threshold: int = 5):
        """
        Args:
            max_chunk_size: Maximum characters per chunk (default 100k)
            header_footer_threshold: lines repeating more than this count are removed
        """
        self.max_chunk_size = max_chunk_size
        self.header_footer_threshold = header_footer_threshold

    # ---------- IO ----------

    def read_file(self, filepath: str) -> str:
        """Read file with multiple encoding attempts."""
        encodings = ["utf-8", "utf-8-sig", "latin-1", "cp1252", "iso-8859-1"]
        last_err = None
        for enc in encodings:
            try:
                with open(filepath, "r", encoding=enc) as f:
                    return f.read()
            except UnicodeDecodeError as e:
                last_err = e
                continue
        raise ValueError(f"Could not decode {filepath} with common encodings. Last error: {last_err}")

    # ---------- Normalization ----------

    def normalize_quotes(self, text: str, report: CleanReport) -> str:
        """
        Normalize common quote styles to straight quotes.
        Keeps speech marks; just standardizes variants.
        """
        replacements = {
            "\u201c": '"',  # left double curly
            "\u201d": '"',  # right double curly
            "\u201e": '"',  # low double
            "\u00ab": '"',  # «
            "\u00bb": '"',  # »
            "\u2018": "'",  # left single curly
            "\u2019": "'",  # right single curly
            "\u201a": "'",  # low single
            "\u2032": "'",  # prime used as apostrophe
            "\u2033": '"',  # double prime
        }
        for src, dst in replacements.items():
            count = text.count(src)
            if count:
                report.quote_normalizations[f"{src}->{dst}"] = report.quote_normalizations.get(f"{src}->{dst}", 0) + count
                text = text.replace(src, dst)

        # Normalize weird backticks that often appear as quotes
        # (keep them as apostrophes/quotes rather than deleting)
        backtick_count = text.count("`")
        if backtick_count:
            report.quote_normalizations["`->'"] = report.quote_normalizations.get("`->'", 0) + backtick_count
            text = text.replace("`", "'")

        return text

    def normalize_punctuation(self, text: str, report: CleanReport) -> str:
        """
        Normalize common punctuation variants without discarding punctuation.
        """
        # Dashes: em/en/minus sign -> hyphen-minus
        dash_variants = ["\u2014", "\u2013", "\u2212"]  # — – −
        for dv in dash_variants:
            c = text.count(dv)
            if c:
                report.punctuation_normalizations[f"{dv}->-"] = report.punctuation_normalizations.get(f"{dv}->-", 0) + c
                text = text.replace(dv, "-")

        # Ellipsis character -> "..."
        ell = "\u2026"
        c = text.count(ell)
        if c:
            report.punctuation_normalizations["…->..."] = report.punctuation_normalizations.get("…->...", 0) + c
            text = text.replace(ell, "...")

        # Non-breaking space -> normal space
        nbsp = "\u00a0"
        c = text.count(nbsp)
        if c:
            report.punctuation_normalizations["NBSP->space"] = report.punctuation_normalizations.get("NBSP->space", 0) + c
            text = text.replace(nbsp, " ")

        # Remove soft hyphen (formatting artifact) without removing real hyphens
        soft_hyphen = "\u00ad"
        c = text.count(soft_hyphen)
        if c:
            report.punctuation_normalizations["soft_hyphen->(removed)"] = report.punctuation_normalizations.get("soft_hyphen->(removed)", 0) + c
            text = text.replace(soft_hyphen, "")

        # Strip other control chars except newline + tab
        # (keeps punctuation and letters from any language)
        before = len(text)
        text = re.sub(r"[\x00-\x08\x0b\x0c\x0e-\x1f]", "", text)
        removed = before - len(text)
        if removed:
            report.punctuation_normalizations["control_chars_removed"] = report.punctuation_normalizations.get("control_chars_removed", 0) + removed

        return text

    # ---------- Removal steps ----------

    def remove_page_numbers(self, text: str, report: CleanReport) -> str:
        """
        Remove standalone page numbers and common page-number patterns.
        Records how many *lines* were removed.
        """
        lines = text.split("\n")
        kept = []
        removed_lines = 0

        # Standalone numeric lines
        standalone_num = re.compile(r"^\s*\d+\s*$")

        # Patterns like "- 42 -" or "– 42 –" or "— 42 —"
        dashed_num = re.compile(r"^\s*[-–—]\s*\d+\s*[-–—]\s*$")

        # "Page 42" as a whole line (or mostly line)
        page_num = re.compile(r"^\s*page\s+\d+\s*$", re.IGNORECASE)

        for ln in lines:
            if standalone_num.match(ln) or dashed_num.match(ln) or page_num.match(ln):
                removed_lines += 1
                continue
            kept.append(ln)

        report.removed_page_number_lines += removed_lines
        return "\n".join(kept)

    def remove_headers_footers(self, text: str, report: CleanReport) -> str:
        """
        Remove repeated headers/footers (heuristic: short lines repeated often).
        Records how many lines were removed.
        """
        lines = text.split("\n")

        line_counts: Dict[str, int] = {}
        for line in lines:
            stripped = line.strip()
            if stripped and len(stripped) < 100:
                line_counts[stripped] = line_counts.get(stripped, 0) + 1

        repeated = {line for line, count in line_counts.items() if count > self.header_footer_threshold}

        if not repeated:
            return text

        kept = []
        removed_lines = 0
        for line in lines:
            if line.strip() in repeated:
                removed_lines += 1
            else:
                kept.append(line)

        report.removed_header_footer_lines += removed_lines
        return "\n".join(kept)

    def remove_frontmatter(self, text: str, report: CleanReport) -> str:
        """
        Remove front matter by jumping to first likely 'main content' marker.
        Records removed character count (approx).
        """
        text_lower = text.lower()

        # Look for the start of main content (first chapter / prologue / part one)
        patterns = [
            r"\bchapter\s+(?:one|1|i)\b",
            r"\bchapter\s+\d+\b",
            r"\bprologue\b",
            r"\bpart\s+(?:one|1|i)\b",
        ]

        main_start = None
        for pat in patterns:
            m = re.search(pat, text_lower)
            if m:
                main_start = m.start()
                break

        # Only cut if the marker is early-ish (avoid chopping mid-book)
        if main_start is not None and main_start < int(len(text) * 0.25):
            report.removed_frontmatter_chars += main_start
            return text[main_start:]

        return text

    def remove_backmatter(self, text: str, report: CleanReport) -> str:
        """
        Remove back matter by finding common end markers in last ~20% of text.
        Records removed character count (approx).
        """
        text_lower = text.lower()

        end_markers = [
            r"\b(?:the\s+)?end\s*$",
            r"\babout the author\b",
            r"\backnowledgments?\b",
            r"\bbibliography\b",
            r"\bindex\s*$",
            r"\balso by\b",
        ]

        search_start = int(len(text) * 0.80)
        tail = text_lower[search_start:]

        earliest_end = None
        for pat in end_markers:
            m = re.search(pat, tail, flags=re.MULTILINE)
            if m:
                pos = search_start + m.start()
                earliest_end = pos if earliest_end is None else min(earliest_end, pos)

        if earliest_end is not None and earliest_end < len(text):
            report.removed_backmatter_chars += (len(text) - earliest_end)
            return text[:earliest_end]

        return text

    # ---------- Whitespace ----------

    def normalize_whitespace(self, text: str, report: CleanReport) -> str:
        """
        Normalize whitespace without killing punctuation:
        - collapse runs of spaces/tabs to single spaces
        - collapse 3+ newlines to 2 newlines
        - trim ends
        """
        before = text

        # Convert Windows newlines
        text = text.replace("\r\n", "\n").replace("\r", "\n")

        # Collapse spaces/tabs (but don't remove newlines)
        text2 = re.sub(r"[ \t]+", " ", text)
        if text2 != text:
            report.whitespace_collapses["space_tab_runs_collapsed"] = report.whitespace_collapses.get("space_tab_runs_collapsed", 0) + 1
            text = text2

        # Collapse 3+ newlines to exactly 2
        text2 = re.sub(r"\n\s*\n\s*\n+", "\n\n", text)
        if text2 != text:
            report.whitespace_collapses["multi_blanklines_collapsed"] = report.whitespace_collapses.get("multi_blanklines_collapsed", 0) + 1
            text = text2

        # Strip outer whitespace
        text2 = text.strip()
        if text2 != text:
            report.whitespace_collapses["strip_outer_whitespace"] = report.whitespace_collapses.get("strip_outer_whitespace", 0) + 1
            text = text2

        _ = before
        return text

    # ---------- Pipeline ----------

    def clean_text(self, text: str, report: CleanReport) -> str:
        """
        Apply cleaning steps (no English-word filtering yet).
        Order matters: normalize quotes/punct first, then remove page nums/headers/etc.
        """
        text = self.normalize_quotes(text, report)
        text = self.normalize_punctuation(text, report)

        text = self.remove_page_numbers(text, report)
        text = self.remove_headers_footers(text, report)

        text = self.remove_frontmatter(text, report)
        text = self.remove_backmatter(text, report)

        text = self.normalize_whitespace(text, report)
        return text

    def chunk_text(self, text: str, filename: str) -> List[Tuple[str, str]]:
        """Split large texts into chunks (paragraph-boundary aware)."""
        if len(text) <= self.max_chunk_size:
            return [(text, filename)]

        chunks: List[Tuple[str, str]] = []
        base_name = Path(filename).stem

        paragraphs = text.split("\n\n")
        current: List[str] = []
        current_size = 0
        chunk_num = 1

        for para in paragraphs:
            para_size = len(para)

            # If a single paragraph is bigger than max_chunk_size,
            # we still place it into its own chunk (avoid infinite loops).
            if current and (current_size + para_size + 2) > self.max_chunk_size:
                chunk_text = "\n\n".join(current)
                chunk_name = f"{base_name}_chunk{chunk_num}.txt"
                chunks.append((chunk_text, chunk_name))
                chunk_num += 1
                current = []
                current_size = 0

            current.append(para)
            current_size += para_size + 2

        if current:
            chunk_text = "\n\n".join(current)
            chunk_name = f"{base_name}_chunk{chunk_num}.txt"
            chunks.append((chunk_text, chunk_name))

        return chunks

    def process_file(self, input_path: str, output_dir: str) -> List[str]:
        """
        Process one file.
        Writes cleaned chunk(s) and a JSON report.
        """
        input_p = Path(input_path)
        output_p = Path(output_dir)
        output_p.mkdir(parents=True, exist_ok=True)

        logging.info("Processing: %s", input_p)

        raw = self.read_file(str(input_p))
        report = CleanReport(
            input_file=str(input_p),
            output_dir=str(output_p),
            original_chars=len(raw),
            final_chars=0,
            final_ratio_pct=0.0,
        )

        cleaned = self.clean_text(raw, report)

        report.final_chars = len(cleaned)
        report.final_ratio_pct = (report.final_chars / report.original_chars * 100.0) if report.original_chars else 0.0

        logging.info("  Original: %s chars", f"{report.original_chars:,}")
        logging.info("  Cleaned:  %s chars (%.1f%%)", f"{report.final_chars:,}", report.final_ratio_pct)

        chunks = self.chunk_text(cleaned, input_p.name)
        report.chunked = (len(chunks) > 1)
        report.chunks_written = len(chunks)

        if report.chunked:
            logging.info("  Chunked into %d files (max_chunk_size=%d)", len(chunks), self.max_chunk_size)

        written_paths: List[str] = []
        for chunk_text, chunk_name in chunks:
            out_file = output_p / chunk_name
            out_file.write_text(chunk_text, encoding="utf-8")
            written_paths.append(str(out_file))
            report.chunk_files.append(str(out_file))
            logging.info("  Saved: %s (%s chars)", out_file.name, f"{len(chunk_text):,}")

        # Write report next to outputs
        report_dir = output_p / "reports"
        report_dir.mkdir(parents=True, exist_ok=True)
        report_path = report_dir / f"{input_p.stem}.report.json"
        report_path.write_text(json.dumps(asdict(report), ensure_ascii=False, indent=2), encoding="utf-8")
        logging.info("  Report: %s", report_path)

        return written_paths

    def process_directory(self, input_dir: str, output_dir: str) -> List[str]:
        """Process all .txt files in a directory."""
        input_p = Path(input_dir)
        text_files = sorted(input_p.glob("*.txt"))

        logging.info("Found %d text files in %s", len(text_files), input_p)

        all_outputs: List[str] = []
        for tf in text_files:
            outs = self.process_file(str(tf), output_dir)
            all_outputs.extend(outs)

        logging.info("Total output chunk files written: %d", len(all_outputs))
        return all_outputs


# -----------------------------
# CLI
# -----------------------------

def build_arg_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(description="Stage-1 text cleaner: normalize quotes/punctuation, remove boilerplate, chunk, and report.")
    p.add_argument("--input-dir", default="raw_texts", help="Directory containing .txt files (default: raw_texts)")
    p.add_argument("--output-dir", default="cleaned_texts", help="Directory to write cleaned files (default: cleaned_texts)")
    p.add_argument("--max-chunk-size", type=int, default=100000, help="Max characters per chunk (default: 100000)")
    p.add_argument("--header-footer-threshold", type=int, default=5, help="Line repetition threshold for header/footer removal (default: 5)")
    p.add_argument("--log-level", default="INFO", choices=["DEBUG", "INFO", "WARNING", "ERROR"], help="Logging verbosity")
    return p


def main() -> None:
    args = build_arg_parser().parse_args()
    logging.basicConfig(
        level=getattr(logging, args.log_level),
        format="%(levelname)s: %(message)s"
    )

    cleaner = TextCleaner(
        max_chunk_size=args.max_chunk_size,
        header_footer_threshold=args.header_footer_threshold,
    )
    cleaner.process_directory(args.input_dir, args.output_dir)


if __name__ == "__main__":
    main()