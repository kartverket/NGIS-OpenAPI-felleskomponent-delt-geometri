using DeltGeometriFelleskomponent.Models;
using NetTopologySuite.Features;
using Newtonsoft.Json;

namespace DeltGeometriFelleskomponent.TopologyImplementation;

public static class ReferenceHelper
{
    private const string ReferencesPrefix = "avgrensesAv";

    public static void AddBoundsReferences(NgisFeature feature, List<BoundsReference> references, string featureType)
    {
        var updatedReferences =
            (GetBoundsReferencesForFeatureType(feature, featureType) ?? new List<BoundsReference>()).Concat(references).ToList();
        ReplaceBoundsReferences(feature, updatedReferences, featureType);
    }
    
    public static void ReplaceBoundsReferences(NgisFeature feature, List<BoundsReference> references, string featureType)
    {
        var key = $"{ReferencesPrefix}{featureType}";

        if (feature.Properties.Exists(key))
        {
            feature.Properties[key] = references;
        }
        else
        {
            feature.Properties.Add(key, references);
        }
    }

    private static IEnumerable<BoundsReference>? GetBoundsReferencesForFeatureType(NgisFeature feature, string featureType)
        => GetBoundsReferencesForKey(feature,$"{ReferencesPrefix}{featureType}");

    private static IEnumerable<BoundsReference>? GetBoundsReferencesForKey(NgisFeature feature, string key)
    {
        var data = feature.Properties.GetOptionalValue(key);
        if (data == null)
        {
            return null;
        }

        if (data is List<BoundsReference> list)
        {
            return list;
        }

        var t = data.GetType();
        if (data is IEnumerable<object> attributesTable)
        {
            return attributesTable.Select(e => CreateBoundsReferenceFromAttributesTable((AttributesTable) e)).ToList();
        }

        return null;
    }

    private static BoundsReference CreateBoundsReferenceFromAttributesTable(AttributesTable attributesTable)
     => new ()
        {
            Featuretype = (string) attributesTable["featuretype"],
            LokalId = (string) attributesTable["lokalId"],
            Reverse = (bool) attributesTable["reverse"],
            Idx = ((List<object>)attributesTable["idx"]).Select(e => (int)(long)e).ToList(),
        };
    


    public static List<BoundsReference> GetBoundsReferences(NgisFeature feature)
        => feature.Properties.GetNames().Where(key => key.StartsWith(ReferencesPrefix))
            .SelectMany(key => GetBoundsReferencesForKey(feature, key) ?? new List<BoundsReference>()).ToList();
    
    public static List<BoundsReference> GetExteriorReferences(NgisFeature feature)
        => GetBoundsReferences(feature).Where(f => f.Idx[1] == 0).ToList();

    public static void RemoveInteriorAt(NgisFeature feature, int idx)
    {
        ReplaceReferences(feature, GetBoundsReferences(feature).Where(f => f.Idx[1] != idx).ToList());
    }

    public static List<List<BoundsReference>> GetInteriorReferences(NgisFeature feature)
    {
        var references = GetBoundsReferences(feature);

        return references
            .Select(r => r.Idx[1])
            .Where(i => i > 0)
            .Distinct()
            .Select(i => references.Where(r => r.Idx[1] == i).ToList())
            .ToList();
    }

    public static void SetReferences(NgisFeature feature, IEnumerable<PolygonCreator.FeatureWithDirection> exterior, IEnumerable<IEnumerable<PolygonCreator.FeatureWithDirection>>? interiors)
    {
        var exteriorReferences = exterior.Select((f, i) => GetBoundsReference(f, 0, i)).ToList();
        var interiorReferences = interiors != null ?interiors.Select((hole, holeIdx) =>
            hole.Select((f, i) => GetBoundsReference(f, holeIdx + 1, i))).ToList() : new List<IEnumerable<BoundsReference>>();

        var references = exteriorReferences.Concat(interiorReferences.SelectMany(h => h)).ToList();
        AddReferences(feature, references);
    }

    private static void AddReferences(NgisFeature feature, List<BoundsReference> references)
    {
        var featureTypes = references.Select(e => e.Featuretype).Distinct();
        foreach (var featureType in featureTypes)
        {
            AddBoundsReferences(feature, references.Where(f => f.Featuretype == featureType).ToList(), featureType);
        }
    }

    private static void ReplaceReferences(NgisFeature feature, List<BoundsReference> references)
    {
        var featureTypes = references.Select(e => e.Featuretype).Distinct();
        foreach (var featureType in featureTypes)
        {
            ReplaceBoundsReferences(feature, references.Where(f => f.Featuretype == featureType).ToList(), featureType);
        }
    }

    public static void AddInterior(NgisFeature feature, IEnumerable<PolygonCreator.FeatureWithDirection> interior)
    {
        var newIdx = GetInteriorReferences(feature).Count + 1;
        var references = interior.Select((f, i) => GetBoundsReference(f, newIdx, i)).ToList();
        AddReferences(feature, references);
    }

    public static BoundsReference GetBoundsReference(PolygonCreator.FeatureWithDirection f, int surfaceIdx, int referenceIdx) =>
        new()
        {
            LokalId = NgisFeatureHelper.GetLokalId(f.Feature)!,
            Featuretype = (string)f.Feature.Properties.GetOptionalValue("featuretype"),
            Reverse = f.IsReversed,
            Idx = CreateIndexList(surfaceIdx, referenceIdx)
        };


    private static List<int> CreateIndexList(int surfaceIdx, int referenceIdx) => new() { 0, surfaceIdx, referenceIdx };

    private static bool IsExterior(BoundsReference reference) => reference.Idx[1] == 0;
}