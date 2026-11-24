const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const source = fs.readFileSync('Assets/WebGLTemplates/CRS/platform.js', 'utf8');
function setup(search = '', options = {}) {
  let listener, subscriber, script;
  const calls = [];
  const document = { hidden: false,
    addEventListener(name, fn) { listener = fn; },
    createElement() { return {}; },
    head: { appendChild(value) { script = value; } }
  };
  const window = { location: { search }, vkBridge: {
    subscribe(fn) { subscriber = fn; },
    send(method) { calls.push(method); return options.reject ? Promise.reject(new Error('init failed')) : Promise.resolve(); }
  } };
  vm.runInNewContext(source, { window, document, navigator: { language: 'ru-RU' }, URLSearchParams, setTimeout, clearTimeout });
  return { window, document, calls, get script() { return script; },
    hide(hidden) { document.hidden = hidden; listener(); },
    event(type) { subscriber({ detail: { type } }); }
  };
}
test('ordinary browser launches offline without requesting VK SDK', async () => {
  const env = setup();
  await env.window.CRSPlatform.ready;
  assert.equal(env.script, undefined);
  assert.equal(env.window.CRSPlatform.language, 'ru');
});
test('VK init completes before ready; independent hide reasons cannot unmute each other', async () => {
  const env = setup('?vk_app_id=123&vk_language=en');
  env.script.onload();
  await env.window.CRSPlatform.ready;
  assert.deepEqual(env.calls, ['VKWebAppInit']);
  assert.equal(env.window.CRSPlatform.language, 'en');
  const messages = [];
  env.event('VKWebAppViewHide');
  env.window.CRSPlatform.attach({ SendMessage(...args) { messages.push(args); } });
  assert.equal(messages.at(-1)[2], '1');
  env.hide(true);
  env.event('VKWebAppViewRestore');
  assert.equal(messages.at(-1)[2], '1');
  env.hide(false);
  assert.equal(messages.at(-1)[2], '0');
});
test('SDK download failure rejects initialization', async () => {
  const env = setup('?vk_app_id=123');
  const result = assert.rejects(env.window.CRSPlatform.ready, /unavailable/);
  env.script.onerror();
  await result;
});
test('VK initialization rejection reaches the loading screen', async () => {
  const env = setup('?vk_app_id=123', { reject: true });
  const result = assert.rejects(env.window.CRSPlatform.ready, /init failed/);
  env.script.onload();
  await result;
});
