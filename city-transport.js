/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/140407203586_WebGL.data.unityweb","bytes":286816719,"sha256":"09311164fc7fdc4889feb0f5ba69119579aca0e0ce4c3baafb7e853caa06a3b7","parts":[{"path":"Build/140407203586_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"730846e357af5a577a928d5e309f0fc5e5b9fcffea4aaf1912ee871654592025"},{"path":"Build/140407203586_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"44deed7149342b2bf9753c378622691746b32b34306878ecffac126039debe0e"},{"path":"Build/140407203586_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"894ebb6ffb5a32ba1f3f16a1c9d081b78ead02528443ff542e898379499a09b5"},{"path":"Build/140407203586_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"8c4e1ee48d3a414c174b38a2097e6fbc12b264e9e72eb5c50e5d70114610819c"},{"path":"Build/140407203586_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"18a9fafeaed4fc7eb67fa23a58cb8cae07d2e405f9e4950dc1bbb2fadd8b542c"},{"path":"Build/140407203586_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"ea8be7e7de7f8e542ff7e84dadcc82f746f7e12fe55029fdf32322a1144db182"},{"path":"Build/140407203586_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"ceb2f3b588f30d43d16f75d5df2b79e6912dc2703ed6242e5ec10cfe526d5c08"},{"path":"Build/140407203586_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"14c6b4b51956fba4907f0b761be98fb471246492bf380d90f9e44a4eda26a9f2"},{"path":"Build/140407203586_WebGL.data.unityweb.part008","bytes":18381263,"sha256":"6a75c7dfa3ae27f5942ecc18c83b2db16ad9016645b518af01a33e8f0d22f019"}],"sourceCommit":"140407203586a6c150faf6355ebd4c1a2faa55c7","uncompressedBytes":358921067};
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
      originalAssetsChanged: false, fullCompressedBufferAllocated: false
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
