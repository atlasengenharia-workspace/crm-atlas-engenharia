window.resizableTable = (function () {
    const current = { th: null, startX: 0, startWidth: 0, storageKey: '' };

    function findTh(wrapperId, index) {
        const wrapper = document.getElementById(wrapperId);
        if (!wrapper) { console.warn('[resizableTable] wrapper nao encontrado', wrapperId); return null; }
        const table = wrapper.querySelector('table');
        if (!table) { console.warn('[resizableTable] table nao encontrada'); return null; }
        const ths = table.querySelectorAll('th');
        if (index < 0 || index >= ths.length) { console.warn('[resizableTable] indice fora', index, ths.length); return null; }
        return ths[index];
    }

    function onMouseMove(e) {
        if (!current.th) return;
        const width = Math.max(30, current.startWidth + (e.clientX - current.startX));
        current.th.style.width = width + 'px';
        current.th.style.minWidth = '0px';
        const table = current.th.closest('table');
        if (table) table.style.tableLayout = 'fixed';
    }

    function onMouseUp(e) {
        if (!current.th) return;
        document.removeEventListener('mousemove', onMouseMove);
        document.removeEventListener('mouseup', onMouseUp);
        const table = current.th.closest('table');
        if (table && current.storageKey) {
            try {
                const widths = Array.from(table.querySelectorAll('th')).map(h => h.style.width || '');
                localStorage.setItem(current.storageKey, JSON.stringify(widths));
                console.log('[resizableTable] salvo', current.storageKey, widths);
            } catch { }
        }
        current.th = null;
    }

    function start(wrapperId, storageKey, thIndex, startX) {
        console.log('[resizableTable] start', wrapperId, storageKey, thIndex, startX);
        const th = findTh(wrapperId, thIndex);
        if (!th) return;

        th.style.position = 'relative';
        const table = th.closest('table');
        if (table) table.style.tableLayout = 'fixed';
        current.th = th;
        current.startX = startX;
        current.startWidth = th.offsetWidth;
        current.storageKey = storageKey;

        document.addEventListener('mousemove', onMouseMove);
        document.addEventListener('mouseup', onMouseUp);
    }

    function restore(wrapperId, storageKey) {
        console.log('[resizableTable] restore', wrapperId, storageKey);
        const wrapper = document.getElementById(wrapperId);
        if (!wrapper) { console.warn('[resizableTable] wrapper nao encontrado restore'); return; }
        const table = wrapper.querySelector('table');
        if (!table) { console.warn('[resizableTable] table nao encontrada restore'); return; }

        const ths = table.querySelectorAll('th');
        console.log('[resizableTable] ths', ths.length);

        try {
            const saved = localStorage.getItem(storageKey);
            if (saved) {
                const widths = JSON.parse(saved);
                table.style.tableLayout = 'fixed';
                ths.forEach((th, i) => {
                    if (widths[i]) {
                        th.style.width = widths[i];
                        th.style.minWidth = '0px';
                    }
                });
                console.log('[resizableTable] larguras restauradas', widths);
            }
        } catch (e) { console.warn('[resizableTable] erro ao restaurar', e); }
    }

    function autoFit(wrapperId, storageKey) {
        console.log('[resizableTable] autoFit', wrapperId, storageKey);
        const wrapper = document.getElementById(wrapperId);
        if (!wrapper) { console.warn('[resizableTable] wrapper nao encontrado autoFit'); return; }
        const table = wrapper.querySelector('table');
        if (!table) { console.warn('[resizableTable] table nao encontrada autoFit'); return; }

        const ths = Array.from(table.querySelectorAll('th'));

        // limpa larguras e deixa o navegador calcular pelo conteúdo
        table.style.tableLayout = 'auto';
        ths.forEach(th => {
            th.style.width = '';
            th.style.minWidth = '0px';
        });

        // força reflow para obter larguras calculadas
        void table.offsetWidth;

        // captura larguras e fixa
        const widths = ths.map(th => th.offsetWidth + 'px');
        table.style.tableLayout = 'fixed';
        ths.forEach((th, i) => {
            th.style.width = widths[i];
            th.style.minWidth = '0px';
        });

        // salva
        try {
            localStorage.setItem(storageKey, JSON.stringify(widths));
            console.log('[resizableTable] autoFit salvo', widths);
        } catch { }
    }

    return { start, restore, autoFit };
})();
