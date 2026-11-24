import type { TeamMember } from '@/types/team';
import MemberAvatar from './MemberAvatar';
import { Card } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';

export default function TeamMemberCard({ member }: { member: TeamMember }) {
  const tags = member.hashtags.map(tag => tag.trim().replace(/^#+/, '')).filter(Boolean).slice(0, 3);
  return <li className="team-member">
    <Card className="member-card" role="article" aria-labelledby={`${member.id}-name`}>
      <MemberAvatar name={member.name} src={member.avatar} />
      <div className="member-heading">
        <h3 className="member-name" id={`${member.id}-name`}>{member.name}</h3>
        {member.role && <p className="page-note">{member.role}</p>}
      </div>
      {tags.length > 0 && <ul className="member-tags" aria-label={`${member.name} 的 hashtag`}>
        {tags.map((tag, index) => <li key={index}><Badge className="member-tag">#{tag}</Badge></li>)}
      </ul>}
      {member.responsibilities.length > 0 && <div className="member-responsibilities">
        <h4 className="member-label">負責項目</h4>
        <ul className="member-task-list">{member.responsibilities.map((task, index) => <li key={index}>{task}</li>)}</ul>
      </div>}
    </Card>
  </li>;
}
