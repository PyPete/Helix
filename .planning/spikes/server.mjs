import http from 'node:http';
import { readFile, realpath, stat } from 'node:fs/promises';
import { dirname, resolve, relative, isAbsolute, extname, sep } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = dirname(fileURLToPath(import.meta.url));
const mime = { '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.mjs': 'text/javascript; charset=utf-8', '.md': 'text/plain; charset=utf-8', '.json': 'application/json; charset=utf-8' };
export function createSpikeServer() {
  return http.createServer(async (req, res) => {
    const finish = (status, text = '') => { res.writeHead(status, { 'Content-Type': 'text/plain; charset=utf-8' }); res.end(req.method === 'HEAD' ? '' : text); };
    res.setHeader('X-Content-Type-Options', 'nosniff');
    res.setHeader('Cache-Control', 'no-store');
    res.setHeader('Referrer-Policy', 'no-referrer');
    res.setHeader('Content-Security-Policy', "default-src 'self'; script-src 'self'; style-src 'self'; connect-src 'self'; img-src 'self' data:; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'");
    if (!/^(127\.0\.0\.1|localhost)(:\d+)?$/.test(req.headers.host ?? '')) return finish(403, 'Localhost host required.');
    if (!['GET', 'HEAD'].includes(req.method)) return finish(405, 'Read-only static server.');
    try {
      // Inspect raw decoded path BEFORE URL normalization can discard traversal segments.
      const path = decodeURIComponent((req.url ?? '/').split('?')[0]);
      if (!path.startsWith('/') || /[\\\u0000:]/.test(path) || path.split('/').some(part => part === '..' || part === '.')) return finish(403, 'Path denied.');
      let candidate = resolve(root, '.' + path);
      const inside = value => { const rel = relative(root, value); return !isAbsolute(rel) && rel !== '..' && !rel.startsWith('..' + sep); };
      if (!inside(candidate)) return finish(403, 'Path denied.');
      candidate = await realpath(candidate);
      if (!inside(candidate)) return finish(403, 'Symlink outside spike root denied.');
      if ((await stat(candidate)).isDirectory()) {
        candidate = await realpath(resolve(candidate, 'index.html'));
        if (!inside(candidate)) return finish(403, 'Path denied.');
      }
      const contentType = mime[extname(candidate)];
      if (!contentType) return finish(403, 'File type denied.');
      const body = await readFile(candidate);
      res.writeHead(200, { 'Content-Type': contentType }); res.end(req.method === 'HEAD' ? undefined : body);
    } catch (error) {
      if (error instanceof URIError) return finish(400, 'Malformed path.');
      if (['ENOENT', 'ENOTDIR'].includes(error.code)) return finish(404, 'Spike file not found.');
      finish(500, 'Unable to serve spike file.');
    }
  });
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const port = Number(process.argv[2] ?? process.env.PORT ?? 4317);
  if (!Number.isInteger(port) || port < 1 || port > 65535) throw new Error('Port must be an integer from 1 to 65535.');
  const server = createSpikeServer();
  server.on('error', error => { console.error(error.message); process.exitCode = 1; });
  server.listen(port, '127.0.0.1', () => console.log(`Helix simulation spikes: http://127.0.0.1:${port}/ (static, localhost only)`));
}
