export class Graph {
  public label: string = "";
  public predecessor: Graph | null = null;
  public successors: Graph[] = [];
}
