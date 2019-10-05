namespace CosmosDbUploader.Cosmos
{
    public class DbConfig
    {
        public string EndpointUrl { get; set; } = "";
        public string AuthorizationKey { get; set; } = "";
        public string DatabaseId { get; set; } = "";
        public string DatasetContainerId { get; set; } = "";
    }
}
