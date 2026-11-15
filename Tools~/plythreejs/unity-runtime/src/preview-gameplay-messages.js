import { UnityType } from './index.js';

// The gameplay SDK is a project package, so a module export can refer to its
// messages without owning their definitions. Supply them to browser previews.
export function registerPreviewGameplayMessages() {
  const subscribers = new Map();
  class GameplayBus {
    static Publish(message) {
      for (const [type, handlers] of subscribers)
        if (message instanceof type)
          for (const handler of [...handlers]) handler(message);
    }
    static Subscribe(type, handler) {
      const handlers = subscribers.get(type) ?? new Set();
      handlers.add(handler);
      subscribers.set(type, handlers);
      return { Dispose: () => handlers.delete(handler) };
    }
    static Unsubscribe(type, handler) { subscribers.get(type)?.delete(handler); }
  }
  const messages = {
    EnemyDetectedPlayerMessage: 'core.enemy.detected_player',
    EnemyLostPlayerMessage: 'core.enemy.lost_player',
    AlertRaisedMessage: 'core.alert.raised',
    SoundHeardMessage: 'core.sound.heard'
  };
  const fallback = { 'Plyground.Gameplay.Runtime.GameplayBus': GameplayBus };
  for (const [name, id] of Object.entries(messages))
    fallback[`Plyground.Gameplay.Runtime.${name}`] = class {
      constructor() { this.Id = id; }
    };
  const missing = Object.fromEntries(Object.entries(fallback)
    .filter(([name]) => !UnityType.GetType(name)));
  UnityType.Register(missing);
  return missing;
}
