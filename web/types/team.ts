export type TeamMember = {
  id: string;
  name: string;
  avatar: string;
  role: string;
  hashtags: string[];
  responsibilities: string[];
};

export type TeamData = {
  teamName: string;
  members: TeamMember[];
};
