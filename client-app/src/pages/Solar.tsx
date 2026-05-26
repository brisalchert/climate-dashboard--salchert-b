import {useEffect, useState} from "react";
import type {SolarPoint} from "../models/SolarPoint.ts";
import {getSolarPointData} from "../services/SolarData.ts";

function Solar() {
  const [data, setData] = useState<SolarPoint | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    // Testing with hardcoded coordinates for now
    const lat = 40.5;
    const lon = -89.5;
    const date = "2026-04-08";

    getSolarPointData(lon, lat, date)
      .then(responseData => {
        setData(responseData);
        setLoading(false);
      })
      .catch(err => {
        setError(err.message);
        setLoading(false);
      });
  }, []);

  if (loading) return <div>Connecting to C# API...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
      <h1>NASA POWER Solar Data</h1>
      {data && (
        <div style={{border: '1px solid #ccc', padding: '10px'}}>
          <p><strong>Longitude:</strong> {data.longitude}</p>
          <p><strong>Latitude:</strong> {data.latitude}</p>
          <p><strong>Elevation:</strong> {data.elevation}m</p>
          <p><strong>Solar Intensity:</strong> {data.intensity} kW-hr/m²/day</p>
        </div>
      )}
    </div>
  )
}

export default Solar;
