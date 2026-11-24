"use client";
import { useState } from "react";
import { IconPhoto } from "@tabler/icons-react";
export default function ResultIcon({
  url,
  name,
  code,
  variant = "thumbnail",
}: {
  url: string;
  name: string;
  code: string;
  variant?: "thumbnail" | "cover";
}) {
  const [failed, setFailed] = useState(false);
  const isCover = variant === "cover";
  return failed || !url ? (
    isCover ? (
      <div className="result-cover-placeholder" role="img" aria-label={name}>
        <IconPhoto size={48} stroke={1.5} aria-hidden="true" />
      </div>
    ) : (
      <div className="result-icon-placeholder">{code}</div>
    )
  ) : (
    <img
      src={url}
      alt={name}
      width={160}
      height={160}
      onError={() => setFailed(true)}
      className={isCover ? "result-cover-image" : "result-icon"}
    />
  );
}
