window.dashboardCharts = {
    masterChart: null,
    vendasChart: null,
    qtdChart: null,
    evoChart: null,
    stackChart: null,
    costEvoChart: null,
    costCatChart: null,
    arecChart: null,
    donutChart: null,

    renderMasterChart: function (canvasId, labels, faturamento, custoDireto, custoIndireto, resultado) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.masterChart && canvasId === 'masterChartCanvas') {
            this.masterChart.destroy();
            this.masterChart = null;
        }

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        type: 'bar',
                        label: 'Faturamento',
                        data: faturamento,
                        backgroundColor: '#171E32',
                        borderRadius: 4,
                        stack: 'f'
                    },
                    {
                        type: 'bar',
                        label: 'Custo direto',
                        data: custoDireto,
                        backgroundColor: '#C8432F',
                        borderRadius: 0,
                        stack: 'c'
                    },
                    {
                        type: 'bar',
                        label: 'Custo indireto',
                        data: custoIndireto,
                        backgroundColor: '#DE9427',
                        borderRadius: 4,
                        stack: 'c'
                    },
                    {
                        type: 'line',
                        label: 'Resultado',
                        data: resultado,
                        borderColor: '#1F9D66',
                        backgroundColor: '#1F9D66',
                        borderWidth: 2.5,
                        pointRadius: 4,
                        pointHoverRadius: 6,
                        tension: 0.35
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: {
                    mode: 'index',
                    intersect: false
                },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: {
                            usePointStyle: true,
                            boxWidth: 8,
                            padding: 15,
                            font: { family: 'Inter', size: 12, weight: '500' }
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        footerFont: { family: 'Inter', size: 12, weight: 'bold' },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + fmtFull(context.parsed.y || 0);
                            },
                            footer: function (tooltipItems) {
                                if (!tooltipItems || tooltipItems.length === 0) return '';
                                const dataIndex = tooltipItems[0].dataIndex;
                                const fat = faturamento[dataIndex] || 0;
                                const res = resultado[dataIndex] || 0;
                                if (fat > 0) {
                                    const margem = ((res / fat) * 100).toFixed(1);
                                    return 'Margem: ' + margem + '%';
                                }
                                return '';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        stacked: true,
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) {
                                return fmtCurrency(value);
                            }
                        }
                    },
                    x: {
                        stacked: true,
                        grid: { display: false },
                        ticks: {
                            font: { family: 'Inter', size: 11 }
                        }
                    }
                }
            }
        });

        if (canvasId === 'masterChartCanvas') {
            this.masterChart = newChart;
        }

        return true;
    },

    renderVendasChart: function (canvasId, labels, avcb, clcb, procAdm, obras, mediaMovel) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.vendasChart && canvasId === 'vendasChartCanvas') {
            this.vendasChart.destroy();
            this.vendasChart = null;
        }

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { type: 'bar', label: 'AVCB', data: avcb, backgroundColor: '#C8432F', borderRadius: 2, stack: 'v' },
                    { type: 'bar', label: 'CLCB', data: clcb, backgroundColor: '#DE9427', borderRadius: 2, stack: 'v' },
                    { type: 'bar', label: 'Proc. Adm', data: procAdm, backgroundColor: '#6B46C1', borderRadius: 2, stack: 'v' },
                    { type: 'bar', label: 'Obras', data: obras, backgroundColor: '#2B6CB0', borderRadius: 2, stack: 'v' },
                    { type: 'line', label: 'Média móvel (3)', data: mediaMovel, borderColor: '#C8432F', borderWidth: 2.5, pointRadius: 0, tension: 0.35 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: { usePointStyle: true, boxWidth: 8, padding: 12, font: { family: 'Inter', size: 11, weight: '500' } }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + fmtFull(context.parsed.y || 0);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        stacked: true,
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) { return fmtCurrency(value); }
                        }
                    },
                    x: {
                        stacked: true,
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'vendasChartCanvas') {
            this.vendasChart = newChart;
        }
        return true;
    },

    renderQtdChart: function (canvasId, labels, periodAtual, periodAnterior) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.qtdChart && canvasId === 'qtdChartCanvas') {
            this.qtdChart.destroy();
            this.qtdChart = null;
        }

        const lineColors = ['#C8432F', '#DE9427', '#6B46C1', '#2B6CB0'];

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    {
                        label: 'Período atual',
                        data: periodAtual,
                        backgroundColor: function (context) {
                            return lineColors[context.dataIndex % lineColors.length];
                        },
                        borderRadius: 4
                    },
                    {
                        label: 'Período anterior',
                        data: periodAnterior,
                        backgroundColor: '#C7CCD8',
                        borderRadius: 4
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: { usePointStyle: true, boxWidth: 8, padding: 12, font: { family: 'Inter', size: 11, weight: '500' } }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        footerFont: { family: 'Inter', size: 12, weight: 'bold' },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + context.parsed.y + ' contrato(s)';
                            },
                            footer: function (tooltipItems) {
                                if (!tooltipItems || tooltipItems.length === 0) return '';
                                const idx = tooltipItems[0].dataIndex;
                                const now = periodAtual[idx] || 0;
                                const prev = periodAnterior[idx] || 0;
                                if (prev > 0) {
                                    const varPct = (((now / prev) - 1) * 100).toFixed(0);
                                    const sign = varPct >= 0 ? '+' : '';
                                    return 'Variação: ' + sign + varPct + '%';
                                }
                                return '';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        grid: { color: '#EEF0F4' },
                        ticks: { font: { family: 'Inter', size: 11 }, precision: 0 }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'qtdChartCanvas') {
            this.qtdChart = newChart;
        }
        return true;
    },

    renderEvoChart: function (canvasId, labels, faturamento, mediaMovel, periodAnterior) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.evoChart && canvasId === 'evoChartCanvas') {
            this.evoChart.destroy();
            this.evoChart = null;
        }

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { type: 'bar', label: 'Faturamento', data: faturamento, backgroundColor: '#2563EB', borderRadius: 4 },
                    { type: 'line', label: 'Média móvel (3)', data: mediaMovel, borderColor: '#C8432F', borderWidth: 2.5, pointRadius: 0, tension: 0.35 },
                    { type: 'line', label: 'Período anterior', data: periodAnterior, borderColor: '#94A3B8', borderDash: [6, 4], borderWidth: 1.5, pointRadius: 0, tension: 0.3 }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: { usePointStyle: true, boxWidth: 8, padding: 12, font: { family: 'Inter', size: 11, weight: '500' } }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + fmtFull(context.parsed.y || 0);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) { return fmtCurrency(value); }
                        }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'evoChartCanvas') {
            this.evoChart = newChart;
        }
        return true;
    },

    renderStackChart: function (canvasId, labels, avcb, clcb, procAdm, obras) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.stackChart && canvasId === 'stackChartCanvas') {
            this.stackChart.destroy();
            this.stackChart = null;
        }

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [
                    { type: 'bar', label: 'AVCB', data: avcb, backgroundColor: '#C8432F', borderRadius: 2, stack: 's' },
                    { type: 'bar', label: 'CLCB', data: clcb, backgroundColor: '#DE9427', borderRadius: 2, stack: 's' },
                    { type: 'bar', label: 'Proc. Adm', data: procAdm, backgroundColor: '#6B46C1', borderRadius: 2, stack: 's' },
                    { type: 'bar', label: 'Obras', data: obras, backgroundColor: '#2B6CB0', borderRadius: 2, stack: 's' }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: { usePointStyle: true, boxWidth: 8, padding: 12, font: { family: 'Inter', size: 11, weight: '500' } }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        footerFont: { family: 'Inter', size: 12, weight: 'bold' },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + fmtFull(context.parsed.y || 0);
                            },
                            footer: function (tooltipItems) {
                                if (!tooltipItems || tooltipItems.length === 0) return '';
                                const idx = tooltipItems[0].dataIndex;
                                const totalMonth = (avcb[idx] || 0) + (clcb[idx] || 0) + (procAdm[idx] || 0) + (obras[idx] || 0);
                                const val = tooltipItems[0].parsed.y || 0;
                                if (totalMonth > 0) {
                                    const pct = ((val / totalMonth) * 100).toFixed(1);
                                    return 'Participação no mês: ' + pct + '%';
                                }
                                return '';
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        stacked: true,
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) { return fmtCurrency(value); }
                        }
                    },
                    x: {
                        stacked: true,
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'stackChartCanvas') {
            this.stackChart = newChart;
        }
        return true;
    },

    renderCostEvoChart: function (canvasId, labels, categories, seriesData) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.costEvoChart && canvasId === 'costEvoChartCanvas') {
            this.costEvoChart.destroy();
            this.costEvoChart = null;
        }

        const palette = ['#4F46E5', '#E11D48', '#D97706', '#0D9488', '#7C3AED', '#94A3B8'];

        const datasets = categories.map((cat, idx) => ({
            type: 'bar',
            label: cat,
            data: seriesData[idx],
            backgroundColor: palette[idx % palette.length],
            borderRadius: 2,
            stack: 'c'
        }));

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: { labels: labels, datasets: datasets },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                interaction: { mode: 'index', intersect: false },
                plugins: {
                    legend: {
                        position: 'top',
                        align: 'end',
                        labels: { usePointStyle: true, boxWidth: 8, padding: 12, font: { family: 'Inter', size: 11, weight: '500' } }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' ' + context.dataset.label + ': ' + fmtFull(context.parsed.y || 0);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        stacked: true,
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) { return fmtCurrency(value); }
                        }
                    },
                    x: {
                        stacked: true,
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'costEvoChartCanvas') {
            this.costEvoChart = newChart;
        }
        return true;
    },

    renderCostCatChart: function (canvasId, categories, values) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.costCatChart && canvasId === 'costCatChartCanvas') {
            this.costCatChart.destroy();
            this.costCatChart = null;
        }

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: categories,
                datasets: [{
                    label: 'Total no período',
                    data: values,
                    backgroundColor: '#2563EB',
                    borderRadius: 4
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' Total: ' + fmtFull(context.parsed.x || 0);
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) { return fmtCurrency(value); }
                        }
                    },
                    y: {
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'costCatChartCanvas') {
            this.costCatChart = newChart;
        }
        return true;
    },

    renderArecChart: function (canvasId, labels, values) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.arecChart && canvasId === 'arecChartCanvas') {
            this.arecChart.destroy();
            this.arecChart = null;
        }

        const lineColors = {
            'AVCB': '#C8432F',
            'CLCB': '#DE9427',
            'Proc. Adm': '#6B46C1',
            'Obras': '#2B6CB0'
        };

        const backgroundColors = labels.map(l => lineColors[l] || '#2563EB');

        const fmtCurrency = function (value) {
            if (value === null || value === undefined) return 'R$ 0';
            if (Math.abs(value) >= 1000000) {
                return 'R$ ' + (value / 1000000).toLocaleString('pt-BR', { minimumFractionDigits: 1, maximumFractionDigits: 1 }) + ' Mi';
            }
            if (Math.abs(value) >= 1000) {
                return 'R$ ' + Math.round(value / 1000).toLocaleString('pt-BR') + ' mil';
            }
            return 'R$ ' + Math.round(value).toLocaleString('pt-BR');
        };

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'A receber',
                    data: values,
                    backgroundColor: backgroundColors,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                return ' Saldo a receber: ' + fmtFull(context.parsed.y || 0);
                            }
                        }
                    }
                },
                scales: {
                    y: {
                        grid: { color: '#EEF0F4' },
                        ticks: {
                            font: { family: 'Inter', size: 11 },
                            callback: function (value) { return fmtCurrency(value); }
                        }
                    },
                    x: {
                        grid: { display: false },
                        ticks: { font: { family: 'Inter', size: 11 } }
                    }
                }
            }
        });

        if (canvasId === 'arecChartCanvas') {
            this.arecChart = newChart;
        }
        return true;
    },

    renderDonutChart: function (canvasId, labels, values) {
        const ctx = document.getElementById(canvasId);
        if (!ctx) return false;

        if (this.donutChart && canvasId === 'donutChartCanvas') {
            this.donutChart.destroy();
            this.donutChart = null;
        }

        const lineColors = {
            'AVCB': '#C8432F',
            'CLCB': '#DE9427',
            'Proc. Adm': '#6B46C1',
            'Obras': '#2B6CB0'
        };

        const backgroundColors = labels.map(l => lineColors[l] || '#2563EB');
        const total = values.reduce((a, b) => a + b, 0);

        const fmtFull = function (value) {
            if (value === null || value === undefined) return 'R$ 0,00';
            return 'R$ ' + value.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
        };

        const newChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: values,
                    backgroundColor: backgroundColors,
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '60%',
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            usePointStyle: true,
                            boxWidth: 8,
                            padding: 12,
                            font: { family: 'Inter', size: 12, weight: '500' }
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(23, 30, 50, 0.95)',
                        titleFont: { family: 'Archivo', size: 13, weight: 'bold' },
                        bodyFont: { family: 'Inter', size: 12 },
                        padding: 12,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                const val = context.parsed || 0;
                                const pct = total > 0 ? ((val / total) * 100).toFixed(1) : '0';
                                return ' ' + context.label + ': ' + fmtFull(val) + ' (' + pct + '%)';
                            }
                        }
                    }
                }
            }
        });

        if (canvasId === 'donutChartCanvas') {
            this.donutChart = newChart;
        }
        return true;
    }
};
