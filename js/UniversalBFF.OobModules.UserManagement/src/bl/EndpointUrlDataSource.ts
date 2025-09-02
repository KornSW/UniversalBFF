import { EntitySchema } from "fusefx-modeldescription";

import { IDataSource } from "ushell-modulebase";
import { BackendService } from "./BackendService";
import {
  LogicalExpression,
  PagingParams,
  SortingField,
  PaginatedList,
} from "fusefx-repositorycontract";
import {
  applyFilter,
  applySorting,
  applyPaging,
} from "ushell-common-components";

export class EndpointUrldataSource implements IDataSource {
  private innerDataSource: IDataSource;
  private zone: string | null = null;
  private reload: boolean = false;

  constructor(innerDataSource: IDataSource, zone: string | null) {
    this.zone = zone;
    this.innerDataSource = innerDataSource;
    this.dataSourceUid = crypto.randomUUID();
    // this.getRecords = innerDataSource.getRecords;
    this.getEntityRefs = innerDataSource.getEntityRefs;
    this.containsIdentityOf = innerDataSource.containsIdentityOf;
    this.entityDeleteMethod = innerDataSource.entityDeleteMethod;
    this.entityFactoryMethod = innerDataSource.entityFactoryMethod;
    this.entityInsertMethod = this.addOrUpdate;
    this.entityUpdateMethod = this.addOrUpdate;
    this.entitySchema = innerDataSource.entitySchema;
    this.reload = false;
  }

  dataSourceUid: string;
  entitySchema?: EntitySchema | undefined;
  entityFactoryMethod: () => any;
  entityUpdateMethod: (entity: any[]) => Promise<boolean>;
  entityInsertMethod: (entity: any[]) => Promise<boolean>;
  entityDeleteMethod: (entity: any[]) => Promise<boolean>;
  extractIdentityFrom(entity: object): object {
    throw new Error("Method not implemented.");
  }
  containsIdentityOf(entity: object): Promise<boolean> {
    throw new Error("Method not implemented.");
  }

  addOrUpdate(entry: any): Promise<any> {
    return BackendService.addOrUpdateStoryEntry(entry);
  }

  getRecords(
    filter?: LogicalExpression,
    pagingParams?: PagingParams,
    sortingParams?: SortingField[]
  ): Promise<PaginatedList> {
    console.log("getRecords", filter);
    if (this.zone) {
      return BackendService.getResolvedUrls(this.zone).then((urls: any[]) => {
        let result = filter
          ? applyFilter(urls, filter, this.entitySchema!)
          : urls;
        result = sortingParams
          ? applySorting(
              result,
              sortingParams,
              sortingParams.map((sp) => {
                return {
                  fieldType: this.entitySchema!.fields.find(
                    (f) => f.name == sp.fieldName
                  )!.type,
                };
              })
            )
          : result;
        const total = result.length;
        result = pagingParams ? applyPaging(result, pagingParams) : result;
        return { page: result, total: total };
      });
    } else {
      return BackendService.getResolvedUrls("").then((urls: any[]) => {
        let result = filter
          ? applyFilter(urls, filter, this.entitySchema!)
          : urls;
        console.log("result after filter", result);
        result = sortingParams
          ? applySorting(
              result,
              sortingParams,
              sortingParams.map((sp) => {
                return {
                  fieldType: this.entitySchema!.fields.find(
                    (f) => f.name == sp.fieldName
                  )!.type,
                };
              })
            )
          : result;
        const total = result.length;
        result = pagingParams ? applyPaging(result, pagingParams) : result;
        console.log("final result", filter);
        return { page: result, total: total };
      });
      // return this.innerDataSource.getRecords(
      //   filter,
      //   pagingParams,
      //   sortingParams
      // );
    }
  }
  getRecord(identityFields: object): Promise<object> {
    throw new Error("Method not implemented.");
  }
  getEntityRefs(
    filter?: LogicalExpression,
    pagingParams?: PagingParams,
    sortingParams?: SortingField[]
  ): Promise<PaginatedList> {
    throw new Error("Method not implemented.");
  }
}
