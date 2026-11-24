import processData from '@/data/process.json';

export const metadata = { title: 'Process' };

type ProcessEntry = { id: string; date: string; title: string; description: string; image: string };

export default function ProcessPage() {
  const entries: ProcessEntry[] = processData.entries;
  return <main className="page-shell">
    <h1 className="title" lang="en">Process</h1>
    <ul className="space-y-6">
      {entries.map(entry => <li className="content-panel" key={entry.id}>
        <time dateTime={entry.date} className="page-note">{entry.date}</time>
        <h2 className="subtitle">{entry.title}</h2>
        {entry.description && <p className="text">{entry.description}</p>}
        {entry.image && <a className="text-link" href={entry.image}>查看紀錄圖片</a>}
      </li>)}
    </ul>
  </main>;
}
