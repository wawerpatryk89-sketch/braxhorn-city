/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/5623047e2713_WebGL.data.unityweb","bytes":734926024,"sha256":"f5a6864b744c938485b0dcbc1dfec75efaad2502d3a1f9b9c0211123d03d4cd0","uncompressedBytes":880716660,"uncompressedSha256":"c7b150390f7eda54b297a9971c68c4460279c5cdfb0b2061dceff4f8fd662604","parts":[{"path":"Build/5623047e2713_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"7085f1ccb779ebf1bce4dba35e0fb041c0b7a6fa1b1c79c31e906113a2310c27"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"099b9679643b824bf625dfff0f1d8a7912339e7260914fbd27158dcbd52e4489"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"47684b3808bd2aef0c0c90b9816ece8b86d826436aaa0b2f3ac47359839cad65"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"94cec861915c179fa6565934bd0f91661c56239e8195edf22de810e04669bee0"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"2ca55369490fa11df185f9100a99c276d6f198b30b95811c450f3e1c6a5ad1c7"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"05c94a60be840e6ae859425491b68c07f83a188eac2de01ab54b9f760d2e743a"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"9ae26d33c539d412317e50e36b004a4b21d29729bd75f684860ce485f5eb0094"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"2a96209b9bfbac2e38f49746b6a5339230ed0963f33abb3af79fd058970f7425"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"1d76ae63208b45f167a603b24ba8ab93b83d240496dcaf98adb714e97020d2fc"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"0f034db1dea1eb0a84507b63ac7bc40e4f1bfac2551aaeecd4c4697f46169c16"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part010","bytes":33554432,"sha256":"8c9f09fafe12f2afcb2ed1c61f3a59ebafa26bb587aa4657db0cea14342acbff"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part011","bytes":33554432,"sha256":"a369da8c673e8a8bc5bb902a09949791a86a3d35ce94e6d306faac5835d4d14a"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part012","bytes":33554432,"sha256":"bc1f2186d5edb72295cbb8dc737fc54a8cbf41d6c4648db2b65030c637b07589"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part013","bytes":33554432,"sha256":"650dafb3fb41a334638061aead273b78e881b4b0528f9f9bc6f64cdda74a0a0f"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part014","bytes":33554432,"sha256":"aab16a1553b6f24f73857dafbfebea5412c06691ba96fbfe9678b981e5c97711"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part015","bytes":33554432,"sha256":"78104427c68130262eadc17a993cb87bd3c88a61b7ebbbee5439f34aae19707c"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part016","bytes":33554432,"sha256":"d929439a89cf1db708cdec643e9c404ded90b267386b7402846f3bdebce6f265"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part017","bytes":33554432,"sha256":"7f51575327c7056d20ff2781c900537deae4d75cc09e98bc50671471cb6710e5"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part018","bytes":33554432,"sha256":"3172e5473e6b5f32176a0856043e04c9338e52873c5c0a9766ef6e0521f471b3"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part019","bytes":33554432,"sha256":"dccb24d4c28c7988085d7b6dbbd7499041a5d8a7ea38590d00a8061c6aa45c7a"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part020","bytes":33554432,"sha256":"0d96ce7e104424049091003a9679566bde849c2fd71b1bb2cde1568aef88f807"},{"path":"Build/5623047e2713_WebGL.data.unityweb.part021","bytes":30282952,"sha256":"0d14734c694aeb3f2bd900134c635c2f43c661d36f4e152f8fdd57f45adb5171"}],"sourceCommit":"5623047e2713a25ffc885a2d3037b5f8727fd606","assetQualityChanged":true};
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
