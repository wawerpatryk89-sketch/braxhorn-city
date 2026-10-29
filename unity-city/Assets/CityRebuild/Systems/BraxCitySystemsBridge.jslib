mergeInto(LibraryManager.library, {
  BXCitySystemsState: function(pointer) {
    try {
      window.BraxhornCitySystems = JSON.parse(UTF8ToString(pointer));
    } catch (error) {
      console.error('Braxhorn city systems telemetry', error);
    }
  }
});
