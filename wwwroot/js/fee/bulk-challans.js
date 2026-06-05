(function () {
    const programSelect = document.getElementById('bulkProgramId');
    const semesterSelect = document.getElementById('bulkSemester');
    const academicYearInput = document.getElementById('bulkAcademicYear');
    const structureSelect = document.getElementById('bulkStructureId');
    const issueDateInput = document.getElementById('bulkIssueDate');
    const dueDateInput = document.getElementById('bulkDueDate');
    const loadStudentsBtn = document.getElementById('bulkLoadStudentsBtn');
    const filterError = document.getElementById('bulkFilterError');
    const studentSection = document.getElementById('bulkStudentSection');
    const actionSection = document.getElementById('bulkActionSection');
    const resultSection = document.getElementById('bulkResultSection');
    const studentTableBody = document.getElementById('bulkStudentTableBody');
    const selectAllCheckbox = document.getElementById('bulkSelectAll');
    const selectionCount = document.getElementById('bulkSelectionCount');
    const generateWholeBtn = document.getElementById('bulkGenerateWholeBtn');
    const generateSelectedBtn = document.getElementById('bulkGenerateSelectedBtn');
    const resultTableBody = document.getElementById('bulkResultTableBody');
    const resultMessage = document.getElementById('bulkResultMessage');

    let loadedStudents = [];

    function showFilterError(message) {
        filterError.textContent = message;
        filterError.classList.remove('d-none');
    }

    function clearFilterError() {
        filterError.textContent = '';
        filterError.classList.add('d-none');
    }

    function readFilters() {
        return {
            programId: Number(programSelect?.value ?? 0),
            semester: semesterSelect?.value ?? '',
            academicYear: Number(academicYearInput?.value ?? 0),
            structureId: Number(structureSelect?.value ?? 0),
            issueDate: issueDateInput?.value ?? '',
            dueDate: dueDateInput?.value ?? ''
        };
    }

    function validateFilters(requireStructure) {
        const filters = readFilters();
        if (!filters.programId) {
            showFilterError('Program is required.');
            return null;
        }

        if (!filters.semester) {
            showFilterError('Semester is required.');
            return null;
        }

        if (!filters.academicYear || filters.academicYear < 1900 || filters.academicYear > 9999) {
            showFilterError('Academic year must be a valid 4-digit year.');
            return null;
        }

        if (requireStructure && !filters.structureId) {
            showFilterError('Fee structure is required.');
            return null;
        }

        if (filters.issueDate && filters.dueDate && filters.issueDate > filters.dueDate) {
            showFilterError('Issue date must be on or before due date.');
            return null;
        }

        clearFilterError();
        return filters;
    }

    async function fetchJson(url, options) {
        const response = await fetch(url, {
            credentials: 'same-origin',
            headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
            ...options
        });

        const payload = await response.json().catch(() => ({}));
        if (!response.ok) {
            throw new Error(payload.message || 'Request failed.');
        }

        return payload;
    }

    async function loadStructures() {
        const filters = validateFilters(false);
        if (!filters) {
            return;
        }

        structureSelect.disabled = true;
        structureSelect.innerHTML = '<option value="">Loading...</option>';

        try {
            const params = new URLSearchParams({
                programId: String(filters.programId),
                semester: filters.semester,
                academicYear: String(filters.academicYear)
            });
            const structures = await fetchJson(`/api/challans/structures?${params.toString()}`);
            structureSelect.innerHTML = '<option value="">— Select structure —</option>';
            structures.forEach((item) => {
                const option = document.createElement('option');
                option.value = item.id;
                option.textContent = item.name;
                structureSelect.appendChild(option);
            });
            structureSelect.disabled = structures.length === 0;
            if (structures.length === 0) {
                structureSelect.innerHTML = '<option value="">No structures for this filter</option>';
            }
        } catch (error) {
            structureSelect.innerHTML = '<option value="">Failed to load structures</option>';
            showFilterError(error.message);
        }
    }

    function updateSelectionCount() {
        const eligibleCheckboxes = Array.from(document.querySelectorAll('.bulk-student-check:not(:disabled)'));
        const selectedCount = eligibleCheckboxes.filter((cb) => cb.checked).length;
        selectionCount.textContent = `${selectedCount} of ${eligibleCheckboxes.length} students selected`;
        selectAllCheckbox.indeterminate = selectedCount > 0 && selectedCount < eligibleCheckboxes.length;
        selectAllCheckbox.checked = eligibleCheckboxes.length > 0 && selectedCount === eligibleCheckboxes.length;
    }

    function renderStudents(students) {
        loadedStudents = students;
        studentTableBody.innerHTML = '';

        students.forEach((student, index) => {
            const row = document.createElement('tr');
            if (student.alreadyHasChallan) {
                row.classList.add('table-secondary', 'text-muted');
            }

            row.innerHTML = `
                <td>
                    <input type="checkbox"
                           class="form-check-input bulk-student-check"
                           data-student-id="${student.studentId}"
                           ${student.alreadyHasChallan ? 'disabled' : 'checked'} />
                </td>
                <td>${index + 1}</td>
                <td>${student.registrationNo}</td>
                <td>${student.rollNo ?? '—'}</td>
                <td>${student.studentName}</td>
                <td>${student.programName}</td>
                <td>${student.hasConcession ? 'Yes' : 'No'}</td>
                <td>${student.alreadyHasChallan ? 'Yes' : 'No'}</td>
            `;
            studentTableBody.appendChild(row);
        });

        studentSection.classList.remove('d-none');
        actionSection.classList.remove('d-none');
        updateSelectionCount();
    }

    async function loadStudents() {
        const filters = validateFilters(true);
        if (!filters) {
            return;
        }

        loadStudentsBtn.disabled = true;
        loadStudentsBtn.textContent = 'Loading...';

        try {
            const params = new URLSearchParams({
                programId: String(filters.programId),
                semester: filters.semester,
                academicYear: String(filters.academicYear)
            });
            const students = await fetchJson(`/api/challans/bulk-eligible-students?${params.toString()}`);
            renderStudents(students);
        } catch (error) {
            showFilterError(error.message);
        } finally {
            loadStudentsBtn.disabled = false;
            loadStudentsBtn.textContent = 'Load students';
        }
    }

    function getSelectedStudentIds() {
        return Array.from(document.querySelectorAll('.bulk-student-check:checked:not(:disabled)'))
            .map((cb) => Number(cb.dataset.studentId))
            .filter((id) => id > 0);
    }

    function buildPayload(studentIds) {
        const filters = validateFilters(true);
        if (!filters) {
            return null;
        }

        return {
            programId: filters.programId,
            structureId: filters.structureId,
            semester: filters.semester,
            academicYear: filters.academicYear,
            issueDate: filters.issueDate,
            dueDate: filters.dueDate,
            studentIds
        };
    }

    function statusRowClass(status) {
        if (!status) {
            return '';
        }

        if (status.toLowerCase() === 'generated') {
            return 'table-success';
        }

        if (status.toLowerCase().startsWith('skipped')) {
            return 'table-warning';
        }

        if (status.toLowerCase().startsWith('error')) {
            return 'table-danger';
        }

        return '';
    }

    function renderResults(payload) {
        document.getElementById('bulkTotalProcessed').textContent = String(payload.totalProcessed ?? 0);
        document.getElementById('bulkTotalGenerated').textContent = String(payload.totalGenerated ?? 0);
        document.getElementById('bulkTotalSkipped').textContent = String(payload.totalSkipped ?? 0);
        document.getElementById('bulkTotalErrors').textContent = String(payload.totalErrors ?? 0);

        resultMessage.textContent = `${payload.totalGenerated ?? 0} challans generated successfully. ${payload.totalSkipped ?? 0} skipped.`;
        resultMessage.classList.remove('d-none');

        resultTableBody.innerHTML = '';
        (payload.results ?? []).forEach((row) => {
            const tr = document.createElement('tr');
            tr.className = statusRowClass(row.status);
            tr.innerHTML = `
                <td>${row.registrationNo ?? ''}</td>
                <td>${row.studentName ?? ''}</td>
                <td>${row.challanNo ?? '—'}</td>
                <td>${row.netPayable != null ? Number(row.netPayable).toFixed(2) : '—'}</td>
                <td>${row.status ?? ''}</td>
            `;
            resultTableBody.appendChild(tr);
        });

        resultSection.classList.remove('d-none');
    }

    async function generateChallans(studentIds, confirmCount, modeLabel) {
        const payload = buildPayload(studentIds);
        if (!payload) {
            return;
        }

        if (modeLabel === 'selected' && (!studentIds || studentIds.length === 0)) {
            showFilterError('At least one student must be selected.');
            return;
        }

        const filters = readFilters();
        const programName = programSelect?.selectedOptions?.[0]?.textContent ?? 'program';
        const message = `You are about to generate challans for ${confirmCount} student(s) for ${programName}, ${filters.semester} ${filters.academicYear}. Confirm?`;
        if (!window.confirm(message)) {
            return;
        }

        clearFilterError();
        generateWholeBtn.disabled = true;
        generateSelectedBtn.disabled = true;

        try {
            const response = await fetchJson('/api/challans/bulk-generate', {
                method: 'POST',
                body: JSON.stringify(payload)
            });
            renderResults(response);
            await loadStudents();
        } catch (error) {
            showFilterError(error.message);
        } finally {
            generateWholeBtn.disabled = false;
            generateSelectedBtn.disabled = false;
        }
    }

    programSelect?.addEventListener('change', () => {
        studentSection.classList.add('d-none');
        actionSection.classList.add('d-none');
        resultSection.classList.add('d-none');
        loadStructures();
    });

    semesterSelect?.addEventListener('change', loadStructures);
    academicYearInput?.addEventListener('change', loadStructures);

    loadStudentsBtn?.addEventListener('click', loadStudents);

    selectAllCheckbox?.addEventListener('change', () => {
        const checked = selectAllCheckbox.checked;
        document.querySelectorAll('.bulk-student-check:not(:disabled)').forEach((cb) => {
            cb.checked = checked;
        });
        updateSelectionCount();
    });

    studentTableBody?.addEventListener('change', (event) => {
        if (event.target.classList.contains('bulk-student-check')) {
            updateSelectionCount();
        }
    });

    generateWholeBtn?.addEventListener('click', () => {
        const eligibleCount = loadedStudents.filter((s) => !s.alreadyHasChallan).length;
        generateChallans(null, eligibleCount, 'whole');
    });

    generateSelectedBtn?.addEventListener('click', () => {
        const selectedIds = getSelectedStudentIds();
        generateChallans(selectedIds, selectedIds.length, 'selected');
    });
})();
