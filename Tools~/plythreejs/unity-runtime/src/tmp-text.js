// Small screen-space TMP atlas renderer. Glyph placement uses exported Unity
// metrics; native font generation, shaping and material effects stay outside it.
function styledCharacters(value, richText, baseBold, baseItalic, baseSize, onUnsupportedTag) {
  if (!richText) return [...value].map(character => ({ character, bold: baseBold,
    italic: baseItalic, size: baseSize, color: null, run: 0 }));
  const result = [];
  const colors = [];
  const sizes = [];
  let boldDepth = 0, italicDepth = 0;
  let run = 0;
  for (let index = 0; index < value.length;) {
    if (value[index] === '<') {
      const end = value.indexOf('>', index + 1);
      if (end !== -1) {
        const tag = value.slice(index + 1, end);
        const color = /^color=#([\da-f]{6})([\da-f]{2})?$/i.exec(tag);
        const size = /^size=(\d+(?:\.\d+)?)(%)?$/.exec(tag);
        if (tag === 'b') boldDepth++;
        else if (tag === '/b') boldDepth = Math.max(0, boldDepth - 1);
        else if (tag === 'i') italicDepth++;
        else if (tag === '/i') italicDepth = Math.max(0, italicDepth - 1);
        else if (size) sizes.push(size[2] ? baseSize * Number(size[1]) / 100 : Number(size[1]));
        else if (tag === '/size') sizes.pop();
        else if (color) {
          const hex = color[1];
          colors.push({ r: parseInt(hex.slice(0, 2), 16) / 255,
            g: parseInt(hex.slice(2, 4), 16) / 255,
            b: parseInt(hex.slice(4, 6), 16) / 255,
            a: color[2] ? parseInt(color[2], 16) / 255 : 1 });
        } else if (tag === '/color') colors.pop();
        else onUnsupportedTag(tag);
        run++;
        index = end + 1;
        continue;
      }
    }
    const character = String.fromCodePoint(value.codePointAt(index));
    result.push({ character, bold: baseBold || boldDepth > 0,
      italic: baseItalic || italicDepth > 0, size: sizes.at(-1) ?? baseSize,
      color: colors.at(-1) ?? null, run });
    index += character.length;
  }
  return result;
}

function findGlyph(font, unicode, seen = new Set()) {
  if (!font || seen.has(font)) return null;
  seen.add(font);
  const glyph = font.glyphs?.get(unicode);
  if (glyph) return { glyph, font };
  for (const fallback of font.fallbackFonts ?? []) {
    const found = findGlyph(fallback, unicode, seen);
    if (found) return found;
  }
  return null;
}

function glyphOrReplacement(font, unicode) {
  return findGlyph(font, unicode) ?? findGlyph(font, 0xFFFD);
}

export function measureTmpLabel(text, maxWidth = Infinity) {
  const font = text.font;
  if (!font?.glyphs || !font.pointSize) return { width: 0, height: 0, lines: 0 };
  const characters = styledCharacters(String(text.text ?? ''), text.richText !== false,
    (text.fontStyle & 1) !== 0, (text.fontStyle & 2) !== 0,
    Math.max(0, Number(text.fontSize) || 0), () => {});
  const scale = Math.max(0, Number(text.fontSize) || 0,
    ...characters.map(style => style.size)) / font.pointSize;
  const advanceFor = style => {
    const found = glyphOrReplacement(font, style.character.codePointAt(0));
    return found ? ((style.character === ' ' ? found.font.spaceWidth ?? found.glyph.advance
      : found.glyph.advance) + (Number(text.characterSpacing) || 0)
      + ((text.fontStyle & 1) !== 0 ? found.font.boldSpacing ?? 0 : 0))
      * (style.size / found.font.pointSize) : 0;
  };
  let width = 0, widest = 0, lines = 1;
  for (let index = 0; index < characters.length; index++) {
    const { character } = characters[index];
    if (character === '\r') continue;
    if (character === '\n') { widest = Math.max(widest, width); width = 0; lines++; continue; }
    if (text.enableWordWrapping && Number.isFinite(maxWidth)
      && character !== ' ' && character !== '\t'
      && (index === 0 || /[\s]/.test(characters[index - 1].character))) {
      let wordWidth = 0;
      for (let next = index; next < characters.length
        && !/[\s]/.test(characters[next].character); next++)
        wordWidth += advanceFor(characters[next]);
      if (width > 0 && width + wordWidth > maxWidth) {
        widest = Math.max(widest, width); width = 0; lines++;
      }
    }
    if (text.enableWordWrapping && Number.isFinite(maxWidth) && width === 0
      && (character === ' ' || character === '\t')) continue;
    const advance = advanceFor(characters[index]);
    if (text.enableWordWrapping && Number.isFinite(maxWidth)
      && width > 0 && width + advance > maxWidth) {
      widest = Math.max(widest, width); width = 0; lines++;
    }
    width += advance;
  }
  const faceHeight = (font.ascentLine ?? font.pointSize * 0.8)
    - (font.descentLine ?? -font.pointSize * 0.2);
  return { width: Math.max(widest, width),
    height: (faceHeight + (lines - 1) * (font.lineHeight
      + (Number(text.lineSpacing) || 0))) * scale, lines };
}

