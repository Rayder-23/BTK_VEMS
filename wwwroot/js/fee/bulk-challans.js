(function () {
    const apiBase = '/api/challans';

    function getEl(id) {
        return document.getElementById(id);
    }

    function setText(el, value) {
        if (el) {
            el.textContent = value;
        }
    }

    const programSelect = getEl('bulkProgramId');
    const structureSelect = getEl('bulkStructureId');
    const issueDateInput = getEl('bulkIssueDate');
    const dueDateInput = getEl('bulkDueDate');
    const loadStudentsBtn = getEl('bulkLoadStudentsBtn');
    const filterError = getEl('bulkFilterError');
    const studentTableBody = getEl('bulkStudentTableBody');
    const selectAllCheckbox = getEl('bulkSelectAll');
    const selectionCount = getEl('bulkSelectionCount');
    const selectedCountLabel = getEl('bulkSelectedCount');
    const generateSelectedBtn = getEl('bulkGenerateSelectedBtn');

    if (!programSelect || !studentTableBody) {
        return;
    }

    let loadedStudents = [];
    let selectedProgramId = 0;

    function showFilterError(message) {
        if (!filterError) {
            return;
        }

        filterError.textContent = message;
        filterError.classList.remove('d-none');
    }

    function clearFilterError() {
        if (!filterError) {
            return;
        }

        filterError.textContent = '';
        filterError.classList.add('d-none');
    }

    function showPlaceholder(message) {
        loadedStudents = [];
        studentTableBody.innerHTML = `
            <tr id="bulkStudentPlaceholder">
                <td colspan="8" class="text-center text-muted py-4">${message}</td>
            </tr>`;
        if (selectAllCheckbox) {
            selectAllCheckbox.checked = false;
            selectAllCheckbox.indeterminate = false;
            selectAllCheckbox.disabled = true;
        }
        updateSelectionCount();
    }

    function readFilters() {
        return {
            programId: Number(programSelect.value || 0),
            structureId: Number(structureSelect?.value || 0),
            issueDate: issueDateInput?.value ?? '',
            dueDate: dueDateInput?.value ?? ''
        };
    }

    async function loadStructures(programId) {
        if (!structureSelect) {
            return;
        }

        structureSelect.disabled = true;

        if (!programId) {
            structureSelect.innerHTML = '<option value="">— Select program first —</option>';
            return;
        }

        structureSelect.innerHTML = '<option value="">Loading...</option>';

        try {
            const structures = await fetchJson(`${apiBase}/program-structures?programId=${programId}`);
            if (!Array.isArray(structures) || structures.length === 0) {
                structureSelect.innerHTML = '<option value="">No fee structure for this program</option>';
                showFilterError('No active fee structure with line items found for this program.');
                return;
            }

            structureSelect.innerHTML = '<option value="">— All / latest fee structure —</option>';
            structures.forEach((item) => {
                const option = document.createElement('option');
                option.value = item.id;
                option.textContent = item.name;
                structureSelect.appendChild(option);
            });
            structureSelect.disabled = false;
            clearFilterError();
        } catch (error) {
            structureSelect.innerHTML = '<option value="">Failed to load fee structures</option>';
            showFilterError(error.message || 'Failed to load fee structures.');
        }
    }

    function validateProgram() {
        const programId = Number(programSelect.value || 0);
        if (!programId) {
            showFilterError('Program is required.');
            return 0;
        }

        clearFilterError();
        return programId;
    }

    function validateDates(issueDate, dueDate) {
        if (issueDate && dueDate && issueDate > dueDate) {
            showFilterError('Issue date must be on or before due date.');
            return false;
        }

        return true;
    }

    async function fetchJson(url, options) {
        let response;
        try {
            response = await fetch(url, {
                credentials: 'same-origin',
                headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
                ...options
            });
        } catch {
            throw new Error('Network error. Check your connection and try again.');
        }

        const payload = await response.json().catch(() => null);
        if (!response.ok) {
            throw new Error(payload?.message || `Server returned ${response.status}.`);
        }

        if (payload === null) {
            throw new Error('Server returned an invalid response. Sign in again and retry.');
        }

        return payload;
    }

    function getSelectedStudentIds() {
        return Array.from(document.querySelectorAll('.bulk-student-check:checked:not(:disabled)'))
            .map((cb) => Number(cb.dataset.studentId))
            .filter((id) => id > 0);
    }

    function updateSelectionCount() {
        const eligibleCheckboxes = Array.from(document.querySelectorAll('.bulk-student-check:not(:disabled)'));
        const selectedCount = getSelectedStudentIds().length;

        setText(
            selectionCount,
            loadedStudents.length === 0
                ? 'Select a program, then search.'
                : `${selectedCount} of ${eligibleCheckboxes.length} students selected`
        );

        setText(selectedCountLabel, String(selectedCount));

        if (generateSelectedBtn) {
            generateSelectedBtn.disabled = selectedCount === 0;
        }

        if (selectAllCheckbox) {
            selectAllCheckbox.disabled = eligibleCheckboxes.length === 0;
            selectAllCheckbox.indeterminate = selectedCount > 0 && selectedCount < eligibleCheckboxes.length;
            selectAllCheckbox.checked = eligibleCheckboxes.length > 0 && selectedCount === eligibleCheckboxes.length;
        }
    }

    function renderStudents(students) {
        loadedStudents = students;
        studentTableBody.innerHTML = '';

        if (students.length === 0) {
            showPlaceholder('No active students found for the selected program.');
            return;
        }

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
                           ${student.alreadyHasChallan ? 'disabled' : ''} />
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

        updateSelectionCount();
    }

    async function loadStudents(options) {
        const preserveResults = options?.preserveResults === true;
        const programId = validateProgram();
        if (!programId) {
            return;
        }

        const filters = readFilters();
        if (!validateDates(filters.issueDate, filters.dueDate)) {
            return;
        }

        if (!loadStudentsBtn) {
            return;
        }

        loadStudentsBtn.disabled = true;
        loadStudentsBtn.textContent = 'Searching...';
        if (!preserveResults) {
            getEl('bulkResultSection')?.classList.add('d-none');
        }

        try {
            const structureId = Number(structureSelect?.value || 0);
            const payload = await fetchJson(`${apiBase}/bulk-eligible-students?programId=${programId}&structureId=${structureId}`);
            const students = Array.isArray(payload.students) ? payload.students : [];
            selectedProgramId = programId;
            renderStudents(students);
        } catch (error) {
            showFilterError(error.message || 'Failed to load students.');
            showPlaceholder('Search failed. Check the program has an active fee structure.');
        } finally {
            loadStudentsBtn.disabled = false;
            loadStudentsBtn.textContent = 'Search';
        }
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
        if (!payload || typeof payload !== 'object') {
            showFilterError('Generation finished but the server response was invalid.');
            return;
        }

        const resultSection = getEl('bulkResultSection');
        const resultTableBody = getEl('bulkResultTableBody');
        const resultMessage = getEl('bulkResultMessage');

        if (!resultSection || !resultTableBody) {
            showFilterError('Challans were generated but the summary panel could not be shown. Refresh the page to see new challans.');
            return;
        }

        setText(getEl('bulkTotalProcessed'), String(payload.totalProcessed ?? 0));
        setText(getEl('bulkTotalGenerated'), String(payload.totalGenerated ?? 0));
        setText(getEl('bulkTotalSkipped'), String(payload.totalSkipped ?? 0));
        setText(getEl('bulkTotalErrors'), String(payload.totalErrors ?? 0));

        if (resultMessage) {
            const generated = payload.totalGenerated ?? 0;
            const skipped = payload.totalSkipped ?? 0;
            const errors = payload.totalErrors ?? 0;

            if (generated > 0) {
                resultMessage.textContent = errors > 0
                    ? `Successfully generated ${generated} challan(s). ${skipped} skipped, ${errors} error(s).`
                    : `Successfully generated ${generated} challan(s).${skipped > 0 ? ` ${skipped} skipped.` : ''}`;
                resultMessage.className = 'alert alert-success py-2';
            } else {
                resultMessage.textContent = `No challans were generated.${skipped > 0 ? ` ${skipped} skipped.` : ''}`;
                resultMessage.className = 'alert alert-warning py-2';
            }

            resultMessage.classList.remove('d-none');
        }

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

    async function generateChallans(studentIds) {
        const programId = selectedProgramId || validateProgram();
        if (!programId) {
            return;
        }

        const filters = readFilters();
        if (!validateDates(filters.issueDate, filters.dueDate)) {
            return;
        }

        if (!studentIds || studentIds.length === 0) {
            showFilterError('Select at least one student using the checkboxes.');
            return;
        }

        const programName = programSelect.selectedOptions?.[0]?.textContent ?? 'program';
        const message = `Create challans for ${studentIds.length} selected student(s) in ${programName}?`;
        if (!window.confirm(message)) {
            return;
        }

        clearFilterError();
        if (generateSelectedBtn) {
            generateSelectedBtn.disabled = true;
        }

        try {
            const response = await fetchJson(`${apiBase}/bulk-generate`, {
                method: 'POST',
                body: JSON.stringify({
                    programId,
                    structureId: filters.structureId,
                    issueDate: filters.issueDate,
                    dueDate: filters.dueDate,
                    studentIds
                })
            });
            renderResults(response);
            await loadStudents({ preserveResults: true });
            getEl('bulkResultSection')?.scrollIntoView({ behavior: 'smooth', block: 'start' });
        } catch (error) {
            showFilterError(error.message || 'Failed to generate challans.');
        } finally {
            updateSelectionCount();
        }
    }

    programSelect.addEventListener('change', () => {
        selectedProgramId = 0;
        showPlaceholder('Program changed. Click Search to load students.');
        getEl('bulkResultSection')?.classList.add('d-none');
        clearFilterError();
        loadStructures(Number(programSelect.value || 0));
    });

    loadStudentsBtn?.addEventListener('click', loadStudents);

    selectAllCheckbox?.addEventListener('change', () => {
        const checked = selectAllCheckbox.checked;
        document.querySelectorAll('.bulk-student-check:not(:disabled)').forEach((cb) => {
            cb.checked = checked;
        });
        updateSelectionCount();
    });

    studentTableBody.addEventListener('change', (event) => {
        if (event.target.classList.contains('bulk-student-check')) {
            updateSelectionCount();
        }
    });

    generateSelectedBtn?.addEventListener('click', () => {
        generateChallans(getSelectedStudentIds());
    });

    updateSelectionCount();
})();
