using backend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;

public class SwaggerFileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        // Kiểm tra nếu endpoint sử dụng [FromForm] với MovieDTO
        var fromFormParameter = context.MethodInfo
            .GetParameters()
            .FirstOrDefault(p => p.ParameterType == typeof(MovieDTO) && 
                                p.GetCustomAttributes(typeof(FromFormAttribute), false).Length > 0);

        if (fromFormParameter != null)
        {
            // Sinh schema từ MovieDTO
            var schema = context.SchemaGenerator.GenerateSchema(typeof(MovieDTO), context.SchemaRepository);

            operation.RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = schema
                    }
                }
            };
        }
    }
}