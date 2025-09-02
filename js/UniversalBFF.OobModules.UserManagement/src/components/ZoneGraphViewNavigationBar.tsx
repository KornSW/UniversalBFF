import React, { useEffect, useMemo, useState } from "react";
import { Graph } from "../bl/graph";
import { BackendService } from "../bl/BackendService";
import TreeView from "ushell-common-components/dist/cjs/_Molecules/TreeView";
import { TreeView1 } from "ushell-common-components/dist/cjs/_Molecules/TreeView";
import BarIcon from "../icons/BarIcon";
import { TreeIcon } from "../icons/TreeIcon";
import { SwitchOff } from "../icons/SwitchOff";
import { SwitchOn } from "../icons/SwitchOn";
import { AntennaIcon } from "../icons/Antenna";
import ReloadIcon from "../icons/ReloadIcon";
import { EndpointUrldataSource } from "../bl/EndpointUrlDataSource";

class TreeNode1 {
  id: any;
  children: TreeNode1[] = [];
  name: string = "";
  type: "folder" | "file" = "file";
  render: (column: string) => JSX.Element = (c) => <div>{c}</div>;
  onSelected?: () => void;
  parent: TreeNode1 | null = null;
  constructor() {}
}

export const ZoneGraphViewNavigationBar: React.FC<{
  zone: string | null;
  setZone: (z: string | null) => void;
  onReload: () => void;
}> = ({ zone, setZone, onReload }) => {
  //###useStates###
  const [treeNodes, setTreeNodes] = useState<TreeNode1[]>([]);

  const [isGraphVisible, setIsGraphVisible] = useState<boolean>(false);

  const [graphActive, setGraphActive] = useState<boolean>(false);

  const [currentZone, setCurrentZone] = useState<string | null>(null);

  //###useEffects###
  useEffect(() => {
    BackendService.getAllZones().then((zones) => {
      setTreeNodes(buildTreeNodes(zones));
    });
  }, []);

  useEffect(() => {
    reload();
  }, [graphActive]);

  //###functions###

  function buildTreeNodes(zones: string[]): TreeNode1[] {
    const result: TreeNode1[] = [];

    let i = 0;
    for (let zone of zones) {
      const path: string[] = zone.split("/");
      i = pushIntoNodes(null, path, result, i) + 1;
    }
    setTreeNodes(result);
    return result;
  }

  function pushIntoNodes(
    parentNode: TreeNode1 | null,
    path: string[],
    treeNodes: TreeNode1[],
    i: number
  ): number {
    if (path.length == 0) return i;

    const pathEntry: string = path[0];
    const remainingPath: string[] = path.slice(1);
    let node: TreeNode1 | undefined = treeNodes.find(
      (tn) => tn.name == pathEntry
    );
    if (!node) {
      node = new TreeNode1();
      node.name = pathEntry;
      node.id = pathEntry + "_" + i;
      node.children = [];
      node.parent = parentNode;
      node.onSelected = () => {
        let fullPath = pathEntry;
        let p: TreeNode1 | null = parentNode;
        while (p) {
          fullPath = p.name + "/" + fullPath;
          p = p.parent;
        }
        setZone(fullPath);
        setCurrentZone(fullPath);
        setGraphActive(true);
        onReload();
      };
      node.type = "file";
      node.render = () => <div>{pathEntry}</div>;
      treeNodes.push(node);
    }
    i = pushIntoNodes(node, remainingPath, node.children, i + 1) + 1;
    return i;
  }

  function pingServices() {
    BackendService.pingServices();
  }

  function reload() {

    console.log("reload");
    
    // if (!graphActive) {
    //   setZone(currentZone);
    //   setZone(null);
    // } else {
    //   setZone(null);
    //   setZone(currentZone);
    // }
    !graphActive ? setZone(null) : setZone(currentZone);
    onReload();
  }

  function reload2() {
    console.log("reload");
    if (!graphActive) {
      setZone(currentZone);
      setZone(null);
    } else {
      setZone(null);
      setZone(currentZone);
    }
  }

  return (
    <div className="flex flex-col h-full">
      <div className="flex justify-between text-lg">
        <div className="flex gap-5">
          <button
            className="hover:text-blue-400"
            onClick={() => setIsGraphVisible((old) => !old)}
          >
            <TreeIcon></TreeIcon>
          </button>
          {isGraphVisible && (
            <button
              className="hover:text-blue-400"
              onClick={() => setGraphActive((old) => !old)}
            >
              {graphActive ? <SwitchOn></SwitchOn> : <SwitchOff></SwitchOff>}
            </button>
          )}
        </div>

        {isGraphVisible && (
          <div className="flex gap-5">
            <button
              className=" hover:text-blue-400"
              onClick={() => {
                onReload();
              }}
            >
              <ReloadIcon></ReloadIcon>
            </button>
            <button
              className="right-0 hover:text-blue-400"
              onClick={() => pingServices()}
            >
              <AntennaIcon></AntennaIcon>
            </button>
          </div>
        )}
      </div>

      {isGraphVisible && (
        <div
          className={`w-72 h-full flex overflow-auto border-0 border-red-300`}
        >
          <TreeView nodes={treeNodes}></TreeView>
        </div>
      )}
    </div>
  );
};
