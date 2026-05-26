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
