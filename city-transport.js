/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"schema":1,"dataPath":"Build/62d5484d8050_WebGL.data.unityweb","bytes":411757926,"sha256":"1a59362f5d08aafc2e4970cfc4687fbba0300f6cd825ac17f78ccd14cff49b72","uncompressedBytes":557645202,"uncompressedSha256":"c66d62ded091c5b52ea0785d8bc7ab542c27f15789766f8ecfc9241e8c33bae2","parts":[{"path":"Build/62d5484d8050_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"807ca5cb7238a709d7a548c2eeeb02ebb93a1f14b9d9e588fce13a39123c9bcb"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"03d3f1a972552933706e4b4a58501af6f98381223405f3e155940410ce2697b6"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"46f3e36b300bfc0919ba583d717cae6f34a96c01448954b19b1be992e7b59548"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"49a4c17102fe702241e730cd821c1aebd1d30fc704c53e5394f25c66effabb2d"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"283a5db198968ad19d5e241a2ba5dbe7a8ba18ac984404c28e91ac19710a95a5"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"43abd3e120b81a3c74f046c43faf6876c65e89b92f45e2358b814b5df003ee1d"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"0c085b4777c28c4bedc19708eeaf6648348f6dd2e0e9e1265941f638b510e394"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"cd9169278c6bdbf44fa0e4ee4df89823628d411e83da349ce6d64d5d2d81176d"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"ef5bc9ff5cad23612f8c46d9fbdbcf8811251b85dba6a7ed64164459676b669b"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"135bfc813b6b9887e8e41ff4c6fb61dabdac2a8f87d255d9227fa7010c6c5d81"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part010","bytes":33554432,"sha256":"f5bad20d2d0b326fdf4f345b34a46d81978a1a12d1bdd91232801c3686ab30c8"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part011","bytes":33554432,"sha256":"14dac182c7751c50aef115cb1fd1d08b04408ae82d84a4a2f6dfbe50558a15d9"},{"path":"Build/62d5484d8050_WebGL.data.unityweb.part012","bytes":9104742,"sha256":"89d778cdfc3fc80f5867304382f8417bbf908aacadcaf90daf90dea262572622"}],"sourceCommit":"62d5484d8050c59c4fcbcc4b4111c41d0d2d29ed","compiledDataBytesUnchanged":true,"districtStreaming":false};
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
