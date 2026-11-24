/* VK integration is optional: the same build also runs on GitHub Pages. */
(function (root) {
  'use strict';
  const params = new URLSearchParams(root.location.search);
  const isVK = params.has('vk_app_id');
  let unity = null;
  let vkHidden = false;
  const language = params.get('vk_language') === 'ru' ? 'ru' :
    params.has('vk_language') ? 'en' : (navigator.language || 'en').slice(0, 2);
  function notify() {
    if (unity) unity.SendMessage('PlatformRuntime', 'SetPageHidden',
      (document.hidden || vkHidden) ? '1' : '0');
  }
  document.addEventListener('visibilitychange', notify);
  const ready = !isVK ? Promise.resolve() : new Promise((resolve, reject) => {
    const script = document.createElement('script');
    const timer = setTimeout(() => reject(new Error('VK SDK timeout')), 15000);
    script.src = 'vendor/vk-bridge-2.15.0.min.js';
    script.onerror = () => { clearTimeout(timer); reject(new Error('VK SDK unavailable')); };
    script.onload = () => {
      if (!root.vkBridge) { clearTimeout(timer); reject(new Error('VK SDK missing')); return; }
      root.vkBridge.subscribe(event => {
        if (event.detail.type === 'VKWebAppViewHide') vkHidden = true;
        if (event.detail.type === 'VKWebAppViewRestore') vkHidden = false;
        notify();
      });
      root.vkBridge.send('VKWebAppInit').then(() => {
        clearTimeout(timer); resolve();
      }, error => { clearTimeout(timer); reject(error); });
    };
    document.head.appendChild(script);
  });
  root.CRSPlatform = {
    ready,
    language,
    isVK,
    attach(instance) { unity = instance; notify(); }
  };
})(window);
