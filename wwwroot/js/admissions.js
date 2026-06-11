// Admissions apply page — program picker sync and validation helpers
(function () {
  'use strict';

  var picker = document.getElementById('applicationProgramPicker');
  var programCode = document.getElementById('ProgramCode');
  var programName = document.getElementById('ProgramName');
  var gradeInput = document.getElementById('DesiredGradeOrSemester');
  var applicationForm = document.getElementById('studentApplicationForm');

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

    var maxLevel = parseInt(opt.getAttribute('data-max-level'), 10);
    if (!isNaN(maxLevel) && gradeInput) {
      gradeInput.max = String(maxLevel);
      if (parseInt(gradeInput.value, 10) > maxLevel) {
        gradeInput.value = String(maxLevel);
      }
    }
  }

  picker.addEventListener('change', syncProgramFields);
  syncProgramFields();

  if (applicationForm) {
    applicationForm.addEventListener('submit', function (e) {
      syncProgramFields();
      if (!programCode.value) {
        e.preventDefault();
        e.stopPropagation();
        picker.classList.add('is-invalid');
        picker.focus();
        return;
      }

      if (window.jQuery && window.jQuery.fn.validate) {
        var valid = window.jQuery(applicationForm).valid();
        if (!valid) {
          e.preventDefault();
          var firstError = applicationForm.querySelector('.input-validation-error, .is-invalid');
          if (firstError) {
            firstError.focus();
          }
        }
      }
    });
  }

  if (applicationForm && applicationForm.querySelector('.alert-danger')) {
    applicationForm.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
})();
