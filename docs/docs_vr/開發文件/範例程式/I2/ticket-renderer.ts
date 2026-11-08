import { mkdirSync, existsSync, readFileSync, writeFileSync, renameSync } from 'node:fs';
import { resolve, join } from 'node:path';
import { createHash } from 'node:crypto';
import QRCode from 'qrcode';
import { Resvg } from '@resvg/resvg-js';

export type TicketResult = {
  result_id: string; token: string; asset_version: string;
  variants: Array<{ variant_id: string; name: string; source_label: string; image_id: string }>;
};
export type TicketFiles = { png: string; svg: string; qrUrl: string; layout: string };
const escape = (s: string) => s.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&apos;' }[c]!));
function lines(text: string, length = 23): string[] {
  const chars = Array.from(text); const result: string[] = [];
  while (chars.length && result.length < 2) result.push(chars.splice(0, length).join(''));
  if (chars.length) result[1] += '…';
  return result;
}
export async function renderTicket(payload: TicketResult, dataDir: string, resultsBase: string, requireArt = false): Promise<TicketFiles> {
  if (!/^[a-f0-9]{64}$/.test(payload.token) || !/^[\w-]+$/.test(payload.asset_version)
    || payload.variants.length !== 5 || new Set(payload.variants.map(x => x.variant_id)).size !== 5)
    throw new Error('Invalid ticket result');
  const url = new URL(resultsBase.replace(/\/$/, '') + '/' + payload.token);
  if (!['https:', 'http:'].includes(url.protocol) || url.username || url.password || url.search || url.hash)
    throw new Error('Invalid RESULTS_BASE_URL');
  const folder = join(dataDir, 'tickets', createHash('sha256').update(payload.result_id).digest('hex').slice(0, 32));
  mkdirSync(folder, { recursive: true });
  const qr = await QRCode.toDataURL(url.href, { errorCorrectionLevel: 'M', margin: 4, width: 320 });
  let placeholder = false;
  const icons = payload.variants.map((v, i) => {
    if (!/^[ABC][123]$/.test(v.variant_id) || !/^[\w-]+$/.test(v.image_id)) throw new Error('Invalid image id');
    const file = resolve('assets', payload.asset_version, 'ticket', v.image_id + '.png');
    const x = i < 3 ? 106 + i * 182 : 197 + (i - 3) * 182;
    const y = i < 3 ? 200 : 360;
    let image: string;
    if (existsSync(file)) image = `<image x="${x-58}" y="${y-58}" width="116" height="116" href="data:image/png;base64,${readFileSync(file).toString('base64')}"/>`;
    else {
      if (requireArt) throw new Error('Missing black-and-white ticket art: ' + v.image_id);
      placeholder = true;
      const radius = 38 + Number(v.variant_id[1]) * 4;
      const shape = v.variant_id[0] === 'A' ? `<path d="M${x-55},${y+20} L${x},${y-50} L${x+55},${y+20} L${x},${y+48}Z"/>`
        : v.variant_id[0] === 'B' ? `<rect x="${x-42}" y="${y-48}" width="84" height="96" rx="30"/>`
        : `<circle cx="${x}" cy="${y}" r="${radius}"/>`;
      image = `<g fill="black">${shape}</g><g fill="white"><circle cx="${x-14}" cy="${y-8}" r="7"/><circle cx="${x+14}" cy="${y-8}" r="7"/></g>`;
    }
    return image + `<text x="${x}" y="${y+80}" text-anchor="middle" font-size="25">${escape(v.variant_id)}</text>`;
  }).join('');
  const list = payload.variants.map((v, i) => {
    const y = 565 + i * 76;
    return `<text x="32" y="${y}" font-size="23" font-weight="bold">${escape(v.name)}</text>`
      + lines(v.source_label).map((line, n) => `<text x="32" y="${y+27+n*24}" font-size="20">${escape(line)}</text>`).join('');
  }).join('');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="576" height="1390" viewBox="0 0 576 1390">
  <rect width="576" height="1390" fill="white"/>
  <g font-family="Microsoft JhengHei, PingFang TC, Noto Sans CJK TC, sans-serif" fill="black">
    <text x="288" y="65" text-anchor="middle" font-size="40" font-weight="bold">聲音收藏票</text>
    <text x="288" y="110" text-anchor="middle" font-size="22">本場收集 5 個變體</text>
    <path d="M24 132H552 M24 490H552 M24 950H552" stroke="black" stroke-width="2" stroke-dasharray="8 7"/>
    ${icons}<text x="288" y="530" text-anchor="middle" font-size="26">聲音清單</text>${list}
    <image x="128" y="975" width="320" height="320" href="${qr}"/>
    <text x="288" y="1325" text-anchor="middle" font-size="22">掃碼聆聽本場旋律</text>
    <text x="288" y="1362" text-anchor="middle" font-size="16">${placeholder ? 'PROTOTYPE ART／測試圖樣' : '聲音收藏票'}</text>
  </g></svg>`;
  const files = { png: join(folder, 'ticket.png'), svg: join(folder, 'ticket.svg'), qrUrl: url.href, layout: 'ticket-v1' };
  const png = new Resvg(svg, { font: { loadSystemFonts: true } }).render().asPng();
  writeFileSync(files.svg + '.tmp', svg); renameSync(files.svg + '.tmp', files.svg);
  writeFileSync(files.png + '.tmp', png); renameSync(files.png + '.tmp', files.png);
  return files;
}
