import type {Feature, Point} from "geojson";

const BASE_URL = "/api/SolarData";

export const getSolarPointData = async (lon: number, lat: number, date: string) => {
  return await fetch(`${BASE_URL}/point?lon=${lon}&lat=${lat}&date=${date}`)
    .then(response => {
      if (!response.ok) throw new Error('Network response was not ok');
      return response.json();
    })
}

export const getSolarRegionData = async (
  lonMin: number,
  lonMax: number,
  latMin: number,
  latMax: number,
  date: string
): Promise<Response> => {

  const response = await fetch(
    `${BASE_URL}/region?lonMin=${lonMin}&lonMax=${lonMax}&latMin=${latMin}&latMax=${latMax}&date=${date}`
  );

  if (!response.ok) {
    throw new Error('Network response was not ok');
  }

  // Return the raw response container so the component can read its stream body
  return response;
};

/**
 * Transforms raw points from the backend into standard GeoJSON Feature objects.
 * Handles case-insensitive variations of longitude, latitude, and intensity.
 */
export function mapRawPointsToGeoJsonFeatures(
  rawPoints: Record<string, number | undefined>[]
): Feature<Point>[] {
  return rawPoints.map((f) => {
    // Guard against case variations from the backend API
    const lon = f.longitude !== undefined ? f.longitude : (f.Longitude ?? 0);
    const lat = f.latitude !== undefined ? f.latitude : (f.Latitude ?? 0);
    const intensity = f.intensity !== undefined ? f.intensity : (f.Intensity ?? 0);

    // Return the strictly structured GeoJSON Point Feature
    return {
      type: 'Feature',
      id: `${lon.toFixed(5)},${lat.toFixed(5)}`,
      geometry: {
        type: 'Point',
        coordinates: [lon, lat]
      },
      properties: {intensity}
    };
  });
};
