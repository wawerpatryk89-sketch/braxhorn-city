mergeInto(LibraryManager.library, {
  BXNutritionState: function(pointer) {
    try { window.BraxhornNutritionState = JSON.parse(UTF8ToString(pointer)); }
    catch (error) { console.error('Braxhorn Nutrition state bridge', error); }
  }
});
