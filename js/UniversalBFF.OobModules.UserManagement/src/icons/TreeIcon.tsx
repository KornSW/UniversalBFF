import { SVGProps } from "react";

export function TreeIcon(props: SVGProps<SVGSVGElement>) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 24 24"
      width="1.3em"
      height="1.3em"
      {...props}
    >
      <g
        fill="none"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="2"
      >
        <path d="M21 12h-8m8-6H8m13 12h-8M3 6v4c0 1.1.9 2 2 2h3"></path>
        <path d="M3 10v6c0 1.1.9 2 2 2h3"></path>
      </g>
    </svg>
  );
}
