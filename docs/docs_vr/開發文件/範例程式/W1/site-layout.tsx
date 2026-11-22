import type { Metadata } from 'next';
import type { ReactNode } from 'react';
import SiteHeader from '@/components/common/SiteHeader';
import './globals.css';

export const metadata: Metadata = { title: '音虫｜作品與遊戲成果' };

export default function RootLayout({ children }: { children: ReactNode }) {
  return <html lang="zh-Hant"><body><SiteHeader />{children}</body></html>;
}
