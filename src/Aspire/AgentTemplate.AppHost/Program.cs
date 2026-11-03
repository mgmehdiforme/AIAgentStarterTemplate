var builder = DistributedApplication.CreateBuilder(args);

// Distributed cache & rate limiting store
var cache = builder.AddRedis("cache");

// Vector database for semantic search / RAG
var vectorDb = builder.AddQdrant("vectordb");

// Local LLM engine via Ollama container integration
var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();

var llama = ollama.AddModel("llama3.2");

// API Host service
var api = builder.AddProject<Projects.AgentTemplate_Api>("agent-api")
    .WithReference(cache)
    .WithReference(vectorDb)
    .WithReference(llama)
    .WithExternalHttpEndpoints();

builder.Build().Run();
