import Map, {Source, Layer, type LayerProps} from 'react-map-gl/maplibre';
import 'maplibre-gl/dist/maplibre-gl.css';
import '../styles/SolarMap.css'
import {useMemo} from "react";

function SolarMap() {
  const dummyData = useMemo(() => {
    const points = [];
    // Create a grid over the US
    for (let lng = -125; lng <= -70; lng += 1) {
      for (let lat = 25; lat <= 50; lat += 1) {
        // Fake intensity: hotter in the Southwest (closer to lat 30, lng -110)
        const distToDesert = Math.sqrt(Math.pow(lat - 35, 2) + Math.pow(lng - -110, 2));
        const intensity = Math.max(0, 10 - distToDesert * 0.3) + Math.random();

        points.push({longitude: lng, latitude: lat, intensity: intensity});
      }
    }
    return points;
  }, []);

  const geoJsonData = useMemo(() => {
    return {
      type: 'FeatureCollection' as const,
      features: dummyData.map((point) => ({
        type: 'Feature' as const,
        geometry: {
          type: 'Point' as const,
          coordinates: [point.longitude, point.latitude],
        },
        properties: {
          // This is the crucial value the heatmap will read
          intensity: point.intensity,
        },
      })),
    };
  }, [dummyData]);

  const heatmapLayer: LayerProps = {
    id: 'solar-heatmap',
    type: 'heatmap',
    paint: {
      // Weight determines how "hot" a single point is based on its intensity property (0 to 10 scale)
      'heatmap-weight': [
        'interpolate', ['linear'], ['get', 'intensity'],
        0, 0,
        10, 1
      ],
      // The color gradient of the heatmap (Transparent -> Blue -> Yellow -> Red)
      'heatmap-color': [
        'interpolate', ['linear'], ['heatmap-density'],
        0, 'rgba(33,102,172,0)',
        0.1, 'rgb(103,169,207)',
        0.3, 'rgb(209,229,240)',
        0.5, 'rgb(253,219,199)',
        1, 'rgb(227, 26, 28)'
      ],
      // How wide each point's heat spreads (in pixels)
      'heatmap-radius': 30,
      // Global opacity of the layer
      'heatmap-opacity': 0.6
    }
  };

  return (
    <div className={"map-canvas"}>
      {/* Sidebar Overlay for future controls */}
      <div className={"map-overlay"}>
        <h2 className={"map-overlay-title"}>Climate Dashboard</h2>
        <p className={"map-overlay-description"}>
          Displaying {dummyData.length} data points rendered via WebGL.
        </p>
      </div>

      {/* Interactive Map Canvas */}
      <Map
        initialViewState={{longitude: -95, latitude: 38, zoom: 3.5}}
        mapStyle="https://basemaps.cartocdn.com/gl/dark-matter-gl-style/style.json"
      >
        {/* Render the Heatmap Source and Layer */}
        <Source id="solar-data" type="geojson" data={geoJsonData}>
          <Layer {...heatmapLayer} />
        </Source>
      </Map>
    </div>
  )
}

export default SolarMap
