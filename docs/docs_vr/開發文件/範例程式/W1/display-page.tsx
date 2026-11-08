export default function DisplayPage() {
  return <main className="flex min-h-screen items-center justify-center bg-black">
    <video src="/videos/prelude.mp4" controls loop playsInline className="max-h-screen w-full" />
  </main>;
}
