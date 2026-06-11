(function () {
  'use strict';
  var picker = document.getElementById('adminApplicationProgramPicker');
  var programCode = document.getElementById('ProgramCode');
  var programName = document.getElementById('ProgramName');
  var form = document.getElementById('studentApplicationAdminForm');

  if (!picker || !programCode || !programName) {
    return;
  }

  function syncProgramFields() {
    var opt = picker.options[picker.selectedIndex];
    if (!opt || !opt.value) {
      programCode.value = '';
      programName.value = '';
      picker.classList.add('is-invalid');
      return;
    }
    picker.classList.remove('is-invalid');
    programCode.value = opt.getAttribute('data-code') || '';
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
