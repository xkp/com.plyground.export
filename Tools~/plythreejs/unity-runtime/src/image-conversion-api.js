// ImageConversion is a separate Unity module. Keep unsupported encoders callable.
let reportUse = (api, fallback = null) => { console.warn(`${api} is unsupported in Preview`); return fallback; };
export function installImageConversionApi(report) { reportUse = report; }

export class ImageConversion {
  static EncodeToPNG(texture) {
    try { if (texture?.EncodeToPNG) return texture.EncodeToPNG(); }
    catch (error) { if (!error.message?.includes('browser canvas')) throw error; }
    return reportUse('UnityEngine.ImageConversion.EncodeToPNG', new Uint8Array(0));
  }
  static EncodeToJPG(texture, quality = 75) {
    try { if (texture?.EncodeToJPG) return texture.EncodeToJPG(quality); }
    catch (error) { if (!error.message?.includes('browser canvas')) throw error; }
    return reportUse('UnityEngine.ImageConversion.EncodeToJPG', new Uint8Array(0));
  }
  static LoadImage() { return reportUse('UnityEngine.ImageConversion.LoadImage', false); }
}

for (const name of ['EncodeToEXR', 'EncodeToTGA',
  'EncodeArrayToPNG', 'EncodeArrayToJPG', 'EncodeArrayToEXR', 'EncodeArrayToTGA',
  'EncodeNativeArrayToPNG', 'EncodeNativeArrayToJPG', 'EncodeNativeArrayToEXR',
  'EncodeNativeArrayToTGA', 'EncodeNativeArrayToR2D',
  'EncodeArrayToR2D']) {
  Object.defineProperty(ImageConversion, name, {
    value() { return reportUse(`UnityEngine.ImageConversion.${name}`, new Uint8Array(0)); },
    configurable: true,
  });
}
