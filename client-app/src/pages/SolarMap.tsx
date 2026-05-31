import Map, {Source, Layer, type LayerProps, type MapRef} from 'react-map-gl/maplibre';
import 'maplibre-gl/dist/maplibre-gl.css';
import type {FeatureCollection, Feature, Point} from 'geojson';
import '../styles/SolarMap.css'
import {type ChangeEvent, useCallback, useEffect, useRef, useState} from "react";
import {getSolarRegionData, mapRawPointsToGeoJsonFeatures} from "../services/SolarData.ts";
import {LngLatBounds} from "maplibre-gl";
import dayjs from 'dayjs';

function normalizeLongitude(lon: number): number {
  return ((lon + 180) % 360 + 360) % 360 - 180;
}

function SolarMap() {
  const [solarData, setSolarData] = useState<FeatureCollection<Point>>({
    type: 'FeatureCollection',
    features: []
  });
  const mapRef = useRef<MapRef | null>(null);
  const debounceTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);
  const loadedGridsRef = useRef<Set<string>>(new Set());
  const abortControllerRef = useRef<AbortController>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [mapBounds, setMapBounds] = useState<LngLatBounds>(new LngLatBounds());
  const [date, setDate] = useState<string>(dayjs().subtract(7, 'day').format('YYYY-MM-DD'));
  const reloadRef = useRef<boolean>(true);

  const fetchMapBoundsData = useCallback(async (map: MapRef) => {
    reloadRef.current = false;
    if (abortControllerRef.current) {
      abortControllerRef.current.abort();
    }

    const controller = new AbortController();
    abortControllerRef.current = controller;

    const bounds = map.getBounds();
    setMapBounds(bounds);

    const lonMin = bounds.getWest();
    const lonMax = bounds.getEast();
    const latMin = bounds.getSouth();
    const latMax = bounds.getNorth();

    const neededGrids: string[] = [];
    const missingGrids: { lonMin: number, lonMax: number, latMin: number, latMax: number, date: string }[] = [];

    for (let lon = lonMin; lon <= lonMax; lon++) {
      for (let lat = latMin; lat <= latMax; lat++) {
        const gridKey = `${lon},${lat},${date}`;
        neededGrids.push(gridKey);

        if (!loadedGridsRef.current.has(gridKey)) {
          missingGrids.push({
            lonMin: lon,
            lonMax: lon + 1,
            latMin: lat,
            latMax: lat + 1,
            date: date
          });
        }
      }
    }

    if (missingGrids.length === 0) {
      return;
    }

    setLoading(true);
    setError(null);

    try {
      // Get the smallest necessary bounding box
      const missingLonMin = Math.min(...missingGrids.map(grid => grid.lonMin));
      const missingLonMax = Math.max(...missingGrids.map(grid => grid.lonMax));
      const missingLatMin = Math.min(...missingGrids.map(grid => grid.latMin));
      const missingLatMax = Math.max(...missingGrids.map(grid => grid.latMax));

      // Open the long-lived HTTP stream connection
      const response = await getSolarRegionData(
        missingLonMin,
        missingLonMax,
        missingLatMin,
        missingLatMax,
        date,
        controller
      );

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

      // Add all grids to the loaded grids reference
      neededGrids.forEach((gridKey) => {
        loadedGridsRef.current.add(gridKey)
      });
    } catch (err: unknown) {
      if (err instanceof Error && err.name === 'AbortError') {
        console.log("Successfully swallowed an aborted request stream.");
      } else {
        const message = err instanceof Error ? err.message : "Failed to load climate data.";
        setError(message);
      }
    } finally {
      if (abortControllerRef.current == controller) {
        setLoading(false);
      }
    }
  }, [date]);

  const handleMapIdle = useCallback(() => {
    if (!reloadRef.current) return;

    // Clear old active timers
    if (debounceTimerRef.current) {
      clearTimeout(debounceTimerRef.current);
    }

    debounceTimerRef.current = setTimeout(() => {
      if (mapRef.current) {
        fetchMapBoundsData(mapRef.current).then(() => console.log("Successfully fetched solar data"));
      }
    }, 800);
  }, [fetchMapBoundsData]);

  const handleUserInteraction = useCallback(() => {
    reloadRef.current = true;
  }, []);

  const handleDateChange = useCallback((e: ChangeEvent<HTMLInputElement>) => {
    reloadRef.current = true;

    setSolarData({
      type: 'FeatureCollection',
      features: []
    });

    loadedGridsRef.current.clear();
    setDate(e.target.value);

    if (mapRef.current) {
      fetchMapBoundsData(mapRef.current).then(() => console.log("Successfully fetched solar data"));
    }
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
        5, 'rgb(103,169,207)',
        10, 'rgb(209,229,240)',
        15, 'rgb(253,219,199)',
        20, 'rgb(239,138,98)',
        25, 'rgb(178,24,43)'
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
        <input id={"date-field"} type={"date"} value={date} onChange={handleDateChange}/>
        {loading && <p className={"map-overlay-description"}>Loading regional metrics...</p>}
        {error && <p className={"map-overlay-description"} style={{color: '#e74c3c'}}>{error}</p>}
        {!loading && (
          <p className={"map-overlay-description"}>
            Displaying {solarData.features.length} data points rendered via WebGL.
            <br/>
            Longitude: {normalizeLongitude(Math.round(mapBounds.getCenter().lng))}°
            <br/>
            Latitude: {Math.round(mapBounds.getCenter().lat)}°
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
        onZoom={handleUserInteraction}
        onDrag={handleUserInteraction}
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
