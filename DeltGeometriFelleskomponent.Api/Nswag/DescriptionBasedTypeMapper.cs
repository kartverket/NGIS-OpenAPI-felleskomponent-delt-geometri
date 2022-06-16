using NJsonSchema;
using NJsonSchema.Generation.TypeMappers;


/*
 * A Type mapper that just sets description
 * Used to link to GeoJSON spec
 */
namespace DeltGeometriFelleskomponent.Api;

public class DescriptionBasedTypeMapper : ITypeMapper
{
    private readonly string _description;

    public DescriptionBasedTypeMapper(Type type, string description)
    {
        MappedType = type;
        _description = description;
    }

    public void GenerateSchema(JsonSchema schema, TypeMapperContext context)
    {
        if (!context.JsonSchemaResolver.HasSchema(MappedType, false))
        {
            var jsonSchema = JsonSchema.CreateAnySchema();
            jsonSchema.Description = _description;
            jsonSchema.Type = JsonObjectType.Object;
            context.JsonSchemaResolver.AddSchema(MappedType, false, jsonSchema);
            schema.Reference = jsonSchema;
        }
        else
        {
            schema.Reference = context.JsonSchemaResolver.GetSchema(MappedType, false);
        }
    }

    public Type MappedType { get; }
    public bool UseReference { get; } = false;
}