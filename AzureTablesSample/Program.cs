using AzureTablesSample;

static class Program
{
    static async Task Main()
    {
        TableSample tableSample = new TableSample();
        await tableSample.QueryEntitiesAsync();

        StorageCRUD storageCRUD = new StorageCRUD();
        //storageCRUD.InsertEntity("urlshortener", "test artist", "test title");
        storageCRUD.GetEntity("urlshortener", "pKey", "row");

    }
}