import express from 'express';
import { FontLoader } from './font';

const cosmos = {
    endpoint: process.env['CosmosDb.EndpointUrl']!,
    key: process.env['CosmosDb.AuthorizationKey']!,
    databaseId: 'QuickDraw',
    datasetContainerId: 'Drawings',
    sumamryContainerId: 'Summary',
};
const fontLoader = new FontLoader(cosmos);

const app = express();
app.get('/font.otf', (req, res, next) => {
    fontLoader.create().then(ab => {
        res.set('content-type', 'application/x-font-otf');
        res.set('access-control-allow-origin', '*');
        res.send(Buffer.from(ab));
        res.end();
    }).catch(next);
});
app.listen(process.env.PORT || 8080, () => console.log('listening'));
