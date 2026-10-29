/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"schema":1,"dataPath":"Build/7a7e0a031af5_WebGL.data.unityweb","bytes":411201395,"sha256":"639ae3a1be657afda35e8d36c7fe3e724ff72427e43ca74f26439196aa337392","uncompressedBytes":556975905,"uncompressedSha256":"78555b12698c83d0d47cf8a08872e0634e247090ddbbb4c26cb5f198bc613908","parts":[{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"e3177bedb030a6d61163aaaab45a2281b3840067813ab3f06cadd775a68a70c7"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"9d3bd5f14ac9d5a05073a44bc72744e72ed33db3db5c12661e65fb6c3f042a1f"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"28cf196460f64c2246b3bbed0c08dea574b13238e0f780c13288637a3d781ea3"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"3a7e43e4ac416377cfc35f6222d350ef61b3cd3dc18013d78485d1e54b4cd996"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"f1594af90a66edd40470eb0baad541a4891a8cb55f32b24bfc900dd7679c78fa"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"3f29edf214df7786b9f565ef4d98bde717ef4068dcca2956c93a767f484b1395"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"4d55fa480fc829b17375c92ece691c7d1085c8c3b3254350dba6c98f8d2895ed"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"3c45b3cd0053922096a5350a0f1c81cbc4db026a69e43d33f6c7c7a7fa7b3c8d"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"e3f720f0189238cc925a3be416f79232ef8b01bc6e988c3cf526649dea0ea13b"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"b36ab7cbc564adc3c6e5d8031b1024b274b736f839f3db61cb19368f3fa2272a"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part010","bytes":33554432,"sha256":"822a15e62f9c5dc3ee43286ae86f3cbc6c71a70ed472360ba14220814a1eac34"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part011","bytes":33554432,"sha256":"0cf376636a43cf53b41335368103902f1fc5471c9e206ad8525c7487c76b0ab7"},{"path":"Build/7a7e0a031af5_WebGL.data.unityweb.part012","bytes":8548211,"sha256":"4c7854acefd6378775b1e49efa5cc2a26491a785f4b2a6488cbd839f2cf85f9d"}],"sourceCommit":"7a7e0a031af51b7acfe17eff9f1212e680dc27dc","compiledDataBytesUnchanged":true,"districtStreaming":false};
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
