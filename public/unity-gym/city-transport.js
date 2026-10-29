/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/b2279bd24f7c_WebGL.data.unityweb","bytes":298989317,"sha256":"7197a00b23757351178f6574df9ee69bc30c154b2978fdd9172427e12c608cb0","uncompressedBytes":372147994,"uncompressedSha256":"23f3530f29faedf893d06348f484e60d5b5b68a81749556980101e5ecb02b74a","parts":[{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"bce7a90c0bfb6c02bccd52592142f884bea1f3b8febf478451c5c1de5a51714d"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"ab27ec53c0e310259a2a18c50a0fde5ca7d35b14e4453bf917b4a2fc26596a63"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"6c0be3b291794601691378640f48ec6bd901c4c1ad423d3539a42b82d7e17259"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"2ff70e7379ef412ee57c3aa73a9de2992015383a715033c3f79f00f692f9199c"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"b1fc7dc4c1658781b29d2e72b8009d2935ed7db9b4376c8ac6aa0df3899d59cf"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"aba64632de125d5a62f67998a0f8b1e5e9e27e1d1d61cf10c0a721aec3256ef2"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"ac3c53b8a99775da0eaf4d1a04031d452d5fa7f086c8679ff94683c13b0450f8"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"db8a0abaa37a0808e15e5154949c2811e657e37ba9b7764f983ed73fc58a1a98"},{"path":"Build/b2279bd24f7c_WebGL.data.unityweb.part008","bytes":30553861,"sha256":"c50c6e9be2700933ae140d42967aa33d2a400f717a07365dacc9f4c86aac8eed"}],"sourceCommit":"b2279bd24f7ce1660160c6fc3af119e9820a32f9","assetQualityChanged":true};
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
