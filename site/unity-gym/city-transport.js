/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/05e1a5339c75_WebGL.data.unityweb","bytes":605965606,"sha256":"fa8ea7dfc1c3c8924920a240f0d69a05b3840e087b691edd63b244dbb9fa7f61","uncompressedBytes":784906070,"uncompressedSha256":"2e024e52ed79dd86ac78b83b67d2dfb8dc25e9dff01f577a200160cb6e3a5a25","parts":[{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"0af72d091e6a197fedab728372aa84472274530b3303e2c6001bac69585c5e17"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"66db8b18b616c289db4b9f89c2d20538e9a24419325edcb893fb07abf2b05a0f"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"f1015347a0b6a826dd24c653655ca382641a426e28500c2240faaf3b746e4cd2"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"545f40b0730e556fae09891d20794698b8a3bcf55259033edc01c3206d216932"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"66bc5707210e1d778f8ceb424cb6a61f749dbc67d91f20e2a3f1a800935dea86"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"25e878e815511bf672f0d1ef76895c41bd14a399cc79f2dae6b2ae8cad3fe290"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"40cf983e5feda4aa55db522bdae5d062a24285d0a69fb759ea7ff2a8427bbedb"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"2856694b524f8cef2d26e28409ec2aeada4424bf1e09ffb72f08a56c01af6ada"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"4b494b3ac51a6e057285ede5dff9b954b8982f11a4704660c573c8d5c1363a4c"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"b02bb7fda22cb35343a98dd5f312a7791f4b5b60e434ef69106db3da1cadb090"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part010","bytes":33554432,"sha256":"90a7316dfb982ad2b284d838dd93105a44e79fa2b92648619cf209d41cb3dea3"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part011","bytes":33554432,"sha256":"6f9f082dd9cc2f507f61a97b20d75d77de0688a97a66202c2e3fed457f6ec876"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part012","bytes":33554432,"sha256":"c5d1ac3070aabca6466a767c2acd36910a0dfee09e14db9a840ffa79951bf185"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part013","bytes":33554432,"sha256":"398c284186a707a9ff6b6f155bae6e54112a4ef220c68ce989c0ea4f569a957e"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part014","bytes":33554432,"sha256":"362355e39919b76e9a5879dcc6465cae316cc28c6d00f9b6f003bbbfa0fdd2a2"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part015","bytes":33554432,"sha256":"d69041575ab35ba19f9b45f8a1b53786c6dedbb9a62f963653e99cd439a9dd8b"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part016","bytes":33554432,"sha256":"5506791b87a7a9e127c9929189c33d47caec97f0aff58bcb77e3d6994d68acf3"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part017","bytes":33554432,"sha256":"47b9e123fcaaa5bba3e973408ee53a9489a6176c63a87d1763b0cf44537f6720"},{"path":"Build/05e1a5339c75_WebGL.data.unityweb.part018","bytes":1985830,"sha256":"e34f750fe097d96b8ee83c30877468274231bf8cae4d7ac3f15e4b6b41ce7244"}],"sourceCommit":"05e1a5339c75026ff5ad8119117361dc537accf5","assetQualityChanged":true};
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
