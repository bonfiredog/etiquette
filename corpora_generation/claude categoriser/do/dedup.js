/**
 * Deduplicates within categories, lowercases all words, and converts
 * verb-category words to their infinitive (present) form.
 *
 * Requires: npm install compromise
 *
 * Usage:
 *   node dedup.js input.json output.json
 */

const fs = require("fs");
const nlp = require("compromise");

const [,, inputFile, outputFile] = process.argv;
if (!inputFile || !outputFile) {
  console.error("Usage: node dedup.js input.json output.json");
  process.exit(1);
}

const grammar = JSON.parse(fs.readFileSync(inputFile, "utf8"));
const result = {};
let totalRemoved = 0;
let totalLemmatised = 0;

function isVerbCategory(categoryKey) {
  return categoryKey.toLowerCase().includes("verb");
}

function toInfinitive(word) {
  const inf = nlp(word).verbs().toInfinitive().out("text").trim();
  return inf || word;
}

for (const [category, words] of Object.entries(grammar)) {
  const isVerb = isVerbCategory(category);
  const seen = new Set();
  const deduped = [];
  let removed = 0;
  let lemmatised = 0;

  for (const word of words) {
    // 1. Lowercase
    let w = word.toLowerCase().trim();

    // 2. Lemmatise if verb category
    if (isVerb) {
      const inf = toInfinitive(w);
      if (inf !== w) {
        lemmatised++;
        w = inf;
      }
    }

    // 3. Deduplicate
    if (!seen.has(w)) {
      seen.add(w);
      deduped.push(w);
    } else {
      removed++;
    }
  }

  if (removed > 0 || lemmatised > 0) {
    const parts = [];
    if (lemmatised > 0) parts.push(`lemmatised ${lemmatised}`);
    if (removed > 0) parts.push(`removed ${removed} duplicate(s)`);
    console.log(`  [${category}]: ${parts.join(", ")}`);
  }

  totalRemoved += removed;
  totalLemmatised += lemmatised;
  result[category] = deduped;
}

fs.writeFileSync(outputFile, JSON.stringify(result, null, 2), "utf8");
console.log(`\nDone! Written to ${outputFile}`);
console.log(`   ${totalLemmatised} verbs lemmatised`);
console.log(`   ${totalRemoved} duplicates removed`);
console.log(`   ${Object.keys(result).length} categories total`);
