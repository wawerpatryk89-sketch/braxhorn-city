mergeInto(LibraryManager.library, {
  BXState: function(pointer) {
    try {
      var state = JSON.parse(UTF8ToString(pointer));
      window.BraxhornState = state;
      if (window.onBraxhornState) window.onBraxhornState(state);
    } catch (e) { console.error('Braxhorn state bridge', e); }
  }
});
