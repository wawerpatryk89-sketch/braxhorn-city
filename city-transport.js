/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"schema":1,"dataPath":"Build/45a9555f3a44_WebGL.data.unityweb","bytes":205283084,"sha256":"0b9adc5fbad774a5cccedf24b55b00e872d23e440ae22b7dd103c48398be369d","uncompressedBytes":299609725,"uncompressedSha256":"08eaf618e0f9d8e6e520445abd6ab66bc7e20f57e6b7f3ea56dfa5b0e8e3bfbf","parts":[{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"7258b8b9fbbe55be8c9bca556cf37ce95f936d320f8fc3183a12d429d9f740e0"},{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"b6d10a9d9464592c4fea21f94b3daa8620910c7be3c58c3aafe715bf67a1df3e"},{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"c18223a4fdb63b0573151113c6ece73d83583e7d2578fdc5d4536fef4cbe64dd"},{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"a97d693b53ffb2f48bcac665efe33bab04c42d9ba862fec9850431b29fd37bc0"},{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"a50339a42e2660136d7b123612498f2974951ecb9a4845267a77347137a3f31f"},{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"e2e7410ad4e3e4ddf68dbee4c1352d3643d6947d64e4f8b3cc6183296c7268da"},{"path":"Build/45a9555f3a44_WebGL.data.unityweb.part006","bytes":3956492,"sha256":"aaaf7cf83371f6200144677caa4a9f42e735bf9b40fe26849e1fec0787fb9563"}],"sourceCommit":"45a9555f3a449d2d78165882b465c8bb82267d7f","compiledDataBytesUnchanged":true,"districtStreaming":false};
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
