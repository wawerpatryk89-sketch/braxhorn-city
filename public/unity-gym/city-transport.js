/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/df93b3cf4d05_WebGL.data.unityweb","bytes":301300080,"sha256":"ac1d06535cf2d02f92cbb6cdc92c8d8183c91153e7c148dde2333eaadf98f730","uncompressedBytes":375004480,"uncompressedSha256":"7f48e80f623bd9369ba8361e3ba3a502bc0177cdb4cc93c65b1f02f6de3ee76c","parts":[{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"12d92277dd8ae42dc3d7b89f3e90aba3b7d9e9f00c4de9301b4a95387b6bb62a"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"9f8bdf241df0624a3680b9a6b6ddcb41c21d829f2fef8e12ef82367678028a83"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"b3fc492f65f572890d36baa7e99f102416e94452e2a49e6bc1fb5736f10bcd71"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"3a2f00f55e2b1725b8815d77893af8ea9281cf415f2e1fe03f92353dc10a7cdd"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"37b61219036671f17953cf099bfdb4ddb43dd75483c191405ec25af466d62163"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"7c11499fc0de5db4a445b8b371edd64c9d2c1fcc240ba47296afacea83b238ea"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"fd7381c8ee51d62be9c246a8276456789a4e8629cb70b432d74ef56b7f798edc"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"d7ff613fa1793f5e514131430fe4d0f7ebbae5fec8e87d5c9e59b7cb1204a58b"},{"path":"Build/df93b3cf4d05_WebGL.data.unityweb.part008","bytes":32864624,"sha256":"8e028d775e064cfaddf629906296acfd71469fcbc35b6259cdf57a69e1096c18"}],"sourceCommit":"df93b3cf4d0538551eab0789c882ffd9da77c97a","assetQualityChanged":true};
  const nativeFetch = window.fetch.bind(window);
  const dataUrl = new URL(manifest.dataPath, document.baseURI).href;
  const inputChunkLimit = 64 * 1024;
  const revision = 'ios-stream-01';
  function reportError(error) {
    const text = error instanceof Error ? error.message : String(error);
    window.dispatchEvent(new CustomEvent('brax-city-load-error', {detail: text}));
  }
  window.BraxCityFetch = async function(input, options = {}) {
    const requestUrl = input instanceof URL ? input.href : typeof input === 'string' ? input : input.url;
    const url = new URL(requestUrl, document.baseURI).href;
    if (url !== dataUrl) return nativeFetch(input, options);
    // The old Unity JS inflater retains compressed input, all inflated chunks,
    // and a merged output buffer at the same time. Do not invoke it for this
    // large city archive: send already-decompressed bytes to Unity instead.
    if (typeof DecompressionStream !== 'function') {
      const error = new Error('Ta przeglądarka nie obsługuje oszczędnego ładowania miasta. Zaktualizuj przeglądarkę lub system.');
      reportError(error); throw error;
    }
    const abort = new AbortController();
    const externalSignal = options.signal || (typeof input === 'object' ? input.signal : null);
    const abortExternally = () => abort.abort(externalSignal.reason);
    if (externalSignal) {
      if (externalSignal.aborted) abortExternally();
      else externalSignal.addEventListener('abort', abortExternally, {once: true});
    }
    let part = 0, reader = null, pending = null, pendingOffset = 0;
    let received = 0, partReceived = 0, currentPart = null;
    const stats = window.BraxCityLoadingStats = {
      revision, mode: 'native-gzip-stream', sourceCommit: manifest.sourceCommit,
      compressedBytes: manifest.bytes, expandedBytes: manifest.uncompressedBytes,
      receivedBytes: 0, decodedBytes: 0, maxInputChunk: 0, complete: false,
      originalAssetsChanged: true, fullCompressedBufferAllocated: false
    };
    function cleanup() {
      if (externalSignal) externalSignal.removeEventListener('abort', abortExternally);
    }
    async function cancel(reason) {
      abort.abort(reason);
      pending = null;
      if (reader) { try { await reader.cancel(reason); } finally { reader.releaseLock(); reader = null; } }
      cleanup();
    }
    const compressed = new ReadableStream({
      async pull(controller) {
        try {
          while (true) {
            if (abort.signal.aborted) throw new DOMException('Ładowanie anulowane', 'AbortError');
            if (pending) {
              const end = Math.min(pendingOffset + inputChunkLimit, pending.byteLength);
              const slice = pending.subarray(pendingOffset, end);
              pendingOffset = end;
              if (end === pending.byteLength) { pending = null; pendingOffset = 0; }
              stats.maxInputChunk = Math.max(stats.maxInputChunk, slice.byteLength);
              controller.enqueue(slice); return;
            }
            if (!reader) {
              if (part === manifest.parts.length) {
                if (received !== manifest.bytes) throw new Error('Niekompletne dane miasta.');
                controller.close(); return;
              }
              currentPart = manifest.parts[part++]; partReceived = 0;
              const response = await nativeFetch(new URL(currentPart.path, document.baseURI).href, {
                method: 'GET', signal: abort.signal, cache: 'default'
              });
              if (!response.ok || !response.body) throw new Error('Nie pobrano części miasta: HTTP ' + response.status);
              reader = response.body.getReader();
            }
            const item = await reader.read();
            if (item.done) {
              reader.releaseLock(); reader = null;
              if (partReceived !== currentPart.bytes) throw new Error('Niepełna część danych miasta.');
              continue;
            }
            received += item.value.byteLength; partReceived += item.value.byteLength;
            if (received > manifest.bytes || partReceived > currentPart.bytes) throw new Error('Nieprawidłowy rozmiar danych miasta.');
            stats.receivedBytes = received;
            pending = item.value; pendingOffset = 0;
          }
        } catch (error) {
          await cancel(error).catch(() => {});
          reportError(error); controller.error(error);
        }
      },
      cancel
    });
    const expanded = compressed.pipeThrough(new DecompressionStream('gzip'));
    const decodedReader = expanded.getReader();
    // Validate the decoded length without concatenating or cloning the stream.
    const checked = new ReadableStream({
      async pull(controller) {
        try {
          const item = await decodedReader.read();
          if (item.done) {
            decodedReader.releaseLock(); cleanup();
            if (stats.decodedBytes !== manifest.uncompressedBytes) throw new Error('Rozmiar rozpakowanego miasta jest nieprawidłowy.');
            stats.complete = true; controller.close(); return;
          }
          stats.decodedBytes += item.value.byteLength;
          if (stats.decodedBytes > manifest.uncompressedBytes) throw new Error('Przekroczony rozmiar rozpakowanych danych.');
          controller.enqueue(item.value);
        } catch (error) {
          abort.abort(error); cleanup(); reportError(error); controller.error(error);
        }
      },
      async cancel(reason) {
        try { await decodedReader.cancel(reason); } finally { abort.abort(reason); cleanup(); }
      }
    });
    // Actual decoded length lets Unity allocate ONE exact-sized buffer.
    return new Response(checked, {status: 200, headers: {
      'Content-Type': 'application/octet-stream',
      'Content-Length': String(manifest.uncompressedBytes),
      'Cache-Control': 'no-store',
      'X-Braxhorn-Transport': revision
    }});
  };
})();
