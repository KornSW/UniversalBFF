import React from "react";

const ErrorState: React.FC<{ message: string }> = ({ message }) => {
  return (
    <div className="m-4 p-4 rounded border border-red-400 text-red-700 dark:text-red-300" role="alert">
      {message}
    </div>
  );
};

export default ErrorState;
