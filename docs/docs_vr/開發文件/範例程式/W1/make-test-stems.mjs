import { mkdirSync, existsSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

// 僅供串接練習的合成音色，不是正式配樂。
const output = resolve(process.argv[2] ?? 'public/assets/prototype-1/stems');
const ids = ['A1', 'A2', 'A3', 'B1', 'B2', 'B3', 'C1', 'C2', 'C3'];
const frequencies = [261.626, 329.628, 391.995, 130.813, 164.814, 195.998, 523.251, 659.255, 783.991];
const paths = ids.map(id => resolve(output, `stem-${id}.wav`));
if (paths.some(path => existsSync(path))) throw new Error('測試音檔已存在；不覆蓋既有素材。');
mkdirSync(output, { recursive: true });
const rate = 48000, beat = 60 / 100, frames = Math.round(rate * beat * 16);
ids.forEach((id, index) => {
  const wav = Buffer.alloc(44 + frames * 2);
  wav.write('RIFF', 0); wav.writeUInt32LE(wav.length - 8, 4); wav.write('WAVE', 8); wav.write('fmt ', 12);
  wav.writeUInt32LE(16, 16); wav.writeUInt16LE(1, 20); wav.writeUInt16LE(1, 22);
  wav.writeUInt32LE(rate, 24); wav.writeUInt32LE(rate * 2, 28); wav.writeUInt16LE(2, 32); wav.writeUInt16LE(16, 34);
  wav.write('data', 36); wav.writeUInt32LE(frames * 2, 40);
  for (let i = 0; i < frames; i++) {
    const t = i / rate, phase = t % beat;
    const envelope = Math.min(phase / .015, 1) * Math.exp(-phase * 7);
    const value = Math.sin(t * frequencies[index] * 2 * Math.PI) * envelope * .12;
    wav.writeInt16LE(Math.round(value * 32767), 44 + i * 2);
  }
  writeFileSync(paths[index], wav);
});
console.log(`Created 9 tutorial stems, 100 BPM, 4/4, 4 bars: ${output}`);
