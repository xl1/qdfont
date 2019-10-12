export type Path = [number[], number[]]; // [[x1, ..., xn], [y1, ..., yn]]
export type Line = [[number, number], [number, number]];

export interface Drawing {
    id: string;
    word: string;
    recognized: boolean;
    drawing: Path[]
}

export interface MaxIdSummary {
    type: 'maxId';
    value: {
        [key: string]: number
    }
}

export interface CosmosDbConfg {
    endpoint: string;
    key: string;
    databaseId: string;
    datasetContainerId: string;
    sumamryContainerId: string;
}
