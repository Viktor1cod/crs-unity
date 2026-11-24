mergeInto(LibraryManager.library, {
  CRS_IsRussian: function () {
    return window.CRSPlatform && window.CRSPlatform.language === 'ru' ? 1 : 0;
  }
});
