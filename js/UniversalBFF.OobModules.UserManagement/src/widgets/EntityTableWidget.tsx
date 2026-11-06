import React, { useMemo } from "react";
import { IDataSource, IWidget } from "ushell-modulebase";
import { readEntityTableWidgetConfig, EntityTableWidgetConfig } from "./EntityTableWidgetConfig";
import EntityTablePage from "../pages/EntityTablePage";
import ErrorState from "../components/ErrorState";
import "./widgets.css";

/**
 * Module entry component (exposed as './EntityTableWidget').
 * Generic and configuration-driven: everything entity specific comes from the UnitOfWork
 * (see 'EntityTableWidgetConfig'), which is declared in 'module.json'.
 * PROMOTION CANDIDATE: should become a built-in widget class of the shell (DD-04).
 */
const EntityTableWidget: React.FC<{ inputData: IWidget }> = ({ inputData }) => {
  const config: EntityTableWidgetConfig | null = readEntityTableWidgetConfig(inputData.state.unitOfWork);

  const entityName: string | undefined = config?.entityName;
  const storeName: string | undefined = config?.storeName;

  const dataSource: IDataSource | null = useMemo(() => {
    if (!entityName) {
      return null;
    }
    return inputData.widgetHost.tryGetDataSource(entityName, storeName);
  }, [inputData.widgetHost, entityName, storeName]);

  if (!config) {
    return <ErrorState message="Invalid use case configuration: the UnitOfWork does not contain an 'entityName'." />;
  }
  if (!dataSource || !dataSource.entitySchema) {
    return <ErrorState message={`No datasource found for entity '${config.entityName}' (is it declared by a datastore of the module?).`} />;
  }

  return <EntityTablePage config={config} dataSource={dataSource} dataSourceManager={inputData.widgetHost} />;
};

export default EntityTableWidget;
