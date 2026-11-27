import assert from 'node:assert/strict';
import test from 'node:test';
import { Animator, GameObject, Rigidbody } from './index.js';
import { serializePreviewObject } from './preview-importer.js';

test('materialized characters retain their exported Humanoid Avatar identity', () => {
  const actor = new GameObject('Character');
  const animator = actor.AddComponent(Animator);
  animator.avatarData = { id: 'avatar-123', name: 'CharacterAvatar',
    isHuman: true, humanDescription: { human: [], skeleton: [] } };
  const scene = serializePreviewObject(actor, 'character');
  const record = scene.components.find(component => component.type === 'Animator');
  assert.equal(record.properties.avatarData.id, 'avatar-123');
  assert.equal(record.properties.avatarData.name, 'CharacterAvatar');
});

test('materialized characters retain their configured Rigidbody mass', () => {
  const actor = new GameObject('Character');
  const rigidbody = actor.AddComponent(Rigidbody);
  rigidbody.mass = 85;
  rigidbody.linearDamping = 0.4;
  const scene = serializePreviewObject(actor, 'character');
  const record = scene.components.find(component => component.type === 'Rigidbody');
  assert.equal(record.properties.mass, 85);
  assert.equal(record.properties.linearDamping, 0.4);
});
