// ============================================
// PDF Form Filler — Client-Side Interactions
// ============================================

(function () {
    'use strict';

    // --- Drag & Drop Upload ---
    const dropZone = document.getElementById('dropZone');
    const fileInput = document.getElementById('pdfFileInput');
    const fileNameSpan = document.getElementById('fileName');
    const uploadForm = document.getElementById('uploadForm');

    if (dropZone && fileInput) {
        // Prevent default drag behaviors on the whole document
        ['dragenter', 'dragover', 'dragleave', 'drop'].forEach(eventName => {
            document.body.addEventListener(eventName, e => {
                e.preventDefault();
                e.stopPropagation();
            });
        });

        // Visual feedback on drag over
        ['dragenter', 'dragover'].forEach(eventName => {
            dropZone.addEventListener(eventName, () => {
                dropZone.classList.add('drag-over');
            });
        });

        ['dragleave', 'drop'].forEach(eventName => {
            dropZone.addEventListener(eventName, () => {
                dropZone.classList.remove('drag-over');
            });
        });

        // Handle drop
        dropZone.addEventListener('drop', e => {
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                const file = files[0];
                if (file.name.toLowerCase().endsWith('.pdf')) {
                    // Transfer file to the hidden input
                    const dt = new DataTransfer();
                    dt.items.add(file);
                    fileInput.files = dt.files;
                    showFileName(file.name);
                    // Auto-submit after a brief delay for visual feedback
                    setTimeout(() => uploadForm.submit(), 400);
                } else {
                    showFileName('⚠ Only PDF files are accepted');
                }
            }
        });

        // Handle file input change (click to browse)
        fileInput.addEventListener('change', () => {
            if (fileInput.files.length > 0) {
                showFileName(fileInput.files[0].name);
                setTimeout(() => uploadForm.submit(), 400);
            }
        });
    }

    function showFileName(name) {
        if (fileNameSpan) {
            fileNameSpan.textContent = name;
            fileNameSpan.style.opacity = '0';
            requestAnimationFrame(() => {
                fileNameSpan.style.transition = 'opacity 0.3s';
                fileNameSpan.style.opacity = '1';
            });
        }
    }

    // --- Auto-dismiss toast ---
    const toast = document.getElementById('errorToast');
    if (toast) {
        setTimeout(() => {
            toast.style.transition = 'opacity 0.4s, transform 0.4s';
            toast.style.opacity = '0';
            toast.style.transform = 'translateY(12px)';
            setTimeout(() => toast.remove(), 400);
        }, 6000);
    }

    // --- Submit loading state (fetch-based download) ---
    const fieldForm = document.getElementById('fieldForm');
    if (fieldForm) {
        fieldForm.addEventListener('submit', function (e) {
            e.preventDefault();

            const btn = fieldForm.querySelector('.btn-primary');
            if (!btn) return;

            const originalHtml = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML = `
                <svg class="spinner btn-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <circle cx="12" cy="12" r="10" stroke-dasharray="60" stroke-dashoffset="20"/>
                </svg>
                Processing...
            `;

            const formData = new FormData(fieldForm);

            fetch(fieldForm.action, {
                method: 'POST',
                body: formData
            })
            .then(response => {
                const contentType = response.headers.get('Content-Type') || '';

                // Server returns JSON with an error field on failure
                if (!response.ok || contentType.includes('application/json')) {
                    return response.json().then(data => {
                        throw new Error(data.error || 'Download failed');
                    });
                }

                // Extract filename from Content-Disposition header
                const disposition = response.headers.get('Content-Disposition') || '';
                let filename = 'filled.pdf';
                // Handle: filename="name.pdf" or filename=name.pdf or filename*=UTF-8''name.pdf
                const quoted = disposition.match(/filename="([^"]+)"/i);
                const unquoted = disposition.match(/filename=([^;\s"]+)/i);
                const star = disposition.match(/filename\*=UTF-8''([^;\s]+)/i);
                if (star) filename = decodeURIComponent(star[1]);
                else if (quoted) filename = quoted[1];
                else if (unquoted) filename = unquoted[1];

                return response.blob().then(blob => ({ blob, filename }));
            })
            .then(({ blob, filename }) => {
                // Create a temporary link and trigger the download
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url;
                a.download = filename;
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                URL.revokeObjectURL(url);
            })
            .catch(err => {
                console.error('Download error:', err);
                alert(err.message || 'Download failed. Please try again.');
            })
            .finally(() => {
                btn.disabled = false;
                btn.innerHTML = originalHtml;
            });
        });
    }
})();

