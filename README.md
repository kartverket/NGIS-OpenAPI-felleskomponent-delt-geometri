# NGIS-OpenAPI-felleskomponent-delt-geometri
Felles hjelpe-komponent for redigering av flategeometri gjennom NGIS-OpenAPI.


## Kode-layout

### DeltGeometriFelleskomponent.Api
Aspnet core 6 api-implementasjon av api

Bruker: 

- NSwag for swagger-generering
- NetTopologySuite.IO.GeoJSON4STJ for å håndtere geojson input/output

### DeltGeometriFelleskomponent.TopologyImplementation
Implementasjon av topologi-håndtering

### DeltGeometriFelleskomponent.Models
Klasser brukt av Api og TopologyImplementation

### DeltGeometriFelleskomponent.Tests
Tester (xUnit)


## Eksempel-request

POST https://localhost:7042/resolveReferences
```
{
    "feature": {
        "operation": "create"
        "localid": "1",
        "geometry": {
            "type": "Point",
            "coordinates": [10.406799, 63.421209]
        },
    },
    "affectedFeatures": []
}
```