mergeInto(LibraryManager.library, {
  BXNovaMusicState: function(pointer) {
    try { window.BraxhornNovaMusic = JSON.parse(UTF8ToString(pointer)); }
    catch (e) { console.error('NOVA audio state', e); }
  }
});
