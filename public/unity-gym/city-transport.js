/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/5dc2b7566060_WebGL.data.unityweb","bytes":299256907,"sha256":"34e041d24bed8c58582ea85e2db311972e2be2a0a6c20f379780ba0acc3649b4","uncompressedBytes":372446906,"uncompressedSha256":"041bd058bf609f5e556b8ed3b9a02771f932dfa0bf160a06402a8e574762bb3f","parts":[{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"25f367d91543088d3ccae16e215b9099fc595cd47ba9f93a6a587699558bf4cd"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"ceddceb1f1e8f747a8a2e6ac91de985559db12126d95899aa11f9c8928a9c09f"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"7c64483fb41195320bdcb75880348f8deae6b69ebdd9fb9bad2d612a3d479f7f"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"06d924b215786ad936acbb3ad6579e382ed18a5a7949f40b66588d40f750a952"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"6aba556623698acdf70681c14e309c9cbecd682197eda71b8206c964e2934738"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"860deebeed766031fd93ca9da9a0eba016c80a23cdb908a2028b768c6b3dccd9"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"a9e238c89f07df1372168b3198ddc023b9052688784b3819428740a4d74e5f7d"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"dfdf2fd20f0750a7fc22fb2a14c8bac61067cd6871edd2c24bac4f41a6d203bc"},{"path":"Build/5dc2b7566060_WebGL.data.unityweb.part008","bytes":30821451,"sha256":"929a9ed7cbd1d9e685a8788ba382619ef959b8d03b3f25e9cce8a854dad1e7c7"}],"sourceCommit":"5dc2b75660603200ada0888ab979a9e378bcb1ba","assetQualityChanged":true};
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
