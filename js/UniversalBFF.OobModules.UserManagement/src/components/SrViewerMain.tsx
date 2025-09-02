import React, { useEffect, useMemo, useState } from "react";
import { EntityTable, Table } from "ushell-common-components";
import { TableColumn } from "ushell-common-components/dist/esm/components/guifad/_Organisms/Table";
import { IDataSource, IDataSourceManagerWidget } from "ushell-modulebase";
import { ZoneGraphViewNavigationBar } from "./ZoneGraphViewNavigationBar";
import { EndpointUrldataSource } from "../bl/EndpointUrlDataSource";
import { EntityLayout } from "ushell-common-components/dist/esm/[Move2LayoutDescription]/EntityLayout";

const SrViewerMain: React.FC<{
  dataSourceManager: IDataSourceManagerWidget;
  dataSource: IDataSource;
}> = ({ dataSource, dataSourceManager }) => {
  const [zone, setZone] = useState<string | null>(null);

  const [reload, setReload] = useState<boolean>(false);

  const entityEndpointUrlLayout: EntityLayout = new EntityLayout(
    "XXXXXEntry"
  );
  entityEndpointUrlLayout.partitions = [
    {
      fields: [
        "Url",
        "InfrastructureScopeName",
        "HostingOriginName",
        "QualifiedContractNameAndVersion",
      ],
      type: "group",
      children: [],
      name: "",
    },
  ];
  entityEndpointUrlLayout.dislpayRemainingFields = false;
  const datasource = new EndpointUrldataSource(dataSource, zone);
  entityEndpointUrlLayout.tableFields = [
    "HostingOrigin",
    "HostingOriginName",
    "Url",
    "QualifiedContractNameAndVersion",
    "InfrastructureScopeName",
    "LastUpdateTimestampUtc2",
    "AvailabilityState2",
  ];
  entityEndpointUrlLayout.fieldLayouts = [
    {
      displayLabel: "Contract",
      fieldName: "QualifiedContractNameAndVersion",
    },
    {
      displayLabel: "Zone",
      fieldName: "InfrastructureScopeName",
    },
  ];

  //###functions###

  function mapStateIntValueToName(intValue: number, lastAccess: string): String {
    const past = new Date(lastAccess);
    if(past.getFullYear() <= 2000) {
      return "[Not monitored]";
    }
    if (intValue === 0) {
      return "Online (idle)";
    } else if (intValue > 0) {
      return `Online (load: ${intValue})`;
    } else if (intValue === -2) {
      return "Downtime";
    } else {
      return "No Response";
    }
  }

  function getStateColor(state: number, lastAccess: string): string {
    const past = new Date(lastAccess);
    if(past.getFullYear() <= 2000) {
      return "text-gray-500";
    }
    if (state >= 0) {
      return "text-green-500";
    }
    if (state !== -2) {
      return "text-red-500";
    }
    return "";
  }

  function getLastAccessHumanReadable(lastAccess: string) {
    let now = new Date();
    now = new Date(
      now.getUTCFullYear(),
      now.getUTCMonth(),
      now.getUTCDate(),
      now.getUTCHours(),
      now.getUTCMinutes(),
      now.getUTCSeconds(),
      now.getUTCMilliseconds()
    );
    const past = new Date(lastAccess);

    if(past.getFullYear() <= 2000) {
      return "-";
    }

    const diffInSeconds = Math.floor((now.getTime() - past.getTime()) / 1000);

    const years = Math.floor(diffInSeconds / (3600 * 24 * 365));
    if (years > 0) return `${years} year${years > 1 ? "s" : ""} ago`;

    const days = Math.floor(diffInSeconds / (3600 * 24));
    if (days > 0) return `${days} day${days > 1 ? "s" : ""} ago`;

    const hours = Math.floor(diffInSeconds / 3600);
    if (hours > 0) return `${hours} hour${hours > 1 ? "s" : ""} ago`;

    const minutes = Math.floor(diffInSeconds / 60);
    if (minutes > 0) return `${minutes} minute${minutes > 1 ? "s" : ""} ago`;

    return `${diffInSeconds} second${diffInSeconds > 1 ? "s" : ""} ago`;
    // let currentTime: Date = new Date();
    // Date.UTC(
    //   currentTime.getUTCFullYear(),
    //   currentTime.getUTCMonth(),
    //   currentTime.getUTCDate(),
    //   currentTime.getUTCHours(),
    //   currentTime.getUTCMinutes(),
    //   currentTime.getUTCSeconds()
    // );
    // // const lastAccesLocalDate: string = new Date(
    // //   lastAccess
    // // ).toLocaleDateString();
    // const lastAccessAsDate: Date = new Date(lastAccess);
    // console.log("lastAcces", lastAccess);
    // console.log("lastAccesDate", lastAccessAsDate);
    // const yearDiff: number =
    //   currentTime.getFullYear() - lastAccessAsDate.getFullYear();
    // if (yearDiff > 0) {
    //   return `${yearDiff}y ago`;
    // }
    // const dayDiff: number = currentTime.getDate() - lastAccessAsDate.getDate();
    // if (dayDiff > 0) {
    //   return `${dayDiff}d ago`;
    // }
    // const hourDiff: number =
    //   currentTime.getHours() - lastAccessAsDate.getHours();
    // const minDiff: number =
    //   currentTime.getMinutes() - lastAccessAsDate.getMinutes();
    // if (hourDiff > 0 && minDiff) {
    //   console.log("hourDiff", hourDiff);
    //   return `${hourDiff}h ago`;
    // }
    // if (minDiff > 0) {
    //   console.log("minDiff", minDiff);
    //   return `${minDiff}m ago`;
    // }
    // const secDiff: number =
    //   currentTime.getSeconds() - lastAccessAsDate.getSeconds();
    // return `${secDiff}s ago`;
    //     Dim ts As TimeSpan = DateTime.UtcNow.Subtract(origin.LastUpdateUtc)
    // If (ts.TotalMinutes < 0) Then
    //   newItem.SubItems.Add($"{(ts.TotalSeconds \ 1)} s. ago")
    // ElseIf (ts.TotalHours < 2) Then
    //   newItem.SubItems.Add($"{(ts.TotalMinutes \ 1)} m. ago")
    // ElseIf (ts.TotalDays < 2) Then
    //   newItem.SubItems.Add($"{(ts.TotalHours \ 1)} h. ago")
    // Else
    //   newItem.SubItems.Add($"{(ts.TotalDays \ 1)} d. ago")
    // End If
  }

  function onReload() {
    setReload((old) => !old);
  }

  return (
    <div className="flex flex-col h-full overflow-hidden">
      <div className="flex  h-full overflow-hidden ">
        <div className="p-4 border dark:border-gray-800 h-full flex overflow-auto1">
          <ZoneGraphViewNavigationBar
            zone={zone}
            setZone={setZone}
            onReload={onReload}
          ></ZoneGraphViewNavigationBar>
        </div>
        <EntityTable
          //enableQueryEditor={false}
          dataSource={datasource}
          entitySchema={dataSource.entitySchema!}
          dataSourceManagerForNavigations={dataSourceManager}
          minWidthInput={40}
          reloadTriggerObject={reload}
          enableParentFilter={false}
          layoutDescription={{
            entityLayouts: [entityEndpointUrlLayout],
          }}
          customColumns={[
            {
              fieldName: "AvailabilityState2",
              fieldType: "string",
              key: "AvailabilityState2",
              label: "State",
              onRenderCell: (cv, r) => (
                <div className={`${getStateColor(r.AvailabilityState, r.LastUpdateTimestampUtc)} `}>
                  {mapStateIntValueToName(r.AvailabilityState, r.LastUpdateTimestampUtc)}
                </div>
              ),
            },
            {
              fieldName: "LastUpdateTimestampUtc2",
              fieldType: "string",
              key: "LastUpdateTimestampUtc2",
              label: "Last Update",
              onRenderCell: (cv, r) => {
                return (
                  <div className={` `}>
                    {getLastAccessHumanReadable(r.LastUpdateTimestampUtc)}
                  </div>
                );
              },
            },
          ]}
        ></EntityTable>
      </div>
    </div>
  );
};
export default SrViewerMain;
