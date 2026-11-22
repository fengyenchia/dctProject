import Link from 'next/link';

export default function SiteHeader() {
  return <header className="border-b">
    <nav aria-label="主要導覽" className="mx-auto flex max-w-5xl gap-6 p-6">
      <Link href="/">首頁</Link>
      <Link href="/results">遊戲成果</Link>
      <Link href="/about">團隊介紹</Link>
    </nav>
  </header>;
}
