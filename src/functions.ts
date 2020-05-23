import { Context } from '@azure/functions';
import { FontLoader } from './font';
import cosmos from './cosmos';

const fontLoader = new FontLoader(cosmos);
let font = fontLoader.create();

export default async function (context: Context) {
    context.res = {
        status: 200,
        headers: {
            'content-type': 'application/x-font-otf',
            'access-control-allow-origin': '*',
        },
        isRaw: true,
        body: new Uint8Array(await font)
    };
    font = fontLoader.create();
};
