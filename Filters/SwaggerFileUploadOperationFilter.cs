using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;

public class SwaggerFileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        operation.RequestBody = new OpenApiRequestBody
        {
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = new Dictionary<string, OpenApiSchema>
                        {
                            ["videoFile"] = new OpenApiSchema { Type = "string", Format = "binary" },
                            ["imageFiles"] = new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string", Format = "binary" } }
                        },
                        Required = new HashSet<string> { "videoFile", "imageFiles" }
                    }
                }
            }
        };
    }
}
