/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"schema":1,"dataPath":"Build/48d81f1f998b_WebGL.data.unityweb","bytes":411968267,"sha256":"0a1121591e20d5a1a7f9ada350ad4364fd0d58fee7a41e7a4e6b5ca952ae40a0","uncompressedBytes":558052588,"uncompressedSha256":"9077384c35abebad7b75c3a787b422dac7b6bbf0c0f2bf93535fcb924fdf9a60","parts":[{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"01367c1765cc238eaaacf5b5ab586ab06382ea7397b9d1a36ef405550763488e"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"76f8ee504b8d4283217e40955e0e8eb4cfed52733ec3bc2c12b68fc9c95441e9"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"920443b587f1ce76e8082bb12d6a10320a6479ca2c362859520afa3a8e5037c1"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"1f5c737b13c8961e148b69cc57e34e2f6a7672a5b9ad0fff2e811f6dcd4b4dd7"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"713793aa37469239e039eec3541afc2c73ea2a129ebd0ea84969587fa23b3d3c"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"5251911a97eba36933982fffea28eae2afc8fa8870fe161656232266751f0623"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"89ea86e137f1cca0bc8aac675c87668f856fba087b071a16fca05833a0bd41e1"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"adde8ccbb356d71be99f81d06274e531e0a32315d2b0f98fa0e170e67ac6b414"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"3179e08c3c95ad8c37a19c5baf931f3f7246e277c1c9599e5f81bbbe9ebe28af"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"66d718b8e209e089a93b23c49762517131eb9b66d8975991a131b46dd9365c12"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part010","bytes":33554432,"sha256":"610818e6f75e0cebd91ae961525d01b13997ea5b89404250ab33e8b34b057e6f"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part011","bytes":33554432,"sha256":"053b5ea2433e2cc2fc8db60c863eef6631aa678fd48f75fb55cc0d16f894a1a2"},{"path":"Build/48d81f1f998b_WebGL.data.unityweb.part012","bytes":9315083,"sha256":"8d4451e24e376ddfd6a616036c594a41d0f3df94704edb40e8bdd3812afcbc18"}],"sourceCommit":"48d81f1f998b9884709e74afe10c3f7ebf0b58ac","compiledDataBytesUnchanged":true,"districtStreaming":false};
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
