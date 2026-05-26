/**
 * Anthropic API Proxy Server
 * Runs locally to allow the HTML file to call the Anthropic API from a browser.
 *
 * Usage:
 *   ANTHROPIC_API_KEY=sk-ant-... node server.js
 *
 * Then open http://localhost:3131/word_categoriser_pipeline.html in your browser.
 * Place word_categoriser_pipeline.html in the same folder as this file.
 */

const http = require("http");
const https = require("https");
const fs = require("fs");
const path = require("path");

const PORT = 3131;
const API_KEY = process.env.ANTHROPIC_API_KEY || "";

if (!API_KEY) {
  console.error("❌  Missing ANTHROPIC_API_KEY environment variable.");
  console.error(
    "    Run as: ANTHROPIC_API_KEY=sk-ant-... node server.js  (Mac/Linux)"
  );
  console.error(
    "         or: set ANTHROPIC_API_KEY=sk-ant-...  then  node server.js  (Windows CMD)"
  );
  process.exit(1);
}

const MIME = {
  ".html": "text/html",
  ".js": "text/javascript",
  ".css": "text/css",
  ".json": "application/json",
};

const server = http.createServer((req, res) => {
  // ── CORS headers (allow the browser page to talk to this proxy) ──
  res.setHeader("Access-Control-Allow-Origin", "*");
  res.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  res.setHeader("Access-Control-Allow-Headers", "Content-Type");

  if (req.method === "OPTIONS") {
    res.writeHead(204);
    res.end();
    return;
  }

  // ── Proxy: POST /v1/messages → Anthropic API ──────────────────────
  if (req.method === "POST" && req.url === "/v1/messages") {
    let body = "";
    req.on("data", (chunk) => (body += chunk));
    req.on("end", () => {
      const options = {
        hostname: "api.anthropic.com",
        path: "/v1/messages",
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "x-api-key": API_KEY,
          "anthropic-version": "2023-06-01",
          "Content-Length": Buffer.byteLength(body),
        },
      };

      const apiReq = https.request(options, (apiRes) => {
        // Log any error responses so we can diagnose them
        if (apiRes.statusCode >= 400) {
          let errorBody = "";
          apiRes.on("data", chunk => errorBody += chunk);
          apiRes.on("end", () => {
            const retryAfter   = apiRes.headers["retry-after"] || "(none)";
            const tokRemaining = apiRes.headers["anthropic-ratelimit-tokens-remaining"] || "?";
            const reqRemaining = apiRes.headers["anthropic-ratelimit-requests-remaining"] || "?";
            console.error(`\n⚠️  HTTP ${apiRes.statusCode} from Anthropic`);
            console.error(`   retry-after: ${retryAfter}s | tokens remaining: ${tokRemaining} | requests remaining: ${reqRemaining}`);
            console.error(`   body: ${errorBody.slice(0, 500)}\n`);
            const fwd = Object.assign({}, apiRes.headers, {
              "access-control-allow-origin": "*",
              "access-control-expose-headers": "retry-after, anthropic-ratelimit-requests-remaining, anthropic-ratelimit-tokens-remaining, anthropic-ratelimit-tokens-reset",
            });
            res.writeHead(apiRes.statusCode, fwd);
            res.end(errorBody);
          });
        } else {
          const fwd = Object.assign({}, apiRes.headers, {
            "access-control-allow-origin": "*",
            "access-control-expose-headers": "retry-after, anthropic-ratelimit-requests-remaining, anthropic-ratelimit-tokens-remaining, anthropic-ratelimit-tokens-reset",
          });
          res.writeHead(apiRes.statusCode, fwd);
          apiRes.pipe(res);
        }
      });

      apiReq.on("error", (e) => {
        console.error("API request error:", e);
        res.writeHead(502);
        res.end(JSON.stringify({ error: e.message }));
      });

      apiReq.write(body);
      apiReq.end();
    });
    return;
  }

  // ── Static file server: serve HTML (and anything else) from this folder ──
  let filePath = path.join(
    __dirname,
    req.url === "/" ? "/word_categoriser_pipeline.html" : req.url
  );
  const ext = path.extname(filePath);

  fs.readFile(filePath, (err, data) => {
    if (err) {
      res.writeHead(404);
      res.end("Not found: " + req.url);
      return;
    }
    res.writeHead(200, { "Content-Type": MIME[ext] || "text/plain", "Cache-Control": "no-store" });
    res.end(data);
  });
});

server.listen(PORT, () => {
  console.log(`\n✅  Proxy running at http://localhost:${PORT}`);
  console.log(
    `    Open → http://localhost:${PORT}/word_categoriser_pipeline.html\n`
  );
});
