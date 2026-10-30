/* Braxhorn: bounded native gzip streaming. Native stream algorithm preserved; room rebuilt on an isolated test branch. */
'use strict';
(() => {
  const manifest = {"dataPath":"Build/cc3325ab1e0e_WebGL.data.unityweb","bytes":574530195,"sha256":"6bda06f16ec1be0165db288ab467d1c294062a0bada4a94297e429bf0b449cc0","uncompressedBytes":748861305,"uncompressedSha256":"aaf1ad5ec6b531e087bd8939adba59e48992e9ab99734794216ee2131f9928ef","parts":[{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part000","bytes":33554432,"sha256":"993d9d092d7942bf57b9202a97d5077fd7d6ba4e5a2cd9a3190abebefb08f5c6"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part001","bytes":33554432,"sha256":"5e983ba250f3e0f53280b5b2adfa55460c05651c7b1ed213376086ddcd1370e9"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part002","bytes":33554432,"sha256":"854eda3ecd96677a8d8adccc38d3e4ab1ef601dae157844bafaa008cb820f5ec"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part003","bytes":33554432,"sha256":"5efdf492a63bacfa7a79ebc3f7d38fb1d05d1acead253b0356d488f685c3154e"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part004","bytes":33554432,"sha256":"e30b1a10306784ba82f6840b60880683221ef38d2f00e660909b18a31b4702dd"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part005","bytes":33554432,"sha256":"0f929df09ec1a85a82136e5bb247c521d7a106907f3d1fe23b2a8bf1a3b26feb"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part006","bytes":33554432,"sha256":"ae2813ee7f174962595a950c7902ae5783f016a16740f15a3e0340fbe05c9c33"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part007","bytes":33554432,"sha256":"c1f2121d9c45401fc75702dff562311ea812945b2a360f36845f54ceda0a7d39"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part008","bytes":33554432,"sha256":"de7f2a4e2833879b8a2db050855bf71ab84f7d1e7740614251c14ab33cebe2ce"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part009","bytes":33554432,"sha256":"707ee2fb24ea55e6a5966746cb6289968e72fb5548c1c773112b090950a28133"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part010","bytes":33554432,"sha256":"bef12809630e69d5e66a38e2a7858620d675e1dca81f30f28049083224fe76d9"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part011","bytes":33554432,"sha256":"f73304b20575277abe54520ce304fdbf03846cd21863e79ab6333fd1c5d3b48e"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part012","bytes":33554432,"sha256":"ec81596ef87fb854afaca4372b2231ea2884159b53661109c255ed2a90a6a3cf"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part013","bytes":33554432,"sha256":"f2cd764ce76b3e1ba7304f3c371136e5c692254b1b3e9597dfda4050306c3454"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part014","bytes":33554432,"sha256":"f961548a803f8b374cd0ef46ce33686d9b7e4e6640a6e89c7e9c4a6f890613f1"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part015","bytes":33554432,"sha256":"00e23d2be93dcbfcaf8b1cc78a65a5a0a830fbce7ed61787c15cbb8049d01cb8"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part016","bytes":33554432,"sha256":"7a399b14b7249d5f7eb759ba487713c12ebaff905ab710a8680a0e435eb6307b"},{"path":"Build/cc3325ab1e0e_WebGL.data.unityweb.part017","bytes":4104851,"sha256":"4b144ebd3276c0453444ff1041123ec7c70e677d697bfa8a3e6975814bb961eb"}],"sourceCommit":"cc3325ab1e0ec5ec5a1bb3ef65da50f325a7999a","assetQualityChanged":true};
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
