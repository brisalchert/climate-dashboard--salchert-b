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
      <div style={{
        position: 'absolute',
        top: 20,
        left: 20,
        zIndex: 1,
        background: 'rgba(30, 30, 30, 0.85)',
        color: '#fff',
        padding: '20px',
        borderRadius: '8px',
        fontFamily: 'sans-serif',
        backdropFilter: 'blur(4px)',
        boxShadow: '0 4px 6px rgba(0,0,0,0.3)'
      }}>
        <h2 style={{margin: '0 0 10px 0'}}>Climate Dashboard</h2>
        <p style={{margin: 0, fontSize: '14px', color: '#aaa'}}>
          Click anywhere on the map to fetch live NASA solar irradiance data.
        </p>

        {loading && <p style={{color: '#e67e22'}}>Querying C# API & NASA...</p>}
        {error && <p style={{color: '#e74c3c'}}>Error: {error}</p>}

        {data && !loading && (
          <div style={{borderTop: '1px solid #444', paddingTop: '10px'}}>
            <p><strong>Latitude:</strong> {data.latitude.toFixed(4)}</p>
            <p><strong>Longitude:</strong> {data.longitude.toFixed(4)}</p>
            <p><strong>Elevation:</strong> {data.elevation}m</p>
            <p style={{color: '#f1c40f', fontSize: '16px'}}>
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
