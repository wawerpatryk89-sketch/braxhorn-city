mergeInto(LibraryManager.library, {
  BXRoomState: function(pointer) {
    try {
      var state = JSON.parse(UTF8ToString(pointer));
      window.BraxhornRoomState = state;
      if (window.onBraxhornRoomState) window.onBraxhornRoomState(state);
    } catch (error) {
      console.error('Braxhorn room diagnostics bridge', error);
    }
  }
});
