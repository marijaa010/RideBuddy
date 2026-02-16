// Common Serbian cities with their coordinates
export interface CityCoordinates {
  lat: number;
  lng: number;
}

const CITY_COORDINATES: { [key: string]: CityCoordinates } = {
  // Major cities
  'beograd': { lat: 44.8176, lng: 20.4633 },
  'belgrade': { lat: 44.8176, lng: 20.4633 },
  'београд': { lat: 44.8176, lng: 20.4633 },
  
  'novi sad': { lat: 45.2671, lng: 19.8335 },
  'нови сад': { lat: 45.2671, lng: 19.8335 },
  
  'niš': { lat: 43.3209, lng: 21.8954 },
  'nis': { lat: 43.3209, lng: 21.8954 },
  'ниш': { lat: 43.3209, lng: 21.8954 },
  
  'kragujevac': { lat: 44.0125, lng: 20.9114 },
  'крагујевац': { lat: 44.0125, lng: 20.9114 },
  
  'subotica': { lat: 46.1005, lng: 19.6672 },
  'суботица': { lat: 46.1005, lng: 19.6672 },
  
  'zrenjanin': { lat: 45.3833, lng: 20.3833 },
  'зрењанин': { lat: 45.3833, lng: 20.3833 },
  
  'pančevo': { lat: 44.8708, lng: 20.6406 },
  'pancevo': { lat: 44.8708, lng: 20.6406 },
  'панчево': { lat: 44.8708, lng: 20.6406 },
  
  'čačak': { lat: 43.8914, lng: 20.3497 },
  'cacak': { lat: 43.8914, lng: 20.3497 },
  'чачак': { lat: 43.8914, lng: 20.3497 },
  
  'kraljevo': { lat: 43.7256, lng: 20.6869 },
  'краљево': { lat: 43.7256, lng: 20.6869 },
  
  'smederevo': { lat: 44.6614, lng: 20.9300 },
  'смедерево': { lat: 44.6614, lng: 20.9300 },
  
  'leskovac': { lat: 42.9981, lng: 21.9461 },
  'лесковац': { lat: 42.9981, lng: 21.9461 },
  
  'užice': { lat: 43.8584, lng: 19.8481 },
  'uzice': { lat: 43.8584, lng: 19.8481 },
  'ужице': { lat: 43.8584, lng: 19.8481 },
  
  'valjevo': { lat: 44.2750, lng: 19.8900 },
  'ваљево': { lat: 44.2750, lng: 19.8900 },
  
  'vranje': { lat: 42.5519, lng: 21.9022 },
  'врање': { lat: 42.5519, lng: 21.9022 },
  
  'šabac': { lat: 44.7472, lng: 19.6939 },
  'sabac': { lat: 44.7472, lng: 19.6939 },
  'шабац': { lat: 44.7472, lng: 19.6939 },
  
  'sombor': { lat: 45.7742, lng: 19.1122 },
  'сомбор': { lat: 45.7742, lng: 19.1122 },
  
  'požarevac': { lat: 44.6200, lng: 21.1886 },
  'pozarevac': { lat: 44.6200, lng: 21.1886 },
  'пожаревац': { lat: 44.6200, lng: 21.1886 },
  
  'pirot': { lat: 43.1536, lng: 22.5886 },
  'пирот': { lat: 43.1536, lng: 22.5886 },
  
  'zaječar': { lat: 43.9039, lng: 22.2856 },
  'zajecar': { lat: 43.9039, lng: 22.2856 },
  'зајечар': { lat: 43.9039, lng: 22.2856 },
  
  'kikinda': { lat: 45.8272, lng: 20.4633 },
  'кикинда': { lat: 45.8272, lng: 20.4633 },
  
  'sremska mitrovica': { lat: 44.9756, lng: 19.6144 },
  'сремска митровица': { lat: 44.9756, lng: 19.6144 },
  
  'jagodina': { lat: 43.9775, lng: 21.2597 },
  'јагодина': { lat: 43.9775, lng: 21.2597 },
  
  'vršac': { lat: 45.1200, lng: 21.3033 },
  'vrsac': { lat: 45.1200, lng: 21.3033 },
  'вршац': { lat: 45.1200, lng: 21.3033 },
};

/**
 * Get coordinates for a city name.
 * Returns default Belgrade coordinates if city not found.
 */
export function getCityCoordinates(cityName: string): CityCoordinates {
  const normalized = cityName.toLowerCase().trim();
  return CITY_COORDINATES[normalized] || { lat: 44.8176, lng: 20.4633 }; // Default to Belgrade
}

/**
 * Check if a city is in our database
 */
export function isCityKnown(cityName: string): boolean {
  const normalized = cityName.toLowerCase().trim();
  return normalized in CITY_COORDINATES;
}
