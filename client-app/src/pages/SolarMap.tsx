import Map, {Marker, type MapLayerMouseEvent} from 'react-map-gl/maplibre';
import '../styles/SolarMap.css'
import {useState, useEffect} from "react";
import {getSolarPointData} from "../services/SolarData.ts";
import type {SolarPoint} from "../models/SolarData.ts";

function SolarMap() {
  const [clickedLocation, setClickedLocation] = useState<{ lng: number; lat: number } | null>(null);
  const [data, setData] = useState<SolarPoint | null>(null);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!clickedLocation) return;

    const date = '2025-06-01';
    const {lng, lat} = clickedLocation;

    getSolarPointData(lng, lat, date)
      .then(responseData => {
        setData(responseData);
        setLoading(false);
      })
      .catch(err => {
        setError(err.message);
        setLoading(false);
      });
  }, [clickedLocation]);

  const handleMapClick = (event: MapLayerMouseEvent) => {
    const {lng, lat} = event.lngLat;

    setLoading(true);
    setError(null);

    setClickedLocation({lng, lat})
  };

  return (
    <div className={"map-canvas"}>
      {/* Sidebar Overlay for future controls */}
      <div className={"map-overlay"}>
        <h2 className={"map-overlay-title"}>Climate Dashboard</h2>
        <p className={"map-overlay-description"}>
          Click anywhere on the map to fetch live NASA solar irradiance data.
        </p>

        {loading && <p className={"loading-label"}>Querying C# API & NASA...</p>}
        {error && <p className={"error-label"}>Error: {error}</p>}

        {data && !loading && (
          <div className={"solar-data-container"}>
            <p><strong>Latitude:</strong> {data.latitude.toFixed(4)}</p>
            <p><strong>Longitude:</strong> {data.longitude.toFixed(4)}</p>
            <p><strong>Elevation:</strong> {data.elevation}m</p>
            <p className={"solar-intensity-label"}>
              <strong>Intensity:</strong> {data.intensity.toFixed(2)} kW-hr/m²/day
            </p>
          </div>
        )}
      </div>

      {/* Interactive Map Canvas */}
      <Map
        initialViewState={{
          longitude: 0,
          latitude: 20,
          zoom: 2
        }}
        mapStyle="https://basemaps.cartocdn.com/gl/dark-matter-gl-style/style.json"
        onClick={handleMapClick}
      >
        {clickedLocation && (
          <Marker longitude={clickedLocation.lng} latitude={clickedLocation.lat} color="#f1c40f"/>
        )}
      </Map>
    </div>
  )
}

export default SolarMap
