import test from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { createSpikeServer } from './server.mjs';

test('static localhost boundary permits spike assets but not traversal, repository files or writes', async () => {
  const server = createSpikeServer();
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  const port = server.address().port;
  const request = (path, method = 'GET', host = `127.0.0.1:${port}`) => new Promise((resolve, reject) => {
    const req = http.request({ hostname: '127.0.0.1', port, path, method, headers: { Host: host } }, res => {
      let body = ''; res.setEncoding('utf8'); res.on('data', value => body += value);
      res.on('end', () => resolve({ status: res.statusCode, body, headers: res.headers }));
    }); req.on('error', reject); req.end();
  });
  try {
    const page = await request('/001-canvas-workspace-comparison/');
    assert.equal(page.status, 200); assert.match(page.headers['content-security-policy'], /connect-src 'self'/);
    assert.equal((await request('/001-canvas-workspace-comparison/model.mjs', 'HEAD')).status, 200);
    for (const path of ['/../AGENTS.md', '/%2e%2e/AGENTS.md', '/%2e%2e%5cAGENTS.md', '/C:/Windows/win.ini', '/001-canvas-workspace-comparison/../../PROJECT.md']) assert.equal((await request(path)).status, 403, path);
    assert.equal((await request('/%ZZ')).status, 400);
    assert.equal((await request('/AGENTS.md')).status, 404);
    assert.equal((await request('/', 'POST')).status, 405);
    assert.equal((await request('/', 'GET', 'untrusted.example')).status, 403);
    assert.equal((await request('/002-sequence-comparison/')).status, 404);
  } finally { await new Promise(resolve => server.close(resolve)); }
});
