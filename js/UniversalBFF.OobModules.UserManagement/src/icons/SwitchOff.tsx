import { SVGProps } from "react";

export function SwitchOff(props: SVGProps<SVGSVGElement>) {
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
        d="M11 4a4 4 0 0 1 0 8H8a5 5 0 0 0 2-4a5 5 0 0 0-2-4zm-6 8a4 4 0 1 1 0-8a4 4 0 0 1 0 8M0 8a5 5 0 0 0 5 5h6a5 5 0 0 0 0-10H5a5 5 0 0 0-5 5"
      ></path>
    </svg>
  );
}
