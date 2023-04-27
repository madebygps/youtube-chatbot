using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;

string endpoint = System.Environment.GetEnvironmentVariable("searchEndpoint");
string adminApiKey = System.Environment.GetEnvironmentVariable("searchApiKey");
string indexName = "videoindex2";


var indexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(adminApiKey));

var indexDefinition = new SearchIndex(indexName)
{
    Fields =
    {
     
        new SearchableField("video_id") { IsKey = true},
        new SearchableField("title") { IsSortable = true },
        new SearchableField("transcript"),
        new SearchableField("key_phrases") { IsFilterable = true },
        new SearchableField("sentiment") { 
            col= true },
    }
};

indexClient.CreateIndex(indexDefinition);
