using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using System;
using System.Collections.Generic;

string endpoint = System.Environment.GetEnvironmentVariable("endpoint");
string adminApiKey = System.Environment.GetEnvironmentVariable("searchApiKey");
string indexName = "videoindex";


var indexClient = new SearchIndexClient(new Uri(endpoint), new AzureKeyCredential(adminApiKey));

var indexDefinition = new SearchIndex(indexName)
{
    Fields =
    {
        new SearchableField("video_id") { IsKey = true},
        new SearchableField("title") { IsSortable = true },
        new SearchableField("transcript"),
        new SearchableField("key_phrases") {  },
    }
};

indexClient.CreateIndex(indexDefinition);

var searchClient = new SearchClient(new Uri(endpoint), indexName, new AzureKeyCredential(adminApiKey));

var document = new Dictionary<string, object>
{
    {"video_id", "your_video_id"},
    {"title", "your_video_title"},
    {"transcript", "your_video_transcript"},
    {"key_phrases", new List<string> { "phrase1", "phrase2" }},
};

var batch = IndexDocumentsBatch.Create(IndexDocumentsAction.Upload(document));
searchClient.IndexDocuments(batch);
