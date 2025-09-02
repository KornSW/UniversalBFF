import React from "react";

import { QueryClient, QueryClientProvider } from "react-query";

import { IWidget } from "ushell-modulebase";
import { BackendService } from "../bl/BackendService";

const queryClient = new QueryClient();

function getApplicationScopeValues(appScope: any): {
  [dimension: string]: any;
} {
  if (!appScope) return {};
  const result: any = {};
  Object.keys(appScope).forEach((apk) => {
    result[apk] = appScope[apk].value;
  });
  return result;
}

const UShellModuleWrapper: React.FC<{ inputData: IWidget; children: any }> = ({
  inputData,
  children,
}) => {
  const uow: any = inputData?.state?.unitOfWork;
  if (uow.backendApiUrl) {
    BackendService.baseUrl = uow.backendApiUrl;
    // this for local execution
    // AfsService.baseUrl = "http://localhost:44351/v1/yyyService";
    // setBackendApiUrl("http://localhost:44351/v1/yyyService");
  }
  const wh: any = inputData.widgetHost;
  BackendService.getTokenMethod = inputData.widgetHost.getAccessToken;
  // AfsService.underscore = wh.getApplicationScopeValues();
  //   AfsService.underscore = getApplicationScopeValues(wh.getApplicationScope());
  return (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
};

export default UShellModuleWrapper;
