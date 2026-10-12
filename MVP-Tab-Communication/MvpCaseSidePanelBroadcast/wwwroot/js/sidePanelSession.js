const SNAPSHOT_KEY = "case-side-panel-snapshot";

export function loadSnapshot() {
  try {
    return sessionStorage.getItem(SNAPSHOT_KEY);
  } catch {
    return null;
  }
}

export function saveSnapshot(json) {
  try {
    sessionStorage.setItem(SNAPSHOT_KEY, json);
  } catch {
    // ignore quota / private mode errors in this demo
  }
}

export function clearSnapshot() {
  try {
    sessionStorage.removeItem(SNAPSHOT_KEY);
  } catch {
    // ignore
  }
}
