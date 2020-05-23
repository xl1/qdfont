export default {
    endpoint: process.env['CosmosDb_EndpointUrl']!,
    key: process.env['CosmosDb_AuthorizationKey']!,
    databaseId: 'QuickDraw',
    datasetContainerId: 'Drawings',
    sumamryContainerId: 'Summary',
};
