// Stand-alone page of the bundle - for local debugging only, never loaded by the shell.
// The widgets of this bundle are exposed via module federation (see webpack.config.js)
// and are referenced by the 'widgetClass' of the use cases in 'module.json'.
import React from "react";
import ReactDOM from "react-dom/client";
import "./index.css";

const StandaloneInfo: React.FC = () => {
  return (
    <div className="p-6 font-sans">
      <h1 className="text-xl font-semibold mb-2">User Management - U-Shell module bundle</h1>
      <p className="mb-2">
        This bundle only provides federated widgets. Run it inside a U-Shell (e.g. hosted by UniversalBFF).
      </p>
      <ul className="list-disc ml-6">
        <li>
          <code>remoteEntry.js</code> (federation scope <code>oobUsrMgmt</code>)
        </li>
        <li>
          <code>./EntityTableWidget</code>
        </li>
      </ul>
    </div>
  );
};

const root: ReactDOM.Root = ReactDOM.createRoot(document.getElementById("root") as HTMLElement);
root.render(
  <React.StrictMode>
    <StandaloneInfo />
  </React.StrictMode>
);
