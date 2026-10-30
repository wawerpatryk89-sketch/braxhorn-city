/* Braxhorn: bounded native gzip streaming. Original Unity asset bytes are unchanged. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/19c71c167d44_WebGL.data.unityweb","bytes":286821275,"sha256":"f5cf692beb441d7a69f0d41361475533def4f2c02e6e55fddc3ad1b89fd0fe06","parts":[{"path":"Build/19c71c167d44_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"7ae445763fb45c70274d0661051c048cdba1e4b13303b795b4a1dcd1c248251d"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"f24cec6fc9c7fa0be31135fa3bfbc0d8a80dbeee152518456b93ee746e28a5b7"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"d522edf3775471c67727fde6ace5469a9ec31c9490f1788ba7f61c8d53cb368d"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"6961721bb53e72f063bcbf74d14a244601b6c29cde52e4cb174be0ab92a09d0f"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"e4ed965491a50e5db12714e938969f54e67eb01ef1bec6291259ea810bd9a10c"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"2b775d4d19b20ff903c05e50664e07b2af3890cc6564950369adaa6b786adac2"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"26c0b3045a315453609832a566b94d6739020dcbb8b8fd6fbd3be01b9ab7c7b4"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"fba9ab335cdc4d2d60637c2fed76000e532c01d56ac624d26af0b9da36e08223"},{"path":"Build/19c71c167d44_WebGL.data.unityweb.part008","bytes":18385819,"sha256":"a61ca423b0dbfeaae420dcba2e808e1afe7fa18ab2cd3444e921ba958e8c9c61"}],"sourceCommit":"19c71c167d4426523a251b94c3aeabbe9664aad5","assetQualityChanged":false,"uncompressedBytes":358936591};
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
