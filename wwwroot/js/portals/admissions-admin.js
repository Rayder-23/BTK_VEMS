(function () {
  'use strict';
  var picker = document.getElementById('adminApplicationProgramPicker');
  var programCode = document.getElementById('ProgramCode');
  var instTypeCode = document.getElementById('InstTypeCode');
  var programName = document.getElementById('ProgramName');
  var form = document.getElementById('studentApplicationAdminForm');

  if (!picker || !programCode || !instTypeCode || !programName) {
    return;
  }

  function syncProgramFields() {
    var opt = picker.options[picker.selectedIndex];
    if (!opt || !opt.value) {
      programCode.value = '';
      instTypeCode.value = '';
      programName.value = '';
      picker.classList.add('is-invalid');
      return;
    }
    picker.classList.remove('is-invalid');
    programCode.value = opt.getAttribute('data-code') || '';
    instTypeCode.value = opt.getAttribute('data-inst') || '';
    programName.value = opt.value || '';
  }

  picker.addEventListener('change', syncProgramFields);
  syncProgramFields();

  if (form) {
    form.addEventListener('submit', function (e) {
      syncProgramFields();
      if (!programCode.value) {
        e.preventDefault();
        picker.classList.add('is-invalid');
        picker.focus();
      }
    });
  }
})();
