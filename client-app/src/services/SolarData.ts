const BASE_URL = "/api/SolarData";

export const getSolarPointData = async (lon: number, lat: number, date: string) => {
  return await fetch(`${BASE_URL}/point?lon=${lon}&lat=${lat}&date=${date}`)
    .then(response => {
      if (!response.ok) throw new Error('Network response was not ok');
      return response.json();
    })
}
