import { CosmosClient, Database } from '@azure/cosmos';
import * as opentype from 'opentype.js';
import { Drawing, MaxIdSummary, Path, CosmosDbConfg } from './models';
import movePath from './movePath';

const emojis: { [key: string]: string } = require('../docs/data.json');
const fontSize = 255;
const strokeWidth = 4;

function drawPath(opath: opentype.Path, [xs, ys]: Path) {
    if (xs.length === 0) return;
    opath.moveTo(xs[0], fontSize - ys[0]);
    for (let i = 1; i < xs.length; i++) {
        opath.lineTo(xs[i], fontSize - ys[i]);
    }
    opath.closePath();
}

export class FontLoader {
    private readonly database: Database;
    private maxIdSummary: MaxIdSummary | undefined;

    constructor(private cosmos: CosmosDbConfg) {
        const client = new CosmosClient(cosmos);
        this.database = client.database(cosmos.databaseId);
    }

    private async readAsync<T>(container: string, partition: string, id: string) {
        const item = this.database.container(container).item(id, partition);
        const { resource } = await item.read<T>();
        return resource;
    }

    private async fetchMaxIdSumamry(): Promise<MaxIdSummary> {
        if (this.maxIdSummary) {
            return this.maxIdSummary;
        }
        return this.maxIdSummary = await this.readAsync<MaxIdSummary>(
            this.cosmos.sumamryContainerId, 'maxId', '0');
    }

    private async readDrawingsAsync(params: { word: string, id: number }[]): Promise<Drawing[]>{
        const container = this.database.container(this.cosmos.datasetContainerId);
        const condition = params.map(({ word, id }) => {
            const idString = id.toString().padStart(8, '0');
            return `(d.word = "${word}" AND d.id = "${idString}")`;
        }).join(' OR ');
        const query = 'SELECT * FROM Drawings d WHERE ' + condition;
        const { resources } = await container.items.query<Drawing>(query, {}).fetchAll();
        return resources;
    }

    private createGlyph(drawing: Drawing, emoji: string) {
        const path = new opentype.Path();
        for (const [xs, ys] of drawing.drawing) {
            const outer = movePath([xs, ys], strokeWidth);
            drawPath(path, outer);
            const inner = movePath([xs.reverse(), ys.reverse()], strokeWidth);
            drawPath(path, inner);
        }

        const unicode = emoji.codePointAt(0)!;
        return new opentype.Glyph({
            name: `uni${unicode.toString(16).padStart(4, '0')}`,
            advanceWidth: fontSize,
            unicode,
            path,
        });
    }

    public async create(): Promise<ArrayBuffer> {
        const maxIdSummary = await this.fetchMaxIdSumamry();
        const params = Object.entries(emojis)
            .filter(([word, emoji]) => emoji)
            .map(([word]) => {
                const maxId = maxIdSummary.value[word];
                const id = Math.random() * (1 + maxId) | 0;
                return { word, id };
            });

        const drawings = await this.readDrawingsAsync(params);
        const glyphs = drawings.map(d => this.createGlyph(d, emojis[d.word]))
        const font = new opentype.Font({
            familyName: 'QuickDraw Emoji',
            styleName: 'Medium',
            unitsPerEm: fontSize,
            ascender: fontSize - 50,
            descender: -50,
            glyphs: [
                new opentype.Glyph({
                    name: '.notdef',
                    unicode: 0,
                    advanceWidth: fontSize,
                    path: new opentype.Path()
                }),
                ...glyphs
            ]
        });

        return font.toArrayBuffer();
    }
}
