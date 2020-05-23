import express from 'express';
import { FontLoader } from './font';
import cosmos from './cosmos';

const fontLoader = new FontLoader(cosmos);

export default (req: express.Request, res: express.Response, next: express.NextFunction) => {
    fontLoader.create().then(ab => {
        res.set('content-type', 'application/x-font-otf');
        res.set('access-control-allow-origin', '*');
        res.send(Buffer.from(ab));
        res.end();
    }).catch(next);
};
