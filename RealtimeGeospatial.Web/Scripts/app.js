var app = angular.module("app", []);

// service to connect to signalr
app.factory('twitterFactory', ['$rootScope', function ($rootScope) {

    var tweets = [];

    var twitterHub = $.connection.twitterHub;

    twitterHub.client.broadcastTweet = function (tweet) {
        // limit array size to 1000
        if (tweets.length >= 1000) {
            tweets.splice(0, 1);
        }
        tweets.push(tweet);
        $rootScope.$apply();
    };

    $.connection.hub.start();

    return function()
    {
        return { tweets: tweets };
    };

}]);

app.controller('TwitterController', ['$scope', 'twitterFactory', function ($scope, twitterFactory) {
    $scope.tweets = twitterFactory().tweets;    
}]);

app.directive('olMap', [function() {
    return {
        restrict: 'E',
        template: '<div id="map" class="map"></div>',
        scope: {
            features: '&'
        },
        link: function (scope) {

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
                div: 'map',
                layers: [vectorLayer, mapboxLayer],
                center: new OpenLayers.LonLat(-2.5833, 51.4500).transform('EPSG:4326', 'EPSG:3857'),
                zoom: 5
            });

            map.addLayers([mapboxLayer, vectorLayer]);

            var selectControl = new OpenLayers.Control.SelectFeature(vectorLayer, {
                hover: true,
                onSelect: function(feature) {
                    var layer = feature.layer;
                    feature.style.fillOpacity = 1;
                    feature.style.pointRadius = 20;
                    layer.drawFeature(feature);                    

                    var content = "<div>@" + feature.attributes.user + ": " + feature.attributes.message + "</div>";

                    var popup = new OpenLayers.Popup.FramedCloud(
                        feature.id+"_popup", 
                        feature.geometry.getBounds(). 
                        getCenterLonLat(),
                        new OpenLayers.Size(250, 100),
                        content,
                        null, 
                        false, 
                        null);

                    feature.popup = popup;
                    map.addPopup(popup);
                },
                onUnselect: function(feature) {
                    var layer = feature.layer;
                    feature.style.fillOpacity = 0.2;
                    feature.style.pointRadius = 6;
                    feature.renderIntent = null;
                    layer.drawFeature(feature);
 
                    map.removePopup(feature.popup);
                }
            });

            map.addControl(selectControl);
            selectControl.activate(); 

            scope.$watchCollection(scope.features, function (value) {
                console.log(value);

                // read last tweet
                var tweet = value[value.length - 1];

                var feature = new OpenLayers.Feature.Vector(
                new OpenLayers.Geometry.Point(tweet.Longitude, tweet.Latitude).transform('EPSG:4326', 'EPSG:3857'),
                    {
                        user: tweet.User,
                        message: tweet.Message
                    },
                    {
                        pointRadius: 6,
                        stroke: false,
                        fillColor: "#ff0000",
                        fillOpacity: 0.2
                    });

                vectorLayer.addFeatures([feature]);

                setInterval(function () {
                    if (feature.popup != undefined) {
                        map.removePopup(feature.popup);
                    }
                    vectorLayer.removeFeatures([feature]);
                }, 10000);

            });
        }
    };
}]);