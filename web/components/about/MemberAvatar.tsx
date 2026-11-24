'use client';

import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar';

export default function MemberAvatar({ name, src }: { name: string; src: string }) {
  return <Avatar className="member-avatar">
    {src && <AvatarImage className="member-avatar-image" src={src} alt={`${name} 的頭像`} />}
    <AvatarFallback className="member-avatar-initial" aria-label={name}>{name.slice(0, 1)}</AvatarFallback>
  </Avatar>;
}
