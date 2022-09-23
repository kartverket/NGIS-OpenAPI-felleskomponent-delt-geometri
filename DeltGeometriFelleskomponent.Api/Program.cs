using DeltGeometriFelleskomponent.TopologyImplementation;
using DeltGeometriFelleskomponent.Api;
using DeltGeometriFelleskomponent.Api.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
    options.AddPolicy("CrossOriginPolicy", p =>
        p.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()));

builder.Services.AddControllers().AddNewtonsoftJson(jsonOptions =>
{
    jsonOptions.SerializerSettings.Converters.Add(new GeoJsonConverter());
    jsonOptions.SerializerSettings.Converters.Add(new StringEnumConverter());
    jsonOptions.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    jsonOptions.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddTransient<ITopologyImplementation, TopologyImplementation>();

builder.Services.AddOpenApiDocument(config =>
{
    config.Title = "NGIS Felleskomponent";
    GeoJsonOpenApiDefs.AddGeoJsonMappings(config);
});

var app = builder.Build();
app.UseCors("CrossOriginPolicy");
// Configure the HTTP request pipeline.
app.UseOpenApi();
app.UseSwaggerUi3(c =>
{
    c.DocExpansion = "full";
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
