/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/8d8ceed0d060_WebGL.data.unityweb","bytes":296378090,"sha256":"798c05f0860eb6b9c029c3772b88a41f458c19d9786fc4e8814f39694f0847d7","uncompressedBytes":369390704,"uncompressedSha256":"c0eb0f504759fa02adf6c8411f65c3024dc2efd5107a40afb43875e51f7ed4e9","parts":[{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"b059ebd26bbeeb079e3562eada8bbb281b73e33681d75ebad548f5101baa4269"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"1346014a47a81b33171bc5978b6d14d1b36914bb5f51a57d3df5ade968fe796b"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"3d21468a08f506dfa20a343968956150217c649a22db1637bcb29e315371c732"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"02b2e6913491a3734eaac601f34f42279cda96c26a1ccefbc41d43819f8fcf5e"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"24dfdbf66d5de923e8de87ed8c1296f4ac2383645e7c2c457abc7d62f824fb12"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"d89305cfcec204b4256076632bb6d7b9b98c2b780f5ed7300c00920e53c9aab0"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"c21b401d039bf99a323db39671229ffa3fa3a5e35d91bf6e616f91b6364cc69b"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"a6a9db3c19a091bcd8508c083de1ae97135eb8d3425a5db7f913ecd6b85ff29c"},{"path":"Build/8d8ceed0d060_WebGL.data.unityweb.part008","bytes":27942634,"sha256":"848e9c527933c325cef0775c2e54a3270046be42a92651a37cdb5fa3e6ac6a10"}],"sourceCommit":"8d8ceed0d0602285d7c1f89628b2df8456e6caa9","assetQualityChanged":true};
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
