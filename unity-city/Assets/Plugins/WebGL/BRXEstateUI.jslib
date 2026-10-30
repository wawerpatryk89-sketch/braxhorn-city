// BRAXHORN Estate WebGL -> HTML source UI bridge.
mergeInto(LibraryManager.library, {
  BRX_EstateReady: function() {
    if (typeof window !== "undefined" && typeof window.BrxUnityEstateReady === "function")
      window.BrxUnityEstateReady();
  },
  BRX_HeroLoaded: function() {
    if (typeof window !== "undefined" && typeof window.BrxUnityHeroReady === "function")
      window.BrxUnityHeroReady();
  }
});
