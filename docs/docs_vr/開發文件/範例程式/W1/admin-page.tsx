import { notFound } from 'next/navigation';
import AdminPanel from '@/components/features/AdminPanel';
export const dynamic = 'force-dynamic';
export default function AdminPage() {
  if (process.env.ENABLE_LOCAL_ADMIN !== '1') notFound();
  return <AdminPanel />;
}
