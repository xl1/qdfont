import express from 'express';
import fontHandler from './index';

const app = express();
app.get('/font.otf', fontHandler);
app.listen(process.env.PORT || 8080, () => console.log('listening'));
