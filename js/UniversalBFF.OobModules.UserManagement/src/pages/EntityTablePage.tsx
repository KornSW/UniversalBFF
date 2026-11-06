import React, { useMemo } from "react";
import { IDataSource, IDataSourceManagerWidget } from "ushell-modulebase";
import { EntityTable } from "ushell-common-components";
import { defaultPageSizes, EntityTableWidgetConfig, LayoutDescription } from "../widgets/EntityTableWidgetConfig";

/**
 * Host-agnostic page (knows no widgetHost): a CommonComponents 'EntityTable'
 * (search, paging, sorting, column filters, CRUD side panel) over the given datasource.
 */
const EntityTablePage: React.FC<{
  config: EntityTableWidgetConfig;
  dataSource: IDataSource;
  dataSourceManager: IDataSourceManagerWidget;
}> = ({ config, dataSource, dataSourceManager }) => {
  const layoutDescription: LayoutDescription | undefined = useMemo(() => {
    return config.layoutDescription;
  }, [config.layoutDescription]);

  return (
    <EntityTable
      dataSource={dataSource}
      entitySchema={dataSource.entitySchema!}
      dataSourceManagerForNavigations={dataSourceManager}
      layoutDescription={layoutDescription}
      pageSizes={config.pageSizes || defaultPageSizes}
      enableCrud={config.enableCrud !== false}
      enableSearch={config.enableSearch !== false}
      enableQueryEditor={config.enableQueryEditor === true}
      enableParentFilter={false}
    />
  );
};

export default EntityTablePage;
