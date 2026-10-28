// Same-origin, per-tab handoff. Each district owns its WebGL context, so the
// neighbourhood cannot consume graphics memory while the player is in the gym.
const KEY = 'bx_city_estate_v1';
const numeric = ['hp','money','bossIdx','cryptStage','kills','specialIdx','batHits','yaw','pitch','dist'];
export function saveCityVisit(data) {
  const state = {version:1,hero:data.hero==='nyxa'?'nyxa':'brax',explore:!!data.explore,cryptOpen:!!data.cryptOpen,bat:!!data.bat};
  for(const key of numeric)if(Number.isFinite(data[key]))state[key]=data[key];
  sessionStorage.setItem(KEY,JSON.stringify(state));
  return state;
}
export function readCityVisit() {
  try {const s=JSON.parse(sessionStorage.getItem(KEY));if(!s||s.version!==1||!['brax','nyxa'].includes(s.hero))return null;
    for(const k of numeric)if(s[k]!==undefined&&!Number.isFinite(s[k]))return null;
    return s;
  } catch {return null;}
}
export function clearCityVisit() { try { sessionStorage.removeItem(KEY); } catch {} }
export function estateURL() {return new URL('./osiedle/?from=city',location.href).href;}
export function cityURL() {return new URL('../?from=osiedle',location.href).href;}
