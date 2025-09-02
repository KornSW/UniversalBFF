import { SVGProps } from "react";
export function SwitchOn(props: SVGProps<SVGSVGElement>) {
  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      viewBox="0 0 16 16"
      width="1.5em"
      height="1.5em"
      {...props}
    >
      <path
        fill="currentColor"
        d="M5 3a5 5 0 0 0 0 10h6a5 5 0 0 0 0-10zm6 9a4 4 0 1 1 0-8a4 4 0 0 1 0 8"
      ></path>
    </svg>
  );
}
