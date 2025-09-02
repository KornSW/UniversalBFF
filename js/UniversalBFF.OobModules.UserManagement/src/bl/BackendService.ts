import axios from "axios";
import { FilterOptions } from "./FilterOptions";
import { LogicalExpression } from "fusefx-repositorycontract";
import { useState } from "react";

const windowU: any = window;

export class BackendService {

  //public static baseUrl: string = windowU.backendServiceUrl;

  public static baseUrl: string = "http://localhost:45357";

  public static tokenSourceUid: string = "00000000-0000-0000-0000-111111111111";

  public static getTokenMethod: (
    tokenSourceUid: string
  ) => Promise<{ token: string; content: any } | null>;

  public static underscore: any = {};
  static async pingServices() {
    const token: any = await this.getTokenMethod(
      "00000000-0000-0000-0000-111111111111"
    );
    console.log(token, "token");
    return axios
      .post(
        `${BackendService.baseUrl}/api/SendForAvailabilityStateUpdates`,
        { _: this.underscore },
        {
          responseType: "json",
          headers: {
            Authorization: `${token.token}`,
          },
        }
      )
      .then((r) => {
        // const result = r.data.return;
        // return result;
      });
  }
  static async getAllZones(): Promise<string[]> {
    const token: any = await this.getTokenMethod(
      "00000000-0000-0000-0000-111111111111"
    );
    console.log(token, "token");
    return axios
      .post(
        `${BackendService.baseUrl}/api/GetAllZones`,
        { _: this.underscore },
        {
          responseType: "json",
          headers: {
            Authorization: `${token.token}`,
          },
        }
      )
      .then((r) => {
        const result = r.data.return;
        return result;
      });
  }

  static async addOrUpdateStoryEntry(entry: any): Promise<any> {
    const token: any = await this.getTokenMethod(
      "00000000-0000-0000-0000-111111111111"
    );
    console.log(token, "token");
    if (!entry.LastUpdateUtc) {
      entry.LastUpdateUtc = new Date().toISOString();
    }
    if (!entry.AvailabilityState) {
      entry.AvailabilityState = 0;
    }
    return axios
      .post(
        `${BackendService.baseUrl}/api/AddOrUpdateXXXXXXXXXXStoreEntry`,
        { _: this.underscore, entry: entry },
        {
          responseType: "json",
          headers: {
            Authorization: `${token.token}`,
          },
        }
      )
      .then((r) => {
        const result = r.data.return;
        return result;
      });
  }

  static async getResolvedUrls(zone: string): Promise<any[]> {
    const token: any = await this.getTokenMethod(
      "00000000-0000-0000-0000-111111111111"
    );
    console.log(token, "token");
    return axios
      .post(
        `${BackendService.baseUrl}/api/getResolvedUrls`,
        { _: this.underscore, zone: zone },
        {
          responseType: "json",
          headers: {
            Authorization: `${token.token}`,
          },
        }
      )
      .then((r) => {
        const result = r.data.return;
        return result;
      });
  }
}
