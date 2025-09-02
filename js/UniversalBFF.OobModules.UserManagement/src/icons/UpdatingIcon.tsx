import React, { useState, useEffect } from "react";
import "./icons.css";

const UpdatingIcon = () => {
  let timer: any | null = null;
  const [red, setRed] = useState(true);
  const [timerRestart, setTimerRestart] = useState<boolean>(false);

  useEffect(() => {
    clearTimeout(timer);
    timer = setTimeout(() => {
      setRed((red) => !red);
      setTimerRestart((timer) => !timer);
    }, 900);
  }, [timerRestart]);

  return (
    <svg
      xmlns="http://www.w3.org/2000/svg"
      fill="none"
      viewBox="0 0 24 24"
      strokeWidth={1.5}
      stroke="currentColor"
      className={`size-6 ${red && "text-red-300"}`}
    >
      <path
        strokeLinecap="round"
        strokeLinejoin="round"
        d="M16.5 3.75V16.5L12 14.25 7.5 16.5V3.75m9 0H18A2.25 2.25 0 0 1 20.25 6v12A2.25 2.25 0 0 1 18 20.25H6A2.25 2.25 0 0 1 3.75 18V6A2.25 2.25 0 0 1 6 3.75h1.5m9 0h-9"
      />
    </svg>
  );
};

export default UpdatingIcon;
