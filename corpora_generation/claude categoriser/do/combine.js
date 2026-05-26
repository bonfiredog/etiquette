/**
 * Combine categorised.json + updated_grammar.json into a flat {category: [words]} JSON.
 *
 * Usage:
 *   node combine.js categorised.json updated_grammar.json output.json
 */

const fs = require("fs");

const [,, categorisedFile, grammarFile, outputFile] = process.argv;
if (!categorisedFile || !grammarFile || !outputFile) {
  console.error("Usage: node combine.js categorised.json updated_grammar.json output.json");
  process.exit(1);
}

const categorised = JSON.parse(fs.readFileSync(categorisedFile, "utf8"));
const grammar = JSON.parse(fs.readFileSync(grammarFile, "utf8"));

// Build a map of category id -> existing examples from the grammar
const result = {};

// Seed with all categories from the grammar (preserves existing examples)
for (const cat of grammar.categories) {
  result[cat.id] = [...(cat.examples || [])];
}

// Add words from categorised.json results
for (const entry of categorised.results || []) {
  if (entry.type === "rejected" || !entry.category) continue;
  if (!result[entry.category]) result[entry.category] = [];
  result[entry.category].push(entry.word);
}

// Deduplicate each category
for (const key of Object.keys(result)) {
  result[key] = [...new Set(result[key])].filter(Boolean).sort();
}

// Remove empty categories
for (const key of Object.keys(result)) {
  if (result[key].length === 0) delete result[key];
}

fs.writeFileSync(outputFile, JSON.stringify(result, null, 2), "utf8");

const totalWords = Object.values(result).reduce((s, arr) => s + arr.length, 0);
console.log(`✅ Done! Written to ${outputFile}`);
console.log(`   ${Object.keys(result).length} categories, ${totalWords} total words`);
