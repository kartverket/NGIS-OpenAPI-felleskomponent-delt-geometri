using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

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
    
    private static PolygonCreator.FeatureWithDirection GetFeatureWithDirection(string id, List<NgisFeature> affectedFeatures)  {
        var reversed = id.StartsWith("-");
        var lokalId = RemoveSign(id);

        return new PolygonCreator.FeatureWithDirection()
        {
            IsReversed = reversed,
            Feature = affectedFeatures.Find(f => GetLokalId(f) == lokalId)!
        };
    }

    public static NgisFeature CreateFeature(Geometry geometry, string lokalId, Operation operation, IEnumerable<string> exterior, IEnumerable<IEnumerable<string>>? interiors, List<NgisFeature> affectedFeatures)
    {
        var feature = CreateFeature(geometry, lokalId, operation);
        
        var exteriors = exterior.Select(id =>GetFeatureWithDirection(id, affectedFeatures)).ToList();

        var interiorFeatures = interiors != null ? interiors.Select(ids => ids.Select(id => GetFeatureWithDirection(id, affectedFeatures))).ToList() : new List<IEnumerable<PolygonCreator.FeatureWithDirection>>();

        ReferenceHelper.SetReferences(feature, exteriors, interiorFeatures);

        return feature;
    }


    
    public static List<string> GetAllReferences(NgisFeature feature)
        => ReferenceHelper.GetBoundsReferences(feature).Select(f => f.LokalId).ToList();

    public static List<string> GetExteriors(NgisFeature feature) 
        => ReferenceHelper.GetExteriorReferences(feature).Select(f => f.LokalId).ToList();

    public static List<List<string>> GetInteriors(NgisFeature feature) 
        => ReferenceHelper.GetInteriorReferences(feature)
        .Select(l => l.Select(f => $"{(f.Reverse ? "-" : "")}{f.LokalId}").ToList())
        .ToList();

    public static void SetOperation(NgisFeature feature, Operation operation)
    {
        feature.Update ??= new UpdateAction();
        feature.Update.Action = operation;
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