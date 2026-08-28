// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();

string endpoint = Environment.GetEnvironmentVariable("MOCK_ARM_ENDPOINT") ?? "http://127.0.0.1:5050";
builder.WebHost.UseUrls(endpoint);

var app = builder.Build();
var resources = new ConcurrentDictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
var requestCounts = new ConcurrentDictionary<string, long>(StringComparer.OrdinalIgnoreCase);

app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/__mock"))
    {
        string key = $"{context.Request.Method} {context.Request.Path}";
        requestCounts.AddOrUpdate(key, 1, (_, count) => count + 1);
    }
    await next();
});

app.MapGet("/__mock/health", () => Results.Ok(new { status = "ready" }));
app.MapGet("/__mock/stats", () => Results.Json(new
{
    total = requestCounts.Values.Sum(),
    requests = requestCounts.OrderBy(pair => pair.Key).ToDictionary(pair => pair.Key, pair => pair.Value)
}));
app.MapPost("/__mock/reset", () =>
{
    resources.Clear();
    requestCounts.Clear();
    return Results.NoContent();
});

app.MapMethods("/{**resourcePath}", new[] { "PUT" }, async (HttpRequest request, string resourcePath) =>
{
    string id = "/" + resourcePath.TrimStart('/');
    JsonObject resource = await ReadResourceAsync(request);
    CompleteResource(resource, id);
    resources[id] = (JsonObject)resource.DeepClone();
    return ArmJson(resource);
});

app.MapMethods("/{**resourcePath}", new[] { "GET" }, (string resourcePath) =>
{
    string id = "/" + resourcePath.TrimStart('/');
    if (id.EndsWith("/vmSizes", StringComparison.OrdinalIgnoreCase))
    {
        return ArmJson(new JsonObject
        {
            ["value"] = new JsonArray
            {
                new JsonObject { ["name"] = "Standard_D2_v3", ["numberOfCores"] = 2, ["memoryInMB"] = 8192, ["maxDataDiskCount"] = 4, ["osDiskSizeInMB"] = 1047552, ["resourceDiskSizeInMB"] = 51200 },
                new JsonObject { ["name"] = "Standard_D4_v3", ["numberOfCores"] = 4, ["memoryInMB"] = 16384, ["maxDataDiskCount"] = 8, ["osDiskSizeInMB"] = 1047552, ["resourceDiskSizeInMB"] = 102400 }
            }
        });
    }
    if (resources.TryGetValue(id, out JsonObject resource))
    {
        return ArmJson(resource);
    }

    // Some management clients retrieve subscription metadata while initializing.
    string[] segments = id.Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (segments.Length == 2 && segments[0].Equals("subscriptions", StringComparison.OrdinalIgnoreCase))
    {
        return ArmJson(new JsonObject
        {
            ["id"] = id,
            ["subscriptionId"] = segments[1],
            ["displayName"] = "Mock subscription",
            ["state"] = "Enabled"
        });
    }

    return Results.NotFound(new
    {
        error = new
        {
            code = "ResourceNotFound",
            message = $"The mock ARM resource '{id}' was not found."
        }
    });
});

