import { Vector2, Vector4 } from './math.js';

export class SecondarySpriteTexture {
  constructor(name = null, texture = null) {
    this.name = name;
    this.texture = texture;
  }
  Equals(other) {
    return other instanceof SecondarySpriteTexture
      && this.name === other.name && this.texture === other.texture;
  }
  GetHashCode() {
    let hash = 2166136261;
    for (const character of this.name ?? '')
      hash = Math.imul(hash ^ character.charCodeAt(0), 16777619);
    return (hash ^ (this.texture?.GetHashCode?.() ?? 0)) | 0;
  }
  static op_Equality(left, right) { return left?.Equals(right) ?? right == null; }
  static op_Inequality(left, right) { return !this.op_Equality(left, right); }
}

function simpleSprite(sprite) {
  if (!sprite?.rect || !sprite?.texture)
    throw new TypeError('DataUtility requires a Sprite with a texture');
  if (sprite.packed || sprite.textureRectOffset?.x || sprite.textureRectOffset?.y)
    throw new Error('Packed or trimmed Sprite UV data requires an exported atlas mapping');
  const width = sprite.texture.width ?? sprite.texture.image?.width;
  const height = sprite.texture.height ?? sprite.texture.image?.height;
  if (!(width > 0 && height > 0))
    throw new Error('DataUtility requires exported texture dimensions');
  return { width, height };
}

export class DataUtility {
  static GetOuterUV(sprite) {
    const { width, height } = simpleSprite(sprite);
    const { x, y, width: w, height: h } = sprite.rect;
    return new Vector4(x / width, y / height, (x + w) / width, (y + h) / height);
  }
  static GetInnerUV(sprite) {
    const { width, height } = simpleSprite(sprite);
    const { x, y, width: w, height: h } = sprite.rect;
    const { x: left, y: bottom, z: right, w: top } = sprite.border;
    return new Vector4((x + left) / width, (y + bottom) / height,
      (x + w - right) / width, (y + h - top) / height);
  }
  static GetPadding(sprite) { simpleSprite(sprite); return new Vector4(); }
  static GetMinSize(sprite) {
    simpleSprite(sprite);
    return new Vector2(sprite.border.x + sprite.border.z,
      sprite.border.y + sprite.border.w);
  }
}
