using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public static class NgisFeatureHelper
{
    public static NgisFeature Copy(NgisFeature feature) => new()
    {
        Properties = feature.Properties, Geometry_Properties = feature.Geometry_Properties,
        Geometry = feature.Geometry.Copy(), Update = feature.Update
    };

    public static string? GetLokalId(NgisFeature feature)
    {
        if (!feature.Properties.Exists("identifikasjon"))
        {
            return null;
        }

        var identifikasjon = (IAttributesTable) feature.Properties["identifikasjon"];
        return  (string)identifikasjon["lokalId"];
    }

    public static Operation? GetOperation(NgisFeature feature)
        => feature.Update?.Action;

    public static NgisFeature CreateFeature(Geometry geometry, string? lokalId = null)
        => new ()
        {
            Geometry = geometry,
            Properties = new AttributesTable(new Dictionary<string, object>()
            {
                {
                    "identifikasjon", new AttributesTable(new Dictionary<string, object>()
                    {
                        {
                            "lokalId", lokalId ?? Guid.NewGuid().ToString()
                        }
                    })
                }
            })
        };

    public static NgisFeature CreateFeature(Geometry geometry, string? lokalId, Operation operation)
    {
        var feature = CreateFeature(geometry, lokalId);
        SetOperation(feature, operation);
        return feature;
    }

    public static NgisFeature CreateFeature(Geometry geometry, string lokalId, Operation operation, IEnumerable<string> exterior, IEnumerable<IEnumerable<string>>? interiors)
    {
        var feature = CreateFeature(geometry, lokalId, operation);
        SetReferences(feature, exterior, interiors);
        return feature;
    }

    public static void SetInterior(NgisFeature feature, IEnumerable<IEnumerable<string>> interiors)
    {
        feature.Geometry_Properties ??= new GeometryProperties();
        feature.Geometry_Properties!.Interiors = interiors.Select(i => i.ToList()).ToList();
    }

    public static void SetInterior(NgisFeature feature, IEnumerable<IEnumerable<NgisFeature>> interiors)
        => SetInterior(feature, interiors.Select(i => i.Select(GetLokalId).OfType<string>()));

    public static void SetExterior(NgisFeature feature, IEnumerable<string> exterior)  {
        feature.Geometry_Properties ??= new GeometryProperties();
        feature.Geometry_Properties!.Exterior = exterior.ToList();
    }
    public static void SetExterior(NgisFeature feature, IEnumerable<NgisFeature> exterior)
        => SetExterior(feature, exterior.Select(GetLokalId).OfType<string>());

    public static void SetReferences(NgisFeature feature, IEnumerable<string> exterior, IEnumerable<IEnumerable<string>>? interiors)
    {
        SetExterior(feature, exterior);
        if (interiors != null)
        {
            SetInterior(feature, interiors);
        }
    }

    public static List<string> GetAllReferences(NgisFeature feature)
    {
        var exteriorReferences = feature.Geometry_Properties?.Exterior;
        var interiorReferences = feature.Geometry_Properties?.Interiors?.SelectMany(l => l).ToList();

        var references = new List<string>();
        if (exteriorReferences != null)
        {
            references = references.Concat(exteriorReferences).ToList();
        }
        if (interiorReferences != null)
        {
            references = references.Concat(interiorReferences).ToList();
        }

        return references.Select(NgisFeatureHelper.RemoveSign).ToList();

    }

    public static List<string> GetExteriors(NgisFeature feature) => feature.Geometry_Properties?.Exterior ?? new List<string>();

    public static List<List<string>> GetInteriors(NgisFeature feature) => feature.Geometry_Properties?.Interiors ?? new List<List<string>>();

    public static void SetReferences(NgisFeature feature, IEnumerable<NgisFeature> exterior,
        IEnumerable<IEnumerable<NgisFeature>>? interiors)
        => SetReferences(feature, exterior.Select(GetLokalId).OfType<string>(),
            interiors?.Select(i => i.Select(GetLokalId).OfType<string>()));

    public static void SetOperation(NgisFeature feature, Operation operation)
    {
        feature.Update ??= new UpdateAction();
        feature.Update.Action = operation;
    }

    public static NgisFeature SetOperation2(NgisFeature feature, Operation operation)
    {
        SetOperation(feature, operation);
        return feature;
    }
    
    public static NgisFeature EnsureLocalId(NgisFeature feature)
    {
        if (GetLokalId(feature) == null)
        {
            SetLokalId(feature, Guid.NewGuid().ToString());
        }

        return feature;
    }

    public static void SetLokalId(NgisFeature feature, string lokalId)
    {
       
        var props = feature.Properties;

        if (!props.Exists("identifikasjon"))
        {
            props.Add("identifikasjon",new AttributesTable());
        }
        var id = ((AttributesTable)props["identifikasjon"]);
        if (!id.Exists("lokalId")) { 
            id.Add("lokalId", lokalId);
        } else
        {
            id["lokalId"] = lokalId;   
        }

        feature.Properties = props;
        
    }

    public static string RemoveSign(string reference)
        => reference.StartsWith("-") ? reference[1..] : reference;

}