app.MapMethods("/{**resourcePath}", new[] { "DELETE" }, (string resourcePath) =>
{
    string id = "/" + resourcePath.TrimStart('/');
    resources.TryRemove(id, out _);

    string prefix = id.TrimEnd('/') + "/";
    foreach (string childId in resources.Keys.Where(key => key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
    {
        resources.TryRemove(childId, out _);
    }

    return Results.Ok();
});

app.MapMethods("/{**resourcePath}", new[] { "POST" }, (string resourcePath) =>
{
    if (resourcePath.EndsWith("/getStatus", StringComparison.OrdinalIgnoreCase))
    {
        return ArmJson(new JsonObject
        {
            ["name"] = "ssis-ir",
            ["properties"] = new JsonObject { ["type"] = "Managed", ["state"] = "Started", ["typeProperties"] = new JsonObject() }
        });
    }
    return Results.Ok();
});
app.MapMethods("/{**resourcePath}", new[] { "PATCH", "HEAD" }, () => Results.Ok());

Console.WriteLine($"Mock ARM server listening at {endpoint}");
await app.RunAsync();

static async Task<JsonObject> ReadResourceAsync(HttpRequest request)
{
    if (request.ContentLength == 0)
    {
        return new JsonObject();
    }

    JsonNode node = await JsonNode.ParseAsync(request.Body);
    return node as JsonObject ?? new JsonObject();
}

static void CompleteResource(JsonObject resource, string id)
{
    string[] segments = id.Split('/', StringSplitOptions.RemoveEmptyEntries);
    resource["id"] = id;
    resource["name"] = segments.LastOrDefault() ?? string.Empty;
    resource["type"] = GetResourceType(segments);
    resource["location"] ??= "eastus";

    JsonObject properties = resource["properties"] as JsonObject ?? new JsonObject();
    resource["properties"] = properties;
    properties["provisioningState"] = "Succeeded";

    string resourceType = resource["type"]?.GetValue<string>() ?? string.Empty;
    if (resourceType.EndsWith("/publicIPAddresses", StringComparison.OrdinalIgnoreCase))
    {
        properties["publicIPAddressVersion"] ??= "IPv4";
        properties["publicIPAllocationMethod"] ??= "Dynamic";
        properties["ipAddress"] ??= "192.0.2.1";
    }
    else if (resourceType.EndsWith("/virtualNetworks", StringComparison.OrdinalIgnoreCase))
    {
        if (properties["subnets"] is JsonArray subnets)
        {
            foreach (JsonNode subnetNode in subnets)
            {
                if (subnetNode is not JsonObject subnet)
                {
                    continue;
                }

                string subnetName = subnet["name"]?.GetValue<string>() ?? "mySubnet";
                subnet["id"] = $"{id}/subnets/{subnetName}";
                JsonObject subnetProperties = subnet["properties"] as JsonObject ?? new JsonObject();
                subnet["properties"] = subnetProperties;
                subnetProperties["provisioningState"] = "Succeeded";
            }
        }
    }
    else if (resourceType.EndsWith("/networkInterfaces", StringComparison.OrdinalIgnoreCase))
    {
        properties["macAddress"] ??= "00-00-5E-00-53-01";
        if (properties["ipConfigurations"] is JsonArray configurations)
        {
            foreach (JsonNode configurationNode in configurations)
            {
                if (configurationNode is not JsonObject configuration)
                {
                    continue;
                }

                string configurationName = configuration["name"]?.GetValue<string>() ?? "Primary";
                configuration["id"] = $"{id}/ipConfigurations/{configurationName}";
                JsonObject configurationProperties = configuration["properties"] as JsonObject ?? new JsonObject();
                configuration["properties"] = configurationProperties;
                configurationProperties["provisioningState"] = "Succeeded";
                configurationProperties["privateIPAddress"] ??= "10.0.0.4";
            }
        }
    }
}

static string GetResourceType(string[] segments)
{
    int providerIndex = Array.FindIndex(segments, segment => segment.Equals("providers", StringComparison.OrdinalIgnoreCase));
    if (providerIndex >= 0 && providerIndex + 2 < segments.Length)
    {
        var typeSegments = new List<string> { segments[providerIndex + 1] };
        for (int index = providerIndex + 2; index < segments.Length; index += 2)
        {
            typeSegments.Add(segments[index]);
        }

        return string.Join("/", typeSegments);
    }

    if (segments.Length >= 4 && segments[2].Equals("resourceGroups", StringComparison.OrdinalIgnoreCase))
    {
        return "Microsoft.Resources/resourceGroups";
    }

    return string.Empty;
}

static IResult ArmJson(JsonNode body)
{
    return Results.Text(
        body.ToJsonString(new JsonSerializerOptions { WriteIndented = false }),
        "application/json",
        statusCode: StatusCodes.Status200OK);
}
