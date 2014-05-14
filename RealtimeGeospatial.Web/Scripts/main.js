$(function () {
    var mapboxLayer = new OpenLayers.Layer.XYZ(
    "Twitter",
    [
        "http://a.tiles.mapbox.com/v3/mthornal.i68h2h9f/${z}/${x}/${y}.png",
        "http://b.tiles.mapbox.com/v3/mthornal.i68h2h9f/${z}/${x}/${y}.png",
        "http://c.tiles.mapbox.com/v3/mthornal.i68h2h9f/${z}/${x}/${y}.png",
        "http://d.tiles.mapbox.com/v3/mthornal.i68h2h9f/${z}/${x}/${y}.png"
    ],
    {
        sphericalMercator: true,
        maxExtent: new OpenLayers.Bounds(-20037508.34, -20037508.34, 20037508.34, 20037508.34)
    }
);

    var vectorLayer = new OpenLayers.Layer.Vector("Overlay");

    var map = new OpenLayers.Map({
        div: "map",
        layers: [vectorLayer, mapboxLayer],
        center: new OpenLayers.LonLat(-2.5833, 51.4500).transform('EPSG:4326', 'EPSG:3857'),
        zoom: 3
    });

    map.zoomToMaxExtent();

    var twitterHub = $.connection.twitterHub;

    twitterHub.client.broadcastTweet = function (tweet) {
        var feature = new OpenLayers.Feature.Vector(
            new OpenLayers.Geometry.Point(tweet.Longitude, tweet.Latitude).transform('EPSG:4326', 'EPSG:3857'),
            { user: tweet.user, message: tweet.Message },
            {
                pointRadius: 3,
                stroke: false,
                fillColor: "#ff0000",
                fillOpacity: 0.4
            });

        vectorLayer.addFeatures([feature]);
    };

    $.connection.hub.start();

});