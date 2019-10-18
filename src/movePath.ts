import { vec2 } from "gl-matrix";
import { Path, Line } from "./models";

function* pathToLines([xs, ys]: Path): IterableIterator<Line> {
    if (xs.length <= 1) return;
    for (let i = 0; i < xs.length - 1; i++) {
        yield [[xs[i], xs[i + 1]], [ys[i], ys[i + 1]]];
    }
}

function moveLine([xs, ys]: Line, offset: number): Line {
    const
        [x0, x1] = xs,
        [y0, y1] = ys,
        norm = vec2.normalize(vec2.create(), [y1 - y0, x0 - x1]),
        [x2, y2] = vec2.scaleAndAdd(vec2.create(), [x0, y0], norm, offset),
        [x3, y3] = vec2.scaleAndAdd(vec2.create(), [x1, y1], norm, offset);
    return [[x2, x3], [y2, y3]];
}

function mergeLinesToPath(lines: Iterable<Line>): Path {
    const xResult: number[] = [];
    const yResult: number[] = [];
    for (const [xs, ys] of lines) {
        xResult.push(...xs);
        yResult.push(...ys);
    }
    return [xResult, yResult];
}

function movePath(path: Path, offset: number): Path {
    return mergeLinesToPath(function*() {
        for (const line of pathToLines(path))
            yield moveLine(line, offset);
    }());
}

export default movePath;
