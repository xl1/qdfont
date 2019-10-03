namespace CosmosDbUploader.Models
{
    public class Drawing
    {
        public string? id { get; set; }
        public string? word { get; set; }
        public string? key_id { get; set; }
        public bool recognized { get; set; }
        public object? drawing { get; set; }
    }
}
