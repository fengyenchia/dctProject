import teamData from "@/data/team.json";
import TeamMemberCard from "@/components/about/TeamMemberCard";
import type { TeamData } from "@/types/team";
import "./team.css";

export const metadata = { title: "About" };

export default function AboutPage() {
  const team: TeamData = teamData;
  return (
    <main className="page-shell">
      <section className="team-section" aria-labelledby="team-heading">
        <h2 className="subtitle" id="team-heading">
          {team.teamName}
        </h2>
        <ul className="team-grid">
          {team.members.map((member) => (
            <TeamMemberCard key={member.id} member={member} />
          ))}
        </ul>
      </section>
    </main>
  );
}
