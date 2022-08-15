using DeltGeometriFelleskomponent.TopologyImplementation;
using System.Text.Json.Serialization;
using DeltGeometriFelleskomponent.Api;
using Microsoft.AspNetCore.Mvc;
using DeltGeometriFelleskomponent.Api.Controllers;
using DeltGeometriFelleskomponenet.CheckinImplementation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<ITopologyImplementation, TopologyImplementation>();
builder.Services.AddTransient<ICheckinImplementation, CheckinImplementation>();

builder.Services.Configure<JsonOptions>(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
});

builder.Services.AddOpenApiDocument(GeoJsonOpenApiDefs.AddGeoJsonMappings);

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseSwaggerUi3();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
