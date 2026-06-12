(function () {
  'use strict';
  var picker = document.getElementById('adminApplicationProgramPicker');
  var programCode = document.getElementById('ProgramCode');
  var programName = document.getElementById('ProgramName');
  var desiredYear = document.getElementById('Form_DesiredYear');
  var paymentAmount = document.getElementById('Form_PaymentAmount');
  var pickBtn = document.getElementById('pickAdmissionPaymentBtn');
  var pickMessage = document.getElementById('pickPaymentMessage');
  var statusSelect = document.getElementById('Form_ApplicationStatus');
  var convertBtn = document.getElementById('convertAsStudentBtn');
  var form = document.getElementById('studentApplicationAdminForm');
  var approvedStatus = 'Approved';

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

  function showPickMessage(text) {
    if (!pickMessage) {
      return;
    }
    if (!text) {
      pickMessage.textContent = '';
      pickMessage.classList.add('d-none');
      return;
    }
    pickMessage.textContent = text;
    pickMessage.classList.remove('d-none');
  }

  picker.addEventListener('change', syncProgramFields);
  syncProgramFields();

  function syncConvertButton() {
    if (!statusSelect || !convertBtn) {
      return;
    }
    if (statusSelect.value === approvedStatus) {
      convertBtn.classList.remove('d-none');
    } else {
      convertBtn.classList.add('d-none');
    }
  }

  if (statusSelect) {
    statusSelect.addEventListener('change', syncConvertButton);
    syncConvertButton();
  }

  if (pickBtn && paymentAmount && desiredYear) {
    pickBtn.addEventListener('click', function () {
      syncProgramFields();
      var code = programCode.value.trim();
      var year = desiredYear.value.trim();

      if (!code || !year) {
        paymentAmount.value = '0';
        showPickMessage('admission fee not exist');
        return;
      }

      pickBtn.disabled = true;
      var url = '/adminportal/admissions/applications/pick-admission-fee'
        + '?programCode=' + encodeURIComponent(code)
        + '&academicYear=' + encodeURIComponent(year);

      fetch(url, { headers: { Accept: 'application/json' } })
        .then(function (response) {
          if (!response.ok) {
            throw new Error('Request failed');
          }
          return response.json();
        })
        .then(function (data) {
          paymentAmount.value = String(data.amount ?? 0);
          showPickMessage(data.found ? '' : (data.message || 'admission fee not exist'));
        })
        .catch(function () {
          paymentAmount.value = '0';
          showPickMessage('admission fee not exist');
        })
        .finally(function () {
          pickBtn.disabled = false;
        });
    });
  }

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
