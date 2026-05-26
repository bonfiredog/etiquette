"""
Enhanced NLP Extractor with Category Limits
Extract words by POS/sentiment, noun phrases
"""

import spacy
import json
import argparse
from pathlib import Path
from collections import defaultdict
from textblob import TextBlob
import re

# Load spaCy model
nlp = spacy.load("en_core_web_sm")

def get_sentiment(text):
    """Get sentiment polarity using TextBlob"""
    try:
        blob = TextBlob(text)
        polarity = blob.sentiment.polarity
        
        if polarity > 0.1:
            return "positive"
        elif polarity < -0.1:
            return "negative"
        else:
            return "neutral"
    except:
        return "neutral"


def extract_words_and_patterns(text_file, max_per_category=500, max_patterns=500):
    """
    Extract words by POS and sentiment, noun phrases, and sentence patterns
    
    Args:
        text_file: Path to cleaned text file
        max_per_category: Maximum words per POS/sentiment category
        max_patterns: Maximum sentence patterns to extract
    """
    print(f"Processing: {text_file}")
    
    # Read text
    with open(text_file, 'r', encoding='utf-8') as f:
        text = f.read()
    
    # Process with spaCy
    doc = nlp(text)
    
    # Storage for categorized words
    categorized = defaultdict(lambda: defaultdict(set))
    noun_phrases = []
    
    # Extract words by POS and sentiment
    for token in doc:
        # Skip punctuation, spaces, numbers
        if token.is_punct or token.is_space or token.like_num:
            continue
        
        # Skip very short or very long words
        if len(token.text) < 2 or len(token.text) > 20:
            continue
        
        # Get lemma
        lemma = token.lemma_.lower()
        
        # Skip non-alphabetic
        if not lemma.isalpha():
            continue
        
        # Get POS tag
        pos = token.pos_
        
        # Get sentiment for the sentence this word is in
        sentence_text = token.sent.text
        sentiment = get_sentiment(sentence_text)
        
        # Categorize by POS and sentiment
        if pos in ["NOUN", "VERB", "ADJ", "ADV"]:
            categorized[pos][sentiment].add(lemma)
    
    # Extract noun phrases
    for chunk in doc.noun_chunks:
        phrase = chunk.text.lower().strip()
        
        # Filter criteria
        if (2 <= len(phrase.split()) <= 4 and  # 2-4 words
            len(phrase) < 50 and                # Not too long
            phrase.replace(' ', '').isalpha()): # Only letters and spaces
            noun_phrases.append(phrase)
    
    
    # Limit each category
    limited_categorized = {}
    for pos, sentiment_dict in categorized.items():
        limited_categorized[pos] = {}
        for sentiment, words in sentiment_dict.items():
            limited_words = list(words)[:max_per_category]
            if limited_words:  # Only include non-empty categories
                limited_categorized[pos][sentiment] = limited_words
    
    # Limit noun phrases
    limited_noun_phrases = list(set(noun_phrases))[:max_per_category]
    
    return {
        "categorized_words": limited_categorized,
        "noun_phrases": limited_noun_phrases,
    }


def process_directory(input_dir, output_file, max_per_category=500, max_patterns=500):
    """Process all text files in directory"""
    
    input_path = Path(input_dir)
    
    # Aggregate results
    all_categorized = defaultdict(lambda: defaultdict(set))
    all_noun_phrases = set()
    
    # Process each file
    text_files = list(input_path.glob("*.txt"))
    
    print(f"\nFound {len(text_files)} text files")
    print(f"Max per category: {max_per_category}")
    print("="*60)
    
    for text_file in text_files:
        results = extract_words_and_patterns(text_file, max_per_category, max_patterns)
        
        # Aggregate categorized words
        for pos, sentiment_dict in results["categorized_words"].items():
            for sentiment, words in sentiment_dict.items():
                all_categorized[pos][sentiment].update(words)
        
        # Aggregate noun phrases and patterns
        all_noun_phrases.update(results["noun_phrases"])
    
    # Limit aggregated results
    limited_output = {
        "categorized_words": {},
        "noun_phrases": list(all_noun_phrases)[:max_per_category],
    }
    
    for pos, sentiment_dict in all_categorized.items():
        limited_output["categorized_words"][pos] = {}
        for sentiment, words in sentiment_dict.items():
            limited_words = sorted(list(words))[:max_per_category]
            if limited_words:
                limited_output["categorized_words"][pos][sentiment] = limited_words
    
    # Save results
    with open(output_file, 'w', encoding='utf-8') as f:
        json.dump(limited_output, f, indent=2, ensure_ascii=False)
    
    # Print summary
    print("\n" + "="*60)
    print("EXTRACTION COMPLETE")
    print("="*60)
    
    print("\nCategorized Words:")
    for pos, sentiment_dict in limited_output["categorized_words"].items():
        for sentiment, words in sentiment_dict.items():
            print(f"  {pos} ({sentiment}): {len(words)} words")
    
    print(f"\nNoun Phrases: {len(limited_output['noun_phrases'])} phrases")
    
    
    print(f"\nOutput saved to: {output_file}")
    print("="*60)


def main():
    parser = argparse.ArgumentParser(
        description="Extract words, phrases from texts"
    )
    
    parser.add_argument(
        "input_dir",
        help="Directory containing cleaned text files"
    )
    
    parser.add_argument(
        "-o", "--output",
        default="extracted_data.json",
        help="Output JSON file (default: extracted_data.json)"
    )
    
    parser.add_argument(
        "-m", "--max-per-category",
        type=int,
        default=500,
        help="Maximum items per category (default: 500)"
    )
    
    
    args = parser.parse_args()
    
    process_directory(
        args.input_dir,
        args.output,
        args.max_per_category,
    )


if __name__ == "__main__":
    main()
