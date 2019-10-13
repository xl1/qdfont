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

    private fetchDrawing(word: string, id: number) {
        return this.readAsync<Drawing>(
            this.cosmos.datasetContainerId, word, id.toString().padStart(8, '0'));
    }

    private async fetchMaxIdSumamry(): Promise<MaxIdSummary> {
        if (this.maxIdSummary) {
            return this.maxIdSummary;
        }
        return this.maxIdSummary = await this.readAsync<MaxIdSummary>(
            this.cosmos.sumamryContainerId, 'maxId', '0');
    }

    private async createGlyph(key: string, id: number, emoji: string) {
        const drawing = await this.fetchDrawing(key, id);

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
        const promises = Object.entries(emojis)
            .filter(([key, emoji]) => emoji)
            .map(([key, emoji]) => {
                const maxId = maxIdSummary!.value[key];
                const id = Math.random() * (1 + maxId) | 0;
                return this.createGlyph(key, id, emoji);
            });

        const glyps = await Promise.all(promises);
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
                ...glyps
            ]
        });

        return font.toArrayBuffer();
    }
}
