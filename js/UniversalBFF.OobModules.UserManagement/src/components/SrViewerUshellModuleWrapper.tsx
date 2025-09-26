import React from "react";
import SrViewerMain from "./SrViewerMain";
import UShellModuleWrapper from "./UshellModuleWrapper";
import {
  IDataSource,
  IDataSourceManagerWidget,
  IWidget,
} from "ushell-modulebase";

const SrViewerUShellWrapper: React.FC<{ inputData: IWidget }> = ({
  inputData,
}) => {
  
  console.log("inputData", inputData);

  const dataSourceManager: IDataSourceManagerWidget = inputData.widgetHost;
  const dataSourceEndpointUrl: IDataSource | null =
    dataSourceManager.tryGetDataSource("XXXEntry");

  console.log("dataSourceManager", dataSourceManager);
  console.log("dataSourceEndpointUrl", dataSourceEndpointUrl);

  if (!dataSourceEndpointUrl) return <div>No DataSource</div>;
  return (
    <UShellModuleWrapper inputData={inputData}>
      <SrViewerMain
        dataSource={dataSourceEndpointUrl}
        dataSourceManager={dataSourceManager}
      ></SrViewerMain>
    </UShellModuleWrapper>
  );
};

export default SrViewerUShellWrapper;
