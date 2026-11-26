// Browser transport for the common UnityWebRequest surface. Browser CORS,
// credentials, redirects, and forbidden headers remain browser-owned.
import * as THREE from 'three';
import { CSharpDictionary } from './collections.js';

export function createUnityWebRequestAPI(AsyncOperation, AssetBundle, Texture2D, AudioClip) {
  class DownloadHandler {
    constructor() { this.data = new Uint8Array(0); this.isDone = false; this.error = null; }
    get text() { return new TextDecoder().decode(this.data); }
    get nativeData() { return this.data; }
    Dispose() { this.data = new Uint8Array(0); }
  }
  class DownloadHandlerBuffer extends DownloadHandler {
    static GetContent(request) { return request.downloadHandler?.data ?? new Uint8Array(0); }
  }
  class DownloadHandlerTexture extends DownloadHandler {
    constructor(readable = false) { super(); this.readable = readable; this.texture = null; }
    static GetContent(request) { return request.downloadHandler?.texture ?? null; }
  }
  class DownloadHandlerAudioClip extends DownloadHandler {
    constructor(url = '', audioType = 0) { super(); this.url = url; this.audioType = audioType; this.audioClip = null; }
    static GetContent(request) { return request.downloadHandler?.audioClip ?? null; }
  }
  class DownloadHandlerAssetBundle extends DownloadHandler {
    constructor(url = '') { super(); this.url = url; this.assetBundle = null; }
    static GetContent(request) { return request.downloadHandler?.assetBundle ?? null; }
  }
  class UploadHandler {
    constructor(data = new Uint8Array(0)) { this.data = data; this.contentType = null; this.progress = 0; }
    Dispose() { this.data = new Uint8Array(0); }
  }
  class UploadHandlerRaw extends UploadHandler {
    constructor(data) { super(typeof data === 'string' ? new TextEncoder().encode(data)
      : data instanceof Uint8Array ? data : new Uint8Array(data ?? [])); }
  }
  class UnityWebRequestAsyncOperation extends AsyncOperation {
    constructor(webRequest = null) { super(); this.webRequest = webRequest; this._nativeAttached = true; }
  }
  class UnityWebRequest {
    static Result = Object.freeze({ InProgress: 0, Success: 1, ConnectionError: 2,
      ProtocolError: 3, DataProcessingError: 4 });
    static kHttpVerbGET = 'GET'; static kHttpVerbPOST = 'POST';
    static kHttpVerbPUT = 'PUT'; static kHttpVerbDELETE = 'DELETE';
    static kHttpVerbHEAD = 'HEAD'; static kHttpVerbCREATE = 'CREATE';
    constructor(url = '', method = 'GET', downloadHandler = null, uploadHandler = null) {
      this.url = String(url ?? ''); this.method = method;
      this.downloadHandler = downloadHandler; this.uploadHandler = uploadHandler;
      this.disposeDownloadHandlerOnDispose = true;
      this.disposeUploadHandlerOnDispose = true;
      this.timeout = 0; this.redirectLimit = 32;
      this.result = UnityWebRequest.Result.InProgress;
      this.responseCode = 0; this.error = null; this.isDone = false;
      this.downloadProgress = 0; this.uploadProgress = 0;
      this.downloadedBytes = 0; this.uploadedBytes = 0;
      this._headers = new Map(); this._responseHeaders = new Map();
      this._sent = false; this._controller = null;
    }
    static Get(url) { return new this(url, 'GET', new DownloadHandlerBuffer()); }
    static Delete(url) { return new this(url, 'DELETE', new DownloadHandlerBuffer()); }
    static Head(url) { return new this(url, 'HEAD', new DownloadHandlerBuffer()); }
    static Post(url, body = '', contentType = 'application/x-www-form-urlencoded') {
      if (body instanceof Map || body && typeof body === 'object'
        && !ArrayBuffer.isView(body) && !Array.isArray(body))
        body = new URLSearchParams(body instanceof Map ? [...body] : Object.entries(body)).toString();
      const upload = new UploadHandlerRaw(body);
      upload.contentType = contentType;
      return new this(url, 'POST', new DownloadHandlerBuffer(), upload);
    }
    static Put(url, body) {
      const upload = new UploadHandlerRaw(body);
      upload.contentType = 'application/octet-stream';
      return new this(url, 'PUT', new DownloadHandlerBuffer(), upload);
    }
    static GetTexture(url, readable = false) {
      return new this(url, 'GET', new DownloadHandlerTexture(readable));
    }
    static GetAudioClip(url, audioType = 0) {
      return new this(url, 'GET', new DownloadHandlerAudioClip(url, audioType));
    }
    static GetAssetBundle(url) {
      return new this(url, 'GET', new DownloadHandlerAssetBundle(url));
    }
    static EscapeURL(value) { return encodeURIComponent(value); }
    static UnEscapeURL(value) { return decodeURIComponent(value); }
    get uri() { return new URL(this.url, globalThis.location?.href ?? 'http://localhost/'); }
    set uri(value) { this.url = String(value); }
    get isModifiable() { return !this._sent; }
    get isNetworkError() { return this.result === UnityWebRequest.Result.ConnectionError; }
    get isHttpError() { return this.result === UnityWebRequest.Result.ProtocolError; }
    get isError() { return this.isNetworkError || this.isHttpError; }
    SetRequestHeader(name, value) {
      if (this._sent) throw new Error('Cannot set request headers after SendWebRequest');
      this._headers.set(String(name).toLowerCase(), [String(name), String(value)]);
    }
    GetRequestHeader(name) { return this._headers.get(String(name).toLowerCase())?.[1] ?? null; }
    GetResponseHeader(name) { return this._responseHeaders.get(String(name).toLowerCase()) ?? null; }
    GetResponseHeaders() { return new CSharpDictionary(this._responseHeaders); }
    Send() { return this.SendWebRequest(); }
    SendWebRequest() {
      if (this._sent) throw new Error('UnityWebRequest has already been sent');
      this._sent = true;
      this.downloadHandler ??= new DownloadHandlerBuffer();
      const operation = new UnityWebRequestAsyncOperation(this);
      this._controller = new AbortController();
      const headers = Object.fromEntries([...this._headers.values()]);
      if (this.uploadHandler?.contentType && !this.GetRequestHeader('Content-Type'))
        headers['Content-Type'] = this.uploadHandler.contentType;
      const body = this.uploadHandler?.data;
      let timer = null;
      if (this.timeout > 0) timer = setTimeout(() => this._controller.abort('timeout'), this.timeout * 1000);
      Promise.resolve().then(async () => {
        if (this.downloadHandler instanceof DownloadHandlerAssetBundle) {
          const cloudRequest = AssetBundle.LoadFromFileAsync(this.uri.pathname);
          await cloudRequest;
          if (cloudRequest.error) throw cloudRequest.error;
          this.downloadHandler.assetBundle = cloudRequest.assetBundle;
          this.downloadHandler.isDone = true;
          this.result = UnityWebRequest.Result.Success;
          this.downloadProgress = 1;
          return;
        }
        const response = await fetch(this.url, { method: this.method, headers,
          body: body?.length ? body : undefined, signal: this._controller.signal,
          redirect: this.redirectLimit === 0 ? 'manual' : 'follow' });
        this.responseCode = response.status;
        response.headers.forEach((value, key) => this._responseHeaders.set(key.toLowerCase(), value));
        this.result = response.ok ? UnityWebRequest.Result.Success
          : UnityWebRequest.Result.ProtocolError;
        this.error = response.ok ? null : `HTTP ${response.status} ${response.statusText}`.trim();
        const bytes = new Uint8Array(await response.arrayBuffer());
        this.downloadHandler.data = bytes;
        this.downloadHandler.isDone = true;
        this.downloadedBytes = bytes.byteLength;
        this.uploadedBytes = body?.length ?? 0;
        this.downloadProgress = 1; this.uploadProgress = 1;
        if (this.result === UnityWebRequest.Result.Success
          && this.downloadHandler instanceof DownloadHandlerTexture) {
          const blob = new Blob([bytes], { type: response.headers.get('content-type') ?? 'image/png' });
          let image;
          if (typeof createImageBitmap === 'function') image = await createImageBitmap(blob);
          else if (typeof Image === 'function') {
            const objectUrl = URL.createObjectURL(blob);
            try { image = await new Promise((resolve, reject) => {
              const element = new Image();
              element.onload = () => resolve(element);
              element.onerror = () => reject(new Error('Could not decode downloaded image'));
              element.src = objectUrl;
            }); } finally { URL.revokeObjectURL(objectUrl); }
          } else throw new Error('Browser image decoder is unavailable');
          const texture = new THREE.Texture(image);
          texture.colorSpace = THREE.SRGBColorSpace;
          texture.needsUpdate = true;
          this.downloadHandler.texture = Texture2D._fromThreeTexture(texture,
            { textureWidth: image.width, textureHeight: image.height,
              textureReadable: this.downloadHandler.readable });
        }
        if (this.result === UnityWebRequest.Result.Success
          && this.downloadHandler instanceof DownloadHandlerAudioClip) {
          const AudioContextType = globalThis.AudioContext ?? globalThis.webkitAudioContext;
          if (!AudioContextType) throw new Error('Browser audio decoder is unavailable');
          const context = new AudioContextType();
          try {
            const buffer = await context.decodeAudioData(bytes.buffer.slice(0));
            this.downloadHandler.audioClip = new AudioClip(buffer, { name: this.url.split('/').at(-1) });
          } finally { await context.close?.(); }
        }
      }).catch(error => {
        this.result = this.result === UnityWebRequest.Result.Success
          ? UnityWebRequest.Result.DataProcessingError
          : UnityWebRequest.Result.ConnectionError;
        this.error = error?.message ?? String(error);
        this.downloadHandler.error = this.error;
        this.downloadHandler.isDone = true;
      }).finally(() => {
        if (timer) clearTimeout(timer);
        this.isDone = true; operation.isDone = true; operation.progress = 1;
        operation._completedInvoked = true;
        operation.completed.Invoke(operation);
      });
      return operation;
    }
    Abort() { this._controller?.abort(); }
    Dispose() {
      if (this.disposeDownloadHandlerOnDispose) this.downloadHandler?.Dispose();
      if (this.disposeUploadHandlerOnDispose) this.uploadHandler?.Dispose();
    }
  }
  class UnityWebRequestTexture {
    static GetTexture(url, readable = false) { return UnityWebRequest.GetTexture(url, readable); }
  }
  class UnityWebRequestMultimedia {
    static GetAudioClip(url, audioType) { return UnityWebRequest.GetAudioClip(url, audioType); }
  }
  class UnityWebRequestAssetBundle {
    static GetAssetBundle(url) { return UnityWebRequest.GetAssetBundle(url); }
  }
  return { UnityWebRequest, UnityWebRequestAsyncOperation, DownloadHandler,
    DownloadHandlerBuffer, DownloadHandlerTexture, DownloadHandlerAudioClip,
    DownloadHandlerAssetBundle, UnityWebRequestTexture, UnityWebRequestMultimedia,
    UnityWebRequestAssetBundle, UploadHandler, UploadHandlerRaw };
}
