import Map, {Source, Layer, type LayerProps, type MapRef} from 'react-map-gl/maplibre';
import 'maplibre-gl/dist/maplibre-gl.css';
import type {FeatureCollection, Feature, Point} from 'geojson';
import '../styles/SolarMap.css'
import {useCallback, useEffect, useRef, useState} from "react";
import {getSolarRegionData, mapRawPointsToGeoJsonFeatures} from "../services/SolarData.ts";

function SolarMap() {
  const [solarData, setSolarData] = useState<FeatureCollection<Point>>({
    type: 'FeatureCollection',
    features: []
  });
  const mapRef = useRef<MapRef | null>(null);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const lastFetchedBoundsRef = useRef<string>("");
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const fetchMapBoundsData = useCallback(async (map: MapRef) => {
    const bounds = map.getBounds();
    const lonMin = bounds.getWest();
    const lonMax = bounds.getEast();
    const latMin = bounds.getSouth();
    const latMax = bounds.getNorth();
    const date = "2026-04-08";

    const currentBoundsSignature = `${Math.round(lonMin)},${Math.round(lonMax)},${Math.round(latMin)},${Math.round(latMax)}`;

    // If map layout has not changed, do not fetch data
    if (currentBoundsSignature === lastFetchedBoundsRef.current) {
      return;
    }

    setLoading(true);
    setError(null);
    lastFetchedBoundsRef.current = currentBoundsSignature;

    try {
      // Open the long-lived HTTP stream connection
      const response = await getSolarRegionData(lonMin, lonMax, latMin, latMax, date)

      if (!response.body) return;
      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let accumulatedFeatures: Feature<Point>[] = [];

      let streamBuffer = "";

      // Read incoming chunk streams continuously in the background
      while (true) {
        const {value, done} = await reader.read();
        if (done) break;

        streamBuffer += decoder.decode(value, {stream: true});
        const lines = streamBuffer.split('\n');
        streamBuffer = lines.pop() || "";

        for (const line of lines) {
          const trimmedLine = line.trim();

          if (trimmedLine.startsWith('data: ')) {
            try {
              const jsonPayload = trimmedLine.substring(6);
              if (!jsonPayload) continue;

              const rawChunkPoints = JSON.parse(jsonPayload);

              // Map the fresh batch
              const newFeatures = mapRawPointsToGeoJsonFeatures(rawChunkPoints);
              accumulatedFeatures = [...accumulatedFeatures, ...newFeatures];

              setSolarData((prevSolarData) => {
                const existingFeatures = prevSolarData?.features || [];

                const existingIds = new Set(existingFeatures.map(f => f.id));

                const uniqueNewFeatures = accumulatedFeatures.filter(
                  f => !existingIds.has(f.id)
                );

                return {
                  type: "FeatureCollection",
                  features: [...existingFeatures, ...uniqueNewFeatures],
                }
              });
            } catch (e) {
              console.warn("Skipped a malformed or partial line segment:", e);
            }
          }
        }
      }
    } catch (err: unknown) {
      const message = err instanceof Error ? err.message : "Failed to load climate data.";
      setError(message);
    } finally {
      setLoading(false);
    }
  }, []);

  const handleMapIdle = useCallback(() => {
    // Clear old active timers
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    debounceTimerRef.current = setTimeout(() => {
      if (mapRef.current) {
        fetchMapBoundsData(mapRef.current);
      }
    }, 600);
  }, [fetchMapBoundsData]);

  // Clean up timers if the component unmounts mid-drag to avoid memory leaks
  useEffect(() => {
    return () => {
      if (debounceTimerRef.current) clearTimeout(debounceTimerRef.current);
    };
  }, []);

  const heatmapLayer: LayerProps = {
    'id': 'solar-heatmap',
    'type': 'heatmap',
    'maxzoom': 9,
    'paint': {
      // Weight determines how "hot" a single point is based on its intensity property
      'heatmap-weight': [
        'interpolate',
        ['linear'],
        ['get', 'intensity'],
        0, 0,
        40, 1
      ],
      // Increase the heatmap color weight by zoom level
      'heatmap-intensity': [
        'interpolate',
        ['exponential', 1.25],
        ['zoom'],
        2.5, 1,
        9, 7
      ],
      // The color gradient of the heatmap (Transparent -> Blue -> Yellow -> Red)
      'heatmap-color': [
        'interpolate',
        ['linear'],
        ['heatmap-density'],
        0, 'rgba(33,102,172,0)',
        0.2, 'rgb(103,169,207)',
        0.4, 'rgb(209,229,240)',
        0.6, 'rgb(253,219,199)',
        0.8, 'rgb(239,138,98)',
        1, 'rgb(178,24,43)'
      ],
      // Adjust the heatmap radius by zoom level
      'heatmap-radius': [
        'interpolate',
        ['exponential', 1.5],
        ['zoom'],
        2.5, 19,
        7, 150
      ],
      // Transition from heatmap to circle layer by zoom level
      'heatmap-opacity': [
        'interpolate',
        ['linear'],
        ['zoom'],
        6, 0.5,
        9, 0
      ]
    }
  };

  const circleLayer: LayerProps = {
    'id': 'solar-points',
    'type': 'circle',
    'minzoom': 7,
    'paint': {
      // Size circle radius by earthquake magnitude and zoom level
      'circle-radius': [
        'interpolate',
        ['linear'],
        ['zoom'],
        7, ['interpolate', ['linear'], ['get', 'intensity'], 1, 1, 6, 4],
        16, ['interpolate', ['linear'], ['get', 'intensity'], 1, 5, 6, 50]
      ],
      // Color circle by solar intensity
      'circle-color': [
        'interpolate',
        ['linear'],
        ['get', 'intensity'],
        1, 'rgba(33,102,172,0)',
        2, 'rgb(103,169,207)',
        3, 'rgb(209,229,240)',
        4, 'rgb(253,219,199)',
        5, 'rgb(239,138,98)',
        6, 'rgb(178,24,43)'
      ],
      'circle-stroke-color': 'white',
      'circle-stroke-width': 1,
      // Transition from heatmap to circle layer by zoom level
      'circle-opacity': [
        'interpolate',
        ['linear'],
        ['zoom'],
        7, 0,
        8, 1
      ]
    }
  }

  return (
    <div className={"map-canvas"}>
      {/* Sidebar Overlay for future controls */}
      <div className={"map-overlay"}>
        <h2 className={"map-overlay-title"}>Climate Dashboard</h2>
        {loading && <p className={"map-overlay-description"}>Loading regional metrics...</p>}
        {error && <p className={"map-overlay-description"} style={{color: '#e74c3c'}}>{error}</p>}
        {!loading && (
          <p className={"map-overlay-description"}>
            Displaying {solarData.features.length} data points rendered via WebGL.
          </p>
        )}
      </div>

      {/* Interactive Map Canvas */}
      <Map
        ref={mapRef}
        initialViewState={{longitude: -95, latitude: 38, zoom: 4.5}}
        minZoom={2.5}
        mapStyle="https://basemaps.cartocdn.com/gl/dark-matter-gl-style/style.json"
        onIdle={handleMapIdle}
      >
        {/* Render the Heatmap Source and Layer */}
        <Source id="solar-data" type="geojson" data={solarData}>
          <Layer {...heatmapLayer} />
          <Layer {...circleLayer} />
        </Source>
      </Map>
    </div>
  )
}

export default SolarMap
