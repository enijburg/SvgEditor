window.svgEditorFile = {
    openFilePicker: function () {
        return new Promise(function (resolve) {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = '.svg,image/svg+xml';
            input.onchange = function () {
                const file = input.files[0];
                if (!file) { resolve(null); return; }
                const reader = new FileReader();
                reader.onload = function (e) { resolve(e.target.result); };
                reader.onerror = function () { resolve(null); };
                reader.readAsText(file);
            };
            input.click();
        });
    },
    downloadFile: function (filename, content) {
        const blob = new Blob([content], { type: 'image/svg+xml' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(url);
    }
};
