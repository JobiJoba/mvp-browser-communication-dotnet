window.mudThemeLocalStorage = {
  get: function (key) {
    try {
      return localStorage.getItem(key);
    } catch {
      return null;
    }
  },
  set: function (key, value) {
    try {
      localStorage.setItem(key, value);
    } catch {
      // ignore quota / private mode errors in this demo
    }
  }
};
