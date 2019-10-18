import express from 'express';
import { FontLoader } from './font';

const cosmos = {
    endpoint: process.env['CosmosDb_EndpointUrl']!,
    key: process.env['CosmosDb_AuthorizationKey']!,
    databaseId: 'QuickDraw',
    datasetContainerId: 'Drawings',
    sumamryContainerId: 'Summary',
};
const fontLoader = new FontLoader(cosmos);

export default (req: express.Request, res: express.Response, next: express.NextFunction) => {
    fontLoader.create().then(ab => {
        res.set('content-type', 'application/x-font-otf');
        res.set('access-control-allow-origin', '*');
        res.send(Buffer.from(ab));
        res.end();
    }).catch(next);
};