export function drawTmpLabel(canvas, text, width, height, onMissingGlyph = () => {},
  thresholdOverride, onUnsupportedTag = () => {}) {
  const font = text.font;
  const image = font?.atlas?.threeTexture?.image ?? font?.atlas?.image;
  if (!image || !font.glyphs) return false;
  const pixelWidth = Math.max(1, Math.ceil(width));
  const pixelHeight = Math.max(1, Math.ceil(height));
  if (canvas.width !== pixelWidth) canvas.width = pixelWidth;
  if (canvas.height !== pixelHeight) canvas.height = pixelHeight;
  const context = canvas.getContext('2d');
  if (!context) return false;
  const bold = (text.fontStyle & 1) !== 0;
  const tint = text.color ?? { r: 1, g: 1, b: 1, a: 1 };
  const margin = text.margin ?? { x: 0, y: 0, z: 0, w: 0 };
  const left = Math.max(0, Number(margin.x) || 0);
  const topMargin = Math.max(0, Number(margin.y) || 0);
  const right = Math.max(0, Number(margin.z) || 0);
  const bottom = Math.max(0, Number(margin.w) || 0);
  const contentWidth = Math.max(1, pixelWidth - left - right);
  const contentHeight = Math.max(1, pixelHeight - topMargin - bottom);
  const key = [pixelWidth, pixelHeight, text.text, text.fontSize, text.fontStyle,
    text.alignment, text.enableWordWrapping, text.characterSpacing, text.lineSpacing,
    text.richText, left, topMargin, right, bottom,
    tint.r, tint.g, tint.b, tint.a, thresholdOverride].join('|');
  if (canvas._tmpDrawKey === key && canvas._tmpDrawFont === font) return true;
  const supersample = 3;
  const scale = Math.max(0, Number(text.fontSize) || 0) / font.pointSize;
  if (!scale) {
    context.clearRect(0, 0, pixelWidth, pixelHeight);
    canvas._tmpDrawKey = key; canvas._tmpDrawFont = font;
    return true;
  }
  const lines = [[]];
  let pen = 0;
  const characters = styledCharacters(String(text.text ?? ''), text.richText !== false,
    bold, (text.fontStyle & 2) !== 0, Math.max(0, Number(text.fontSize) || 0),
    onUnsupportedTag);
  for (let index = 0; index < characters.length; index++) {
    const style = characters[index];
    const { character } = style;
    if (character === '\r') continue;
    if (character === '\n') { lines.push([]); pen = 0; continue; }
    if (text.enableWordWrapping && character !== ' ' && character !== '\t'
      && (index === 0 || /[\s]/.test(characters[index - 1].character))) {
      let wordWidth = 0;
      for (let next = index; next < characters.length
        && !/[\s]/.test(characters[next].character); next++) {
        const found = glyphOrReplacement(font, characters[next].character.codePointAt(0));
        if (found) wordWidth += ((characters[next].character === ' '
          ? found.font.spaceWidth ?? found.glyph.advance : found.glyph.advance)
          + (Number(text.characterSpacing) || 0)
          + (characters[next].bold ? found.font.boldSpacing ?? 0 : 0))
          * (characters[next].size / found.font.pointSize);
      }
      if (pen > 0 && pen + wordWidth > contentWidth) { lines.push([]); pen = 0; }
    }
    if (text.enableWordWrapping && pen === 0 && (character === ' ' || character === '\t'))
      continue;
    const found = glyphOrReplacement(font, character.codePointAt(0));
    if (!findGlyph(font, character.codePointAt(0))) onMissingGlyph(character);
    if (!found) continue;
    const { glyph, font: glyphFont } = found;
    const glyphScale = style.size / glyphFont.pointSize;
    const advance = ((character === ' ' ? glyphFont.spaceWidth ?? glyph.advance
      : glyph.advance) + (Number(text.characterSpacing) || 0)
      + (style.bold ? glyphFont.boldSpacing ?? 0 : 0)) * glyphScale;
    const alignAdvance = ((character === ' ' ? glyphFont.spaceWidth ?? glyph.advance
      : glyph.advance) + (Number(text.characterSpacing) || 0)
      + (bold ? glyphFont.boldSpacing ?? 0 : 0)) * glyphScale;
    if (text.enableWordWrapping && pen > 0 && pen + advance > contentWidth) {
      lines.push([]); pen = 0;
    }
    lines.at(-1).push({ glyph, glyphFont, glyphScale, x: pen, advance,
      alignAdvance, style, character });
    pen += advance;
  }
  const layoutScale = Math.max(0, Number(text.fontSize) || 0,
    ...characters.map(style => style.size)) / font.pointSize;
  const lineHeight = (font.lineHeight + (Number(text.lineSpacing) || 0)) * layoutScale;
  const ascent = (font.ascentLine ?? font.pointSize * 0.8) * layoutScale;
  const descent = (font.descentLine ?? -font.pointSize * 0.2) * layoutScale;
  const blockHeight = ascent - descent + (lines.length - 1) * lineHeight;
  const vertical = text.alignment & 0xFF00;
  const top = topMargin + (vertical === 0x400 ? contentHeight - blockHeight
    : vertical === 0x200 ? (contentHeight - blockHeight) / 2 : 0);
  const layers = new Map();
  const layerFor = style => {
    let layer = layers.get(style.run);
    if (layer) return layer;
    const raster = canvas.ownerDocument.createElement('canvas');
    raster.width = pixelWidth * supersample;
    raster.height = pixelHeight * supersample;
    const rasterContext = raster.getContext('2d', { willReadFrequently: true });
    if (!rasterContext) return null;
    rasterContext.setTransform(supersample, 0, 0, supersample, 0, 0);
    rasterContext.clearRect(0, 0, pixelWidth, pixelHeight);
    layer = { raster, rasterContext, style };
    layers.set(style.run, layer);
    return layer;
  };
  for (let lineIndex = 0; lineIndex < lines.length; lineIndex++) {
    const line = lines[lineIndex];
    const last = line.findLast(item => item.character !== ' ' && item.character !== '\t');
    const lineWidth = last ? line.slice(0, line.indexOf(last) + 1)
      .reduce((sum, item) => sum + item.alignAdvance, 0) : 0;
    const horizontal = text.alignment & 0xFF;
    const offset = left + (horizontal === 4 ? contentWidth - lineWidth
      : horizontal === 2 ? (contentWidth - lineWidth) / 2 : 0);
    const baseline = top + ascent + lineIndex * lineHeight;
    for (const { glyph, glyphFont, glyphScale, x, style } of line) {
      if (!glyph.width || !glyph.height) continue;
      const layer = layerFor(style);
      if (!layer) return false;
      const glyphImage = glyphFont.atlas.threeTexture?.image ?? glyphFont.atlas.image;
      if (style.italic) {
        layer.rasterContext.save();
        layer.rasterContext.translate(baseline * 0.2, 0);
        layer.rasterContext.transform(1, 0, -0.2, 1, 0, 0);
      }
      layer.rasterContext.drawImage(glyphImage, glyph.x,
        glyphImage.height - glyph.y - glyph.height,
        glyph.width, glyph.height, offset + x + glyph.bearingX * glyphScale,
        baseline - glyph.bearingY * glyphScale,
        glyph.width * glyphScale, glyph.height * glyphScale);
      if (style.italic) layer.rasterContext.restore();
    }
  }
  context.clearRect(0, 0, pixelWidth, pixelHeight);
  context.imageSmoothingQuality = 'high';
  for (const { raster, rasterContext, style } of layers.values()) {
    const threshold = thresholdOverride ?? (style.bold ? 0.335 : 0.5);
    const color = style.color ?? { r: 1, g: 1, b: 1, a: 1 };
    const pixels = rasterContext.getImageData(0, 0, raster.width, raster.height);
    for (let i = 0; i < pixels.data.length; i += 4) {
      const distance = pixels.data[i + 3] / 255;
      const coverage = Math.max(0, Math.min(1, (distance - threshold) / 0.08 + 0.5));
      pixels.data[i] = Math.round(tint.r * color.r * 255);
      pixels.data[i + 1] = Math.round(tint.g * color.g * 255);
      pixels.data[i + 2] = Math.round(tint.b * color.b * 255);
      pixels.data[i + 3] = Math.round(coverage * (tint.a ?? 1) * color.a * 255);
    }
    rasterContext.putImageData(pixels, 0, 0);
    context.drawImage(raster, 0, 0, pixelWidth, pixelHeight);
  }
  canvas._tmpDrawKey = key; canvas._tmpDrawFont = font;
  return true;
}
