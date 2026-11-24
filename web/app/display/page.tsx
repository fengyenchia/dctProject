export const metadata = { title: "Display" };

export default function DisplayPage() {
  return (
    <main className="video-frame">
      <video
        src="/videos/prelude.mp4"
        controls
        loop
        playsInline
        className="video-content"
      />
    </main>
  );
}
