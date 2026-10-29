/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/25fbb9ff9274_WebGL.data.unityweb","bytes":301332483,"sha256":"de92616f01259bd8c2b4c0c949bed661cba0365091927571b42829a804e4c1e2","uncompressedBytes":374939877,"uncompressedSha256":"0ec2431f8bcee0fdd85eb7211fdb3aae73ee8d9217141a8e33415290afa3198c","parts":[{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"c6a4b8826d04e70a6f3b26db2c01e906fec56b403979787e0a6368c5745c099f"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"83c8cc86e6fc9f4721b87553c14a6410f488bb5b52c15440d2c3648d527af305"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"3f121d89057d423418c3315053457d1a02344eb79dc72565bc4ad10c08ca3094"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"f1a135f775434d0db42c208bcde81b5d42079cf1f8472a78bf553293c6770720"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"2ea8f10c1aa950a2d983f127d7f4275b21d5637a8203c3b8e59eabd4df2f9eb6"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"567f08257f15d185b4b9c2fcbc5000bb5536bf2c206d77e645348f207c270947"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"8e501b08dc8b476a829ed0f9b52b2f87a2524ec7235a5cdff704ce119d246d94"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"fe8e52cb2477d867743e7209e0e59a960f3f32b815330b31631c1b53ecf47574"},{"path":"Build/25fbb9ff9274_WebGL.data.unityweb.part008","bytes":32897027,"sha256":"f4e81d3f26856340c6cc0972b349179e31ac005e455227b4a659e188ee343095"}],"sourceCommit":"25fbb9ff9274c3ac0575e51000ca08c4d069c5ec","assetQualityChanged":true};
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
