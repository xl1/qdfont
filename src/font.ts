import { CosmosClient, Database } from '@azure/cosmos';
import * as opentype from 'opentype.js';
import { Drawing, MaxIdSummary, Path, CosmosDbConfg } from './models';
import movePath from './movePath';

const emojis: { [key: string]: string } = require('../docs/data.json');
const fontSize = 255;
const descender = 50;
const strokeWidth = 4;

export class GlyphBuilder {
    private readonly path = new opentype.Path();
    private readonly paths: Path[] = [];
    private xMax = 0;
    private yMax = 0;

    private addPath([xs, ys]: Path) {
        const xShift = (fontSize - this.xMax) / 2;
        const yShift = (fontSize + this.yMax) / 2 - descender;
        this.path.moveTo(xShift + xs[0], yShift - ys[0]);
        for (let i = 1; i < xs.length; i++) {
            this.path.lineTo(xShift + xs[i], yShift - ys[i]);
        }
        this.path.closePath();
    }

    addDrawing([xs, ys]: Path): this {
        if (xs.length === 0) return this;
        this.xMax = Math.max(this.xMax, ...xs);
        this.yMax = Math.max(this.yMax, ...ys);
        this.paths.push([xs, ys]);
        return this;
    }

    build(character: string): opentype.Glyph {
        for (const [xs, ys] of this.paths) {
            const [oxs, oys] = movePath([xs, ys], strokeWidth);
            const [ixs, iys] = movePath([xs.reverse(), ys.reverse()], strokeWidth);
            this.addPath([oxs.concat(ixs), oys.concat(iys)]);
        }

        const unicode = character.codePointAt(0)!;
        return new opentype.Glyph({
            name: `uni${unicode.toString(16).padStart(4, '0')}`,
            advanceWidth: fontSize,
            unicode,
            path: this.path
        });
    }
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
        const glyphs = drawings.map(d => {
            const builder = new GlyphBuilder();
            d.drawing.forEach(builder.addDrawing, builder);
            return builder.build(emojis[d.word]);
        });

        const font = new opentype.Font({
            familyName: 'QuickDraw Emoji',
            styleName: 'Medium',
            unitsPerEm: fontSize,
            ascender: fontSize - descender,
            descender: -descender,
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
