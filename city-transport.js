/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"schema":1,"dataPath":"Build/55fb02f90b27_WebGL.data.unityweb","bytes":339801436,"sha256":"8d504441ce0fe489a87a15ec6d7841037c6e7d886f0f9c245e33705ca5e27ca9","uncompressedBytes":429025775,"uncompressedSha256":"f30da95b3c220cb9a0d3a6d2662345055fcbd36638c2bf767eba78f88b672425","parts":[{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"f103203fa65c38274cae72d169d7b392f515ae66754ee2e612f9a14e8afa5339"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"c9df21632d9d0b68548706f563a0de74a3e2ea55fbbb06afb8e52f0d5eb057cc"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"c9ee344e81b725f53559e7b430ede577c2261a0cc6db82ee531d084141ac7306"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"fe2bc5169ee1ce43aff1b318bd603fd9ad9e93b311c2684af7803b37f3643d2a"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"6a8fee313564bd414eafc513714dbdb1873df3937ffdcc1ed54faa2a7e29a1ef"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"1f78e44754f4d123d76d54e385f0294b181462bec7ffb92a19fe6c815cddf3af"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"4d6b679cbbc9ac5db3086af829b4fb8284a2f7e29d22bb8581ae35c668b8a47d"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"4282878d9d247447728bcfd64e462de53fff75ce0a7436bc28120147e99835d4"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"59135d53225536537dd3bffc6a428b448a84bee14432ecbf1603de1bdac57b5d"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"88cfc79bf0427de9a46a30bd340b6b78139b86b108eaaf65f2a358b10c95fea6"},{"path":"Build/55fb02f90b27_WebGL.data.unityweb.part010","bytes":4257116,"sha256":"6d1543741f78d2c61cf7718b6fa2dca4017e167c449bb29aaf8e90dc5d58f392"}],"sourceCommit":"55fb02f90b27a5d43b8eb61d8e873141c53d5513","compiledDataBytesUnchanged":true,"districtStreaming":false};
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
