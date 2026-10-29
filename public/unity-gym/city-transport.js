/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/027d95f07d7d_WebGL.data.unityweb","bytes":296151226,"sha256":"79b9281bbd9a1dc2d4b0869451329600ea17ec3b6e1b9bf4db87d1b913cba55f","uncompressedBytes":369109269,"uncompressedSha256":"5c2a014bc7674cf2c37d518cef1ca9b791f3157d31c11a93b8e2cb702bd982b6","parts":[{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"7de39094d652fe9d799d2a29650f0e40ebccd060b920c0be281d17c3629a5c79"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"8efa4abca82560c1bf3956a164b0f860cfa6d14a6c56ae403fb1e73fde4f2e38"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"19d35399ce01f680c2146abb8795115d902cdde845f553ebc922bb207e0f7c21"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"0ea72d7acd740b06e94a1fe01ab7547d37a583001e2985547ac3c4ecf21f6f68"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"a09504b8a6dec41f01fede63a340ea7b720bfb5ed33c9bc16446f7507fb38723"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"bf4b6963be76da11b8282bfde43f18c2d3c2f80cc181b54a2e8739d1917665ea"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"3f1c89b4dff3d4424a79adc0a56d655909f3dd8e926de548f23164df03f590b3"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"4d08e6b4d55664fa7cfc4e8c44ef4155f9f3ae80b8c2c717989cc354cc5866a3"},{"path":"Build/027d95f07d7d_WebGL.data.unityweb.part008","bytes":27715770,"sha256":"e4b635ad540e01e08b3d01e58ae254b701b80cb6e71fbd64d3260edc04a33c29"}],"sourceCommit":"027d95f07d7d99b5de0f37e335dfe7ad2a5992ed","assetQualityChanged":true};
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
