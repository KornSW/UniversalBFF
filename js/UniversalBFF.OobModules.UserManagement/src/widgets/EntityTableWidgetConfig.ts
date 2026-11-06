import React from "react";
import { EntityTable } from "ushell-common-components";

/** the layout description type, as consumed by the CommonComponents 'EntityTable' */
export type LayoutDescription = NonNullable<React.ComponentProps<typeof EntityTable>["layoutDescription"]>;

/**
 * The UnitOfWork contract of the 'EntityTableWidget' (= its configuration within 'module.json').
 *
 * It is a SUPERSET of the contract of the CommonComponents 'EntityTableModule'
 * ('entityName', 'layoutDescription', 'pageSizes' - see 'ushell-design-debt' DD-02),
 * so that a use case can switch to a built-in widget class later without changing its configuration.
 */
export interface EntityTableWidgetConfig {
  /** name of the entity (as declared in the schema of a datastore of this module) */
  entityName: string;

  /** optional: the key of the datastore (only needed if the entity name is ambiguous) */
  storeName?: string;

  layoutDescription?: LayoutDescription;

  pageSizes?: number[];

  /** default: true */
  enableCrud?: boolean;

  /** default: true */
  enableSearch?: boolean;

  /** default: false */
  enableQueryEditor?: boolean;
}

export const defaultPageSizes: number[] = [50, 100, 200, 500];

export function readEntityTableWidgetConfig(unitOfWork: any): EntityTableWidgetConfig | null {
  if (!unitOfWork || typeof unitOfWork.entityName !== "string" || unitOfWork.entityName === "") {
    return null;
  }
  return unitOfWork as EntityTableWidgetConfig;
}
