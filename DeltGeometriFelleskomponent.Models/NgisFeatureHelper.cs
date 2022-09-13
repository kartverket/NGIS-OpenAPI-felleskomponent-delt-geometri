using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.Models;

public static class NgisFeatureHelper
{

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

    public static void EnsureLocalId(NgisFeature feature)
    {
        if (GetLokalId(feature) == null)
        {
            SetLokalId(feature, Guid.NewGuid().ToString());
        }
    }

    public static void SetLokalId(NgisFeature feature, string lokalId)
    {
       
        var props = feature.Properties;

        if (!props.Exists("identifikasjon"))
        {
            props.Add("identifikasjon",new AttributesTable());
        }
        ((IAttributesTable)props["identifikasjon"]).Add("lokalId", lokalId);

        feature.Properties = props;
        
    }

}