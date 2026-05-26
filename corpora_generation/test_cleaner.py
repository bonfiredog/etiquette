"""
Text Cleaning Pipeline for NLP Processing
Cleans and prepares book texts for grammar extraction
"""

import re
import os
from pathlib import Path
from typing import List, Tuple


class TextCleaner:
    def __init__(self, max_chunk_size: int = 100000):
        """
        Initialize text cleaner
        
        Args:
            max_chunk_size: Maximum characters per chunk (default 100k)
        """
        self.max_chunk_size = max_chunk_size
        
        # Common front/back matter patterns
        self.frontmatter_patterns = [
            r'table of contents.*?(?=chapter|prologue|\n\n[A-Z])',
            r'copyright.*?(?=chapter|prologue|\n\n[A-Z])',
            r'dedication.*?(?=chapter|prologue|\n\n[A-Z])',
            r'acknowledgments?.*?(?=chapter|prologue|\n\n[A-Z])',
            r'preface.*?(?=chapter|prologue|\n\n[A-Z])',
            r'introduction.*?(?=chapter|prologue|\n\n[A-Z])',
        ]
        
        self.backmatter_patterns = [
            r'(?:the end|epilogue).*?(?:about the author|acknowledgments?|bibliography|index).*',
            r'about the author.*',
            r'also by.*',
            r'bibliography.*',
            r'index\s*$',
        ]
        
    def read_file(self, filepath: str) -> str:
        """Read file with multiple encoding attempts"""
        encodings = ['utf-8', 'latin-1', 'cp1252', 'iso-8859-1']
        
        for encoding in encodings:
            try:
                with open(filepath, 'r', encoding=encoding) as f:
                    return f.read()
            except UnicodeDecodeError:
                continue
        
        raise ValueError(f"Could not decode {filepath} with any common encoding")
    
    def remove_page_numbers(self, text: str) -> str:
        """Remove standalone page numbers"""
        # Remove lines that are just numbers
        text = re.sub(r'^\s*\d+\s*$', '', text, flags=re.MULTILINE)
        
        # Remove page number patterns like "- 42 -" or "Page 42"
        text = re.sub(r'[-–—]\s*\d+\s*[-–—]', '', text)
        text = re.sub(r'\bpage\s+\d+\b', '', text, flags=re.IGNORECASE)
        
        return text
    
    def remove_headers_footers(self, text: str) -> str:
        """Remove repeated headers/footers (heuristic approach)"""
        lines = text.split('\n')
        
        # Find lines that repeat frequently (likely headers/footers)
        line_counts = {}
        for line in lines:
            stripped = line.strip()
            if stripped and len(stripped) < 100:  # Headers/footers are usually short
                line_counts[stripped] = line_counts.get(stripped, 0) + 1
        
        # Remove lines that appear more than 5 times (likely headers/footers)
        threshold = 5
        repeated_lines = {line for line, count in line_counts.items() if count > threshold}
        
        cleaned_lines = [line for line in lines if line.strip() not in repeated_lines]
        
        return '\n'.join(cleaned_lines)
    
    def remove_frontmatter(self, text: str) -> str:
        """Remove front matter using common patterns"""
        text_lower = text.lower()
        
        # Find the start of main content (first chapter or prologue)
        main_content_start = None
        patterns = [
            r'\bchapter\s+(?:one|1|i)\b',
            r'\bprologue\b',
            r'\bpart\s+(?:one|1|i)\b',
        ]
        
        for pattern in patterns:
            match = re.search(pattern, text_lower)
            if match:
                main_content_start = match.start()
                break
        
        if main_content_start and main_content_start < len(text) * 0.2:  # Within first 20%
            return text[main_content_start:]
        
        return text
    
    def remove_backmatter(self, text: str) -> str:
        """Remove back matter using common patterns"""
        text_lower = text.lower()
        
        # Find common end markers
        end_markers = [
            r'\b(?:the\s+)?end\s*$',
            r'\babout the author\b',
            r'\backnowledgments?\b',
            r'\bbibliography\b',
        ]
        
        earliest_end = len(text)
        
        for pattern in end_markers:
            # Look for pattern in last 20% of text
            search_start = int(len(text) * 0.8)
            match = re.search(pattern, text_lower[search_start:])
            if match:
                actual_pos = search_start + match.start()
                earliest_end = min(earliest_end, actual_pos)
        
        if earliest_end < len(text):
            return text[:earliest_end]
        
        return text
    
    def clean_text(self, text: str) -> str:
        """Apply all cleaning steps"""
        # Convert to lowercase
        text = text.lower()
        
        # Remove non-English characters and numbers
        # Keep letters, spaces, basic punctuation, newlines
        text = re.sub(r'[^a-z\s\.\,\!\?\;\:\-\'\"\n]', ' ', text)
        
        # Remove page numbers
        text = self.remove_page_numbers(text)
        
        # Remove headers/footers
        text = self.remove_headers_footers(text)
        
        # Remove front and back matter
        text = self.remove_frontmatter(text)
        text = self.remove_backmatter(text)
        
        # Normalize whitespace
        text = re.sub(r' +', ' ', text)  # Multiple spaces to single
        text = re.sub(r'\n\s*\n\s*\n+', '\n\n', text)  # Multiple newlines to double
        text = text.strip()
        
        return text
    
    def chunk_text(self, text: str, filename: str) -> List[Tuple[str, str]]:
        """
        Split large texts into chunks
        
        Returns:
            List of (chunk_text, chunk_filename) tuples
        """
        if len(text) <= self.max_chunk_size:
            return [(text, filename)]
        
        chunks = []
        base_name = Path(filename).stem
        
        # Split on paragraph boundaries
        paragraphs = text.split('\n\n')
        current_chunk = []
        current_size = 0
        chunk_num = 1
        
        for para in paragraphs:
            para_size = len(para)
            
            if current_size + para_size > self.max_chunk_size and current_chunk:
                # Save current chunk
                chunk_text = '\n\n'.join(current_chunk)
                chunk_name = f"{base_name}_chunk{chunk_num}.txt"
                chunks.append((chunk_text, chunk_name))
                
                # Start new chunk
                current_chunk = [para]
                current_size = para_size
                chunk_num += 1
            else:
                current_chunk.append(para)
                current_size += para_size
        
        # Add final chunk
        if current_chunk:
            chunk_text = '\n\n'.join(current_chunk)
            chunk_name = f"{base_name}_chunk{chunk_num}.txt"
            chunks.append((chunk_text, chunk_name))
        
        return chunks
    
    def process_file(self, input_path: str, output_dir: str) -> List[str]:
        """
        Process a single file through the cleaning pipeline
        
        Returns:
            List of output file paths
        """
        print(f"Processing: {input_path}")
        
        # Read file
        text = self.read_file(input_path)
        original_length = len(text)
        
        # Clean text
        text = self.clean_text(text)
        cleaned_length = len(text)
        
        print(f"  Original: {original_length:,} chars")
        print(f"  Cleaned: {cleaned_length:,} chars ({cleaned_length/original_length*100:.1f}%)")
        
        # Chunk if needed
        filename = Path(input_path).name
        chunks = self.chunk_text(text, filename)
        
        if len(chunks) > 1:
            print(f"  Split into {len(chunks)} chunks")
        
        # Save chunks
        output_paths = []
        os.makedirs(output_dir, exist_ok=True)
        
        for chunk_text, chunk_name in chunks:
            output_path = os.path.join(output_dir, chunk_name)
            with open(output_path, 'w', encoding='utf-8') as f:
                f.write(chunk_text)
            output_paths.append(output_path)
            print(f"  Saved: {output_path} ({len(chunk_text):,} chars)")
        
        return output_paths
    
    def process_directory(self, input_dir: str, output_dir: str) -> List[str]:
        """
        Process all text files in a directory
        
        Returns:
            List of all output file paths
        """
        all_outputs = []
        
        # Find all text files
        input_path = Path(input_dir)
        text_files = list(input_path.glob('*.txt'))
        
        print(f"Found {len(text_files)} text files to process\n")
        
        for text_file in text_files:
            outputs = self.process_file(str(text_file), output_dir)
            all_outputs.extend(outputs)
            print()
        
        print(f"Total output files: {len(all_outputs)}")
        return all_outputs


# Example usage
if __name__ == "__main__":
    # Initialize cleaner
    cleaner = TextCleaner(max_chunk_size=100000)  # 100k chars per chunk
    
    # Process single file
    # cleaner.process_file('input/book.txt', 'cleaned_output')
    
    # Or process entire directory
    cleaner.process_directory('raw_texts', 'cleaned_texts')